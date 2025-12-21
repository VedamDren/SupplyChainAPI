// ProductionCalculationController.cs - ДОБАВЛЯЕМ МЕТОДЫ СОХРАНЕНИЯ
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.ProductionCalculation;
using SupplyChainMathLib;
using System.Net;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using SupplyChainAPI.Models.ProductionPlan;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionCalculationController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly ILogger<ProductionCalculationController> _logger;
        private readonly ProductionPlanCalculator _productionPlanCalculator;
        private readonly ComprehensiveCalculator _comprehensiveCalculator;
        private readonly IMapper _mapper;

        // Константа для допуска при сравнении
        private const decimal COMPARISON_TOLERANCE = 0.01m;

        public ProductionCalculationController(
            SupplyChainContext context,
            ILogger<ProductionCalculationController> logger,
            IMapper mapper) // Добавляем IMapper в конструктор
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _productionPlanCalculator = new ProductionPlanCalculator();
            _comprehensiveCalculator = new ComprehensiveCalculator();
        }

        /// <summary>
        /// Рассчитать план производства на год для подразделения и материала
        /// </summary>
        [HttpPost("CalculateYearlyProductionPlan")]
        [ProducesResponseType(typeof(List<ProductionCalculationResult>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<List<ProductionCalculationResult>>> CalculateYearlyProductionPlan(
            [FromBody] ProductionCalculationRequest request)
        {
            // СУЩЕСТВУЮЩИЙ КОД РАСЧЕТА (БЕЗ ИЗМЕНЕНИЙ)
            // ... ваш существующий код расчета ...
            try
            {
                // Валидация входных данных
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                if (request.SubdivisionId <= 0)
                {
                    return BadRequest("Invalid SubdivisionId");
                }

                if (request.MaterialId <= 0)
                {
                    return BadRequest("Invalid MaterialId");
                }

                // Получаем данные о подразделении и материале
                var subdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Id == request.SubdivisionId);

                if (subdivision == null)
                {
                    return BadRequest($"Subdivision with Id {request.SubdivisionId} not found");
                }

                var material = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == request.MaterialId);

                if (material == null)
                {
                    return BadRequest($"Material with Id {request.MaterialId} not found");
                }

                var results = new List<ProductionCalculationResult>();
                var startDate = new DateTime(request.Date.Year, 1, 1);
                var endDate = new DateTime(request.Date.Year, 12, 1);

                // Сначала проверим, есть ли вообще данные в TransferPlans для этого подразделения
                var totalTransfersCount = await _context.TransferPlans
                    .CountAsync(tp => tp.SourceSubdivisionId == request.SubdivisionId &&
                                     tp.MaterialId == request.MaterialId &&
                                     tp.TransferDate.Year == request.Date.Year);

                _logger.LogInformation(
                    "Total transfers found for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}, Year: {Year}: {Count}",
                    request.SubdivisionId, request.MaterialId, request.Date.Year, totalTransfersCount);

                // Получаем ВСЕ данные за год одним запросом для оптимизации
                var yearTransfers = await _context.TransferPlans
                    .Where(tp => tp.SourceSubdivisionId == request.SubdivisionId &&
                                tp.MaterialId == request.MaterialId &&
                                tp.TransferDate.Year == request.Date.Year)
                    .Select(tp => new
                    {
                        tp.TransferDate,
                        tp.Quantity
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} transfer records from database",
                    yearTransfers.Count);

                // Группируем по месяцам
                var transferPlansByMonth = yearTransfers
                    .GroupBy(t => new DateTime(t.TransferDate.Year, t.TransferDate.Month, 1))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => t.Quantity)
                    );

                // Получаем все планы запасов за год
                var inventoryDates = new List<DateTime>();
                for (var month = 1; month <= 13; month++) // +1 месяц для декабря
                {
                    var date = new DateTime(request.Date.Year, month > 12 ? 1 : month, 1);
                    if (month > 12) date = date.AddYears(1); // Январь следующего года для расчета декабря
                    inventoryDates.Add(date);
                }

                var inventoryPlans = await _context.InventoryPlans
                    .Where(ip => ip.SubdivisionId == request.SubdivisionId &&
                                 ip.MaterialId == request.MaterialId &&
                                 inventoryDates.Contains(ip.Date))
                    .ToDictionaryAsync(ip => ip.Date, ip => ip.Quantity);

                // Рассчитываем для каждого месяца года
                for (var currentMonth = startDate; currentMonth <= endDate; currentMonth = currentMonth.AddMonths(1))
                {
                    try
                    {
                        var currentMonthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                        var nextMonthStart = currentMonthStart.AddMonths(1);

                        // Получаем план запасов на начало текущего месяца
                        decimal currentMonthInventory = await GetInventoryValueAsync(
                            request.SubdivisionId,
                            request.MaterialId,
                            currentMonthStart,
                            subdivision.Name,
                            material.Name);

                        // Получаем план запасов на начало следующего месяца
                        decimal nextMonthInventory = await GetInventoryValueAsync(
                            request.SubdivisionId,
                            request.MaterialId,
                            nextMonthStart,
                            subdivision.Name,
                            material.Name);

                        // Получаем план перемещений на текущий месяц ИЗ этого подразделения
                        decimal currentMonthTransfer = 0;
                        if (transferPlansByMonth.TryGetValue(currentMonthStart, out var transferQuantity))
                        {
                            currentMonthTransfer = transferQuantity;
                        }

                        // Логируем данные для отладки
                        _logger.LogDebug(
                            "Month {Month:yyyy-MM}: CurrentInventory={CurrentInventory}, NextInventory={NextInventory}, Transfer={Transfer}",
                            currentMonthStart, currentMonthInventory, nextMonthInventory, currentMonthTransfer);

                        // Используем ProductionPlanCalculator для расчета
                        var calculationResult = _productionPlanCalculator.CalculateDetailedProductionPlan(
                            currentMonthStart,
                            nextMonthInventory,
                            currentMonthInventory,
                            currentMonthTransfer);

                        // Создаем результат для ответа
                        var result = new ProductionCalculationResult
                        {
                            Date = currentMonthStart,
                            ProductionPlan = calculationResult.ProductionPlan,
                            CurrentInventory = calculationResult.CurrentMonthInventory,
                            PreviousInventory = calculationResult.NextMonthInventory,
                            TransferQuantity = calculationResult.TransferQuantity
                        };

                        results.Add(result);

                        // Логирование
                        _logger.LogInformation(
                            "Production plan calculated for {Year}-{Month:00}: " +
                            "CurrentInventory: {CurrentInventory}, " +
                            "NextMonthInventory: {NextMonthInventory}, " +
                            "Transfer: {Transfer}, " +
                            "ProductionPlan: {ProductionPlan}",
                            currentMonthStart.Year, currentMonthStart.Month,
                            currentMonthInventory, nextMonthInventory,
                            currentMonthTransfer, calculationResult.ProductionPlan);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Error calculating production plan for {Year}-{Month}, SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}",
                            currentMonth.Year, currentMonth.Month,
                            request.SubdivisionId, request.MaterialId);

                        // Добавляем результат с ошибкой
                        results.Add(new ProductionCalculationResult
                        {
                            Date = currentMonth,
                            ProductionPlan = 0,
                            CurrentInventory = 0,
                            PreviousInventory = 0,
                            TransferQuantity = 0
                        });
                    }
                }

                return Ok(results);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in yearly production calculation");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in yearly production calculation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error calculating yearly production plan for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}, Year: {Year}",
                    request?.SubdivisionId, request?.MaterialId, request?.Date.Year);
                return StatusCode(500, "Internal server error during yearly production calculation");
            }
        }

        /// <summary>
        /// Сохранить рассчитанный план производства в базу данных
        /// </summary>
        [HttpPost("SaveCalculatedPlan")]
        [ProducesResponseType(typeof(List<ProductionPlanResponseDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<List<ProductionPlanResponseDto>>> SaveCalculatedPlan(
            [FromBody] SaveProductionPlanRequestDto request)
        {
            try
            {
                // Валидация входных данных
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                if (request.SubdivisionId <= 0)
                {
                    return BadRequest("Invalid SubdivisionId");
                }

                if (request.MaterialId <= 0)
                {
                    return BadRequest("Invalid MaterialId");
                }

                if (request.CalculatedPlans == null || !request.CalculatedPlans.Any())
                {
                    return BadRequest("No calculated plans provided");
                }

                // Проверяем существование подразделения и материала
                var subdivisionExists = await _context.Subdivisions
                    .AnyAsync(s => s.Id == request.SubdivisionId);

                if (!subdivisionExists)
                {
                    return BadRequest($"Subdivision with Id {request.SubdivisionId} not found");
                }

                var materialExists = await _context.Materials
                    .AnyAsync(m => m.Id == request.MaterialId);

                if (!materialExists)
                {
                    return BadRequest($"Material with Id {request.MaterialId} not found");
                }

                var savedPlans = new List<ProductionPlan>();
                var results = new List<ProductionPlanResponseDto>();

                // Для каждого рассчитанного месяца
                foreach (var calculatedPlan in request.CalculatedPlans)
                {
                    try
                    {
                        // Проверяем валидность даты
                        if (calculatedPlan.Date == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid date in calculated plan, skipping");
                            continue;
                        }

                        // Нормализуем дату (начало месяца)
                        var planDate = new DateTime(
                            calculatedPlan.Date.Year,
                            calculatedPlan.Date.Month,
                            1);

                        // Проверяем, существует ли уже план на эту дату
                        var existingPlan = await _context.ProductionPlans
                            .FirstOrDefaultAsync(p =>
                                p.SubdivisionId == request.SubdivisionId &&
                                p.MaterialId == request.MaterialId &&
                                p.Date == planDate);

                        if (existingPlan != null)
                        {
                            // Обновляем существующий план
                            existingPlan.Quantity = (int)Math.Round(calculatedPlan.ProductionPlan);
                            existingPlan.Date = planDate;

                            _logger.LogInformation(
                                "Updated existing production plan for SubdivisionId: {SubdivisionId}, " +
                                "MaterialId: {MaterialId}, Date: {Date}, Quantity: {Quantity}",
                                request.SubdivisionId, request.MaterialId, planDate.ToString("yyyy-MM"),
                                existingPlan.Quantity);
                        }
                        else
                        {
                            // Создаем новый план
                            var newPlan = new ProductionPlan
                            {
                                SubdivisionId = request.SubdivisionId,
                                MaterialId = request.MaterialId,
                                Date = planDate,
                                Quantity = (int)Math.Round(calculatedPlan.ProductionPlan)
                            };

                            await _context.ProductionPlans.AddAsync(newPlan);
                            savedPlans.Add(newPlan);

                            _logger.LogInformation(
                                "Created new production plan for SubdivisionId: {SubdivisionId}, " +
                                "MaterialId: {MaterialId}, Date: {Date}, Quantity: {Quantity}",
                                request.SubdivisionId, request.MaterialId, planDate.ToString("yyyy-MM"),
                                newPlan.Quantity);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error saving calculated plan for SubdivisionId: {SubdivisionId}, " +
                            "MaterialId: {MaterialId}, Date: {Date}",
                            request.SubdivisionId, request.MaterialId, calculatedPlan.Date);
                    }
                }

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();

                // Загружаем сохраненные планы с навигационными свойствами для маппинга в DTO
                var savedPlanIds = savedPlans.Select(p => p.Id).ToList();
                var savedPlansWithNav = await _context.ProductionPlans
                    .Include(p => p.Subdivision)
                    .Include(p => p.Material)
                    .Where(p => savedPlanIds.Contains(p.Id))
                    .ToListAsync();

                // Маппим в DTO для ответа
                results = _mapper.Map<List<ProductionPlanResponseDto>>(savedPlansWithNav);

                // Логируем итог
                _logger.LogInformation(
                    "Successfully saved {SavedCount} production plans for SubdivisionId: {SubdivisionId}, " +
                    "MaterialId: {MaterialId}. Total processed: {TotalCount}",
                    results.Count, request.SubdivisionId, request.MaterialId,
                    request.CalculatedPlans.Count());

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error saving production plans for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}",
                    request?.SubdivisionId, request?.MaterialId);

                return StatusCode(500, "Internal server error during production plan save");
            }
        }

        /// <summary>
        /// Получить сохраненные планы производства по подразделению, материалу и году
        /// </summary>
        [HttpGet("GetSavedPlans/{subdivisionId}/{materialId}/{year}")]
        [ProducesResponseType(typeof(List<ProductionPlanResponseDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<List<ProductionPlanResponseDto>>> GetSavedPlans(
            int subdivisionId, int materialId, int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                var plans = await _context.ProductionPlans
                    .Include(p => p.Subdivision)
                    .Include(p => p.Material)
                    .Where(p => p.SubdivisionId == subdivisionId &&
                                p.MaterialId == materialId &&
                                p.Date >= startDate &&
                                p.Date <= endDate)
                    .OrderBy(p => p.Date)
                    .ToListAsync();

                if (!plans.Any())
                {
                    return NotFound($"No production plans found for SubdivisionId: {subdivisionId}, " +
                                   $"MaterialId: {materialId}, Year: {year}");
                }

                var result = _mapper.Map<List<ProductionPlanResponseDto>>(plans);

                _logger.LogInformation(
                    "Retrieved {Count} production plans for SubdivisionId: {SubdivisionId}, " +
                    "MaterialId: {MaterialId}, Year: {Year}",
                    result.Count, subdivisionId, materialId, year);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving production plans for SubdivisionId: {SubdivisionId}, " +
                    "MaterialId: {MaterialId}, Year: {Year}",
                    subdivisionId, materialId, year);

                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Удалить планы производства за определенный год
        /// </summary>
        [HttpDelete("DeleteYearlyPlans/{subdivisionId}/{materialId}/{year}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> DeleteYearlyPlans(int subdivisionId, int materialId, int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                var plansToDelete = await _context.ProductionPlans
                    .Where(p => p.SubdivisionId == subdivisionId &&
                                p.MaterialId == materialId &&
                                p.Date >= startDate &&
                                p.Date <= endDate)
                    .ToListAsync();

                if (!plansToDelete.Any())
                {
                    return BadRequest($"No production plans found to delete for " +
                                    $"SubdivisionId: {subdivisionId}, MaterialId: {materialId}, Year: {year}");
                }

                _context.ProductionPlans.RemoveRange(plansToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Deleted {Count} production plans for SubdivisionId: {SubdivisionId}, " +
                    "MaterialId: {MaterialId}, Year: {Year}",
                    plansToDelete.Count, subdivisionId, materialId, year);

                return Ok(new
                {
                    DeletedCount = plansToDelete.Count,
                    Message = "Production plans deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting production plans for SubdivisionId: {SubdivisionId}, " +
                    "MaterialId: {MaterialId}, Year: {Year}",
                    subdivisionId, materialId, year);

                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Сравнить расчетные планы с сохраненными
        /// </summary>
        [HttpPost("ComparePlans")]
        [ProducesResponseType(typeof(PlanComparisonResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<PlanComparisonResultDto>> ComparePlans(
            [FromBody] PlanComparisonRequestDto request)
        {
            try
            {
                if (request == null || request.CalculatedPlans == null || !request.CalculatedPlans.Any())
                {
                    return BadRequest("Invalid request data");
                }

                // Получаем сохраненные планы
                var year = request.CalculatedPlans.First().Date.Year;
                var savedPlans = await _context.ProductionPlans
                    .Where(p => p.SubdivisionId == request.SubdivisionId &&
                                p.MaterialId == request.MaterialId &&
                                p.Date.Year == year)
                    .ToListAsync();

                var comparison = new PlanComparisonResultDto
                {
                    SubdivisionId = request.SubdivisionId,
                    MaterialId = request.MaterialId,
                    Year = year,
                    TotalCalculatedPlans = request.CalculatedPlans.Count(),
                    TotalSavedPlans = savedPlans.Count
                };

                // Сравниваем по месяцам
                foreach (var calculatedPlan in request.CalculatedPlans)
                {
                    var monthStart = new DateTime(calculatedPlan.Date.Year, calculatedPlan.Date.Month, 1);
                    var savedPlan = savedPlans.FirstOrDefault(p => p.Date == monthStart);

                    var comparisonDetail = new PlanComparisonDetailDto
                    {
                        Date = monthStart,
                        CalculatedQuantity = calculatedPlan.ProductionPlan,
                        SavedQuantity = savedPlan?.Quantity ?? 0,
                        HasSavedPlan = savedPlan != null,
                        Difference = savedPlan != null ?
                            calculatedPlan.ProductionPlan - savedPlan.Quantity : 0
                    };

                    comparison.Details.Add(comparisonDetail);
                }

                // Рассчитываем статистику
                comparison.MatchingPlans = comparison.Details.Count(d =>
                    Math.Abs(d.Difference) < COMPARISON_TOLERANCE);

                comparison.DifferentPlans = comparison.Details.Count(d =>
                    d.HasSavedPlan && Math.Abs(d.Difference) >= COMPARISON_TOLERANCE);

                comparison.MissingPlans = comparison.Details.Count(d => !d.HasSavedPlan);

                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing production plans");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Тестовый метод для проверки данных в TransferPlans
        /// </summary>
        [HttpGet("DebugTransfers/{subdivisionId}/{materialId}/{year}")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<object>> DebugTransfers(int subdivisionId, int materialId, int year)
        {
            try
            {
                // Проверяем данные в TransferPlans
                var transfers = await _context.TransferPlans
                    .Where(tp => tp.SourceSubdivisionId == subdivisionId &&
                                tp.MaterialId == materialId &&
                                tp.TransferDate.Year == year)
                    .Select(tp => new
                    {
                        tp.Id,
                        tp.SourceSubdivisionId,
                        tp.DestinationSubdivisionId,
                        tp.MaterialId,
                        tp.TransferDate,
                        tp.Quantity,
                        SourceSubdivision = tp.SourceSubdivision.Name,
                        DestinationSubdivision = tp.DestinationSubdivision.Name,
                        Material = tp.Material.Name
                    })
                    .OrderBy(tp => tp.TransferDate)
                    .ToListAsync();

                // Группируем по месяцам для наглядности
                var monthlySummary = transfers
                    .GroupBy(t => new { t.TransferDate.Year, t.TransferDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalQuantity = g.Sum(t => t.Quantity),
                        Transfers = g.Select(t => new
                        {
                            t.Id,
                            t.TransferDate,
                            t.Quantity,
                            t.SourceSubdivision,
                            t.DestinationSubdivision,
                            t.Material
                        })
                    })
                    .ToList();

                return Ok(new
                {
                    TotalTransfers = transfers.Count,
                    MonthlySummary = monthlySummary,
                    AllTransfers = transfers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging transfers");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Рассчитать план производства по формуле:
        /// План производства = План запасов на начало следующего месяца - План запасов на начало текущего месяца + План перемещений текущего месяца
        /// </summary>
        [HttpPost("CalculateProductionPlan")]
        [ProducesResponseType(typeof(ProductionCalculationResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ProductionCalculationResult>> CalculateProductionPlan(
            [FromBody] ProductionCalculationRequest request)
        {
            try
            {
                // Валидация входных данных
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                if (request.SubdivisionId <= 0)
                {
                    return BadRequest("Invalid SubdivisionId");
                }

                if (request.MaterialId <= 0)
                {
                    return BadRequest("Invalid MaterialId");
                }

                // Получаем данные о подразделении и материале
                var subdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Id == request.SubdivisionId);

                if (subdivision == null)
                {
                    return BadRequest($"Subdivision with Id {request.SubdivisionId} not found");
                }

                var material = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == request.MaterialId);

                if (material == null)
                {
                    return BadRequest($"Material with Id {request.MaterialId} not found");
                }

                // Определяем текущий и следующий месяц
                var currentMonth = new DateTime(request.Date.Year, request.Date.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                // Получаем план запасов на начало текущего месяца
                var currentMonthInventory = await GetInventoryValueAsync(
                    request.SubdivisionId,
                    request.MaterialId,
                    currentMonth,
                    subdivision.Name,
                    material.Name);

                // Получаем план запасов на начало следующего месяца
                var nextMonthInventory = await GetInventoryValueAsync(
                    request.SubdivisionId,
                    request.MaterialId,
                    nextMonth,
                    subdivision.Name,
                    material.Name);

                // Получаем план перемещений на текущий месяц ИЗ этого подразделения
                var currentMonthTransfer = await GetTransferQuantityAsync(
                    request.SubdivisionId,
                    request.MaterialId,
                    currentMonth);

                // Используем новую библиотеку для расчета
                var calculationResult = _productionPlanCalculator.CalculateDetailedProductionPlan(
                    currentMonth,
                    nextMonthInventory,
                    currentMonthInventory,
                    currentMonthTransfer);

                // Создаем результат для ответа
                var result = new ProductionCalculationResult
                {
                    Date = currentMonth,
                    ProductionPlan = calculationResult.ProductionPlan,
                    CurrentInventory = calculationResult.CurrentMonthInventory,
                    PreviousInventory = calculationResult.NextMonthInventory,
                    TransferQuantity = calculationResult.TransferQuantity
                };

                // Логирование
                _logger.LogInformation(
                    "Production plan calculated for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}, Date: {Date:yyyy-MM}. " +
                    "CurrentInventory: {CurrentInventory}, NextInventory: {NextInventory}, Transfer: {Transfer}, ProductionPlan: {ProductionPlan}",
                    request.SubdivisionId, request.MaterialId, currentMonth.ToString("yyyy-MM"),
                    currentMonthInventory, nextMonthInventory, currentMonthTransfer, calculationResult.ProductionPlan);

                return Ok(result);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in production calculation");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in production calculation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error calculating production plan for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}",
                    request?.SubdivisionId, request?.MaterialId);
                return StatusCode(500, "Internal server error during production calculation");
            }
        }

        #region Вспомогательные методы (без изменений)

        private async Task<decimal> GetInventoryValueAsync(
            int subdivisionId,
            int materialId,
            DateTime date,
            string subdivisionName = null,
            string materialName = null)
        {
            // Для января 2023 используем фиксированные значения
            if (InitialValues2023.IsJanuary2023(date))
            {
                if (string.IsNullOrEmpty(subdivisionName) || string.IsNullOrEmpty(materialName))
                {
                    var subdivision = await _context.Subdivisions.FindAsync(subdivisionId);
                    var material = await _context.Materials.FindAsync(materialId);

                    if (subdivision != null && material != null)
                    {
                        subdivisionName = subdivision.Name;
                        materialName = material.Name;
                    }
                }

                if (!string.IsNullOrEmpty(subdivisionName) && !string.IsNullOrEmpty(materialName))
                {
                    var fixedValue = InitialValues2023.GetFixedJanuary2023Value(
                        subdivisionName,
                        materialName);

                    _logger.LogDebug(
                        "Using fixed January 2023 value for {Subdivision}/{Material}: {Value}",
                        subdivisionName, materialName, fixedValue);

                    return fixedValue;
                }
            }

            var inventoryPlan = await _context.InventoryPlans
                .FirstOrDefaultAsync(ip =>
                    ip.SubdivisionId == subdivisionId &&
                    ip.MaterialId == materialId &&
                    ip.Date == date);

            var value = inventoryPlan?.Quantity ?? 0;

            _logger.LogDebug(
                "Inventory value for SubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}, Date: {Date}: {Value}",
                subdivisionId, materialId, date.ToString("yyyy-MM-dd"), value);

            return value;
        }

        private async Task<decimal> GetTransferQuantityAsync(int subdivisionId, int materialId, DateTime date)
        {
            // ИСПРАВЛЕНО: используем SourceSubdivisionId вместо DestinationSubdivisionId
            // Для расчета производства нужны перемещения ИЗ подразделения
            var transfers = await _context.TransferPlans
                .Where(tp =>
                    tp.SourceSubdivisionId == subdivisionId &&
                    tp.MaterialId == materialId &&
                    tp.TransferDate.Year == date.Year &&
                    tp.TransferDate.Month == date.Month)
                .ToListAsync();

            var total = transfers.Sum(t => t.Quantity);

            _logger.LogDebug(
                "Transfer quantity for SourceSubdivisionId: {SubdivisionId}, MaterialId: {MaterialId}, Month: {Year}-{Month}: {Count} records, Total: {Total}",
                subdivisionId, materialId, date.Year, date.Month, transfers.Count, total);

            return total;
        }

        #endregion
    }
}