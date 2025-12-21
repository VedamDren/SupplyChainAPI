using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.RawMaterialPurchaseDTO;
using AutoMapper;
using SupplyChainMathLib;
using System.Net;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawMaterialPurchasesController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RawMaterialPurchasesController> _logger;
        private readonly ComprehensiveCalculator _calculator;

        public RawMaterialPurchasesController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<RawMaterialPurchasesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _calculator = new ComprehensiveCalculator();
        }

        // Существующие методы...

        // POST: api/RawMaterialPurchases/CalculateYearPlan
        [HttpPost("CalculateYearPlan")]
        [ProducesResponseType(typeof(RawMaterialPurchaseYearPlanResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<RawMaterialPurchaseYearPlanResultDto>> CalculateYearPlan(
            RawMaterialPurchaseYearPlanRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Получаем данные подразделения и сырья
                var subdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Id == request.SubdivisionId);

                var rawMaterial = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == request.RawMaterialId);

                if (subdivision == null || rawMaterial == null)
                {
                    return NotFound("Подразделение или сырье не найдены");
                }

                // Получаем технологические карты для данного подразделения и сырья
                var techCards = await _context.TechnologicalCards
                    .Include(tc => tc.FinishedProduct)
                    .Where(tc => tc.SubdivisionId == request.SubdivisionId &&
                                 tc.RawMaterialId == request.RawMaterialId)
                    .ToListAsync();

                if (!techCards.Any())
                {
                    return BadRequest($"Нет технологических карт для сырья '{rawMaterial.Name}' в подразделении '{subdivision.Name}'");
                }

                var monthlyPlans = new List<MonthlyPurchasePlanDto>();
                var yearStart = new DateTime(request.Year, 1, 1);

                // Расчет для каждого месяца года
                for (int month = 1; month <= 12; month++)
                {
                    var currentMonth = new DateTime(request.Year, month, 1);
                    var nextMonth = currentMonth.AddMonths(1);

                    try
                    {
                        var monthlyPlan = await CalculateMonthlyPurchasePlan(
                            request.SubdivisionId,
                            request.RawMaterialId,
                            currentMonth,
                            techCards);

                        monthlyPlans.Add(monthlyPlan);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"Ошибка расчета для месяца {currentMonth:MM.yyyy}, SubdivisionId: {request.SubdivisionId}, RawMaterialId: {request.RawMaterialId}");

                        // Добавляем план с нулевым количеством и описанием ошибки
                        monthlyPlans.Add(new MonthlyPurchasePlanDto
                        {
                            SubdivisionId = request.SubdivisionId,
                            RawMaterialId = request.RawMaterialId,
                            Date = currentMonth,
                            PurchasePlanQuantity = 0,
                            CurrentMonthInventory = 0,
                            NextMonthInventory = 0,
                            TotalProductionPlans = 0,
                            CalculationFormula = "Ошибка расчета",
                            Note = $"Ошибка: {ex.Message}"
                        });
                    }
                }

                // Формируем результат
                var result = new RawMaterialPurchaseYearPlanResultDto
                {
                    SubdivisionId = request.SubdivisionId,
                    SubdivisionName = subdivision.Name,
                    RawMaterialId = request.RawMaterialId,
                    RawMaterialName = rawMaterial.Name,
                    Year = request.Year,
                    TotalQuantity = monthlyPlans.Sum(p => p.PurchasePlanQuantity),
                    AverageMonthlyQuantity = monthlyPlans.Any() ?
                        monthlyPlans.Average(p => p.PurchasePlanQuantity) : 0,
                    MonthlyPlans = monthlyPlans,
                    CalculationSummary = GenerateCalculationSummary(monthlyPlans)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка расчета годового плана закупа");
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        // POST: api/RawMaterialPurchases/SaveYearPlan
        [HttpPost("SaveYearPlan")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SaveYearPlan(SaveYearPlanRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid || request?.PurchasePlans == null || !request.PurchasePlans.Any())
                {
                    return BadRequest(ModelState);
                }

                var savedCount = 0;
                var updatedCount = 0;

                foreach (var plan in request.PurchasePlans)
                {
                    // Проверяем, существует ли уже запись на эту дату
                    var existingPurchase = await _context.RawMaterialPurchases
                        .FirstOrDefaultAsync(p =>
                            p.SubdivisionId == plan.SubdivisionId &&
                            p.RawMaterialId == plan.RawMaterialId &&
                            p.PurchaseDate.Year == plan.Date.Year &&
                            p.PurchaseDate.Month == plan.Date.Month);

                    if (existingPurchase != null)
                    {
                        // Обновляем существующую запись
                        existingPurchase.Quantity = (int)Math.Round(plan.PurchasePlanQuantity);
                        existingPurchase.PurchaseDate = plan.Date;
                        _context.Entry(existingPurchase).State = EntityState.Modified;
                        updatedCount++;
                    }
                    else
                    {
                        // Создаем новую запись
                        var newPurchase = new RawMaterialPurchase
                        {
                            SubdivisionId = plan.SubdivisionId,
                            RawMaterialId = plan.RawMaterialId,
                            PurchaseDate = plan.Date,
                            Quantity = (int)Math.Round(plan.PurchasePlanQuantity)
                        };
                        _context.RawMaterialPurchases.Add(newPurchase);
                        savedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"План закупа успешно сохранен",
                    Saved = savedCount,
                    Updated = updatedCount,
                    Total = savedCount + updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения годового плана закупа");
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        // Вспомогательный метод для расчета месячного плана закупа
        private async Task<MonthlyPurchasePlanDto> CalculateMonthlyPurchasePlan(
            int subdivisionId,
            int rawMaterialId,
            DateTime month,
            List<TechnologicalCard> techCards)
        {
            var nextMonth = month.AddMonths(1);

            // 1. Получаем план запасов на начало текущего месяца
            var currentMonthInventoryPlan = await _context.InventoryPlans
                .FirstOrDefaultAsync(ip =>
                    ip.SubdivisionId == subdivisionId &&
                    ip.MaterialId == rawMaterialId &&
                    ip.Date.Year == month.Year &&
                    ip.Date.Month == month.Month);

            // 2. Получаем план запасов на начало следующего месяца
            var nextMonthInventoryPlan = await _context.InventoryPlans
                .FirstOrDefaultAsync(ip =>
                    ip.SubdivisionId == subdivisionId &&
                    ip.MaterialId == rawMaterialId &&
                    ip.Date.Year == nextMonth.Year &&
                    ip.Date.Month == nextMonth.Month);

            decimal currentMonthInventory = currentMonthInventoryPlan?.Quantity ?? 0;
            decimal nextMonthInventory = nextMonthInventoryPlan?.Quantity ?? 0;

            // 3. Рассчитываем сумму планов производства за текущий месяц
            //    по всем готовым продуктам, использующим это сырье
            decimal totalProductionPlans = 0;
            var productionDetails = new List<string>();

            foreach (var techCard in techCards)
            {
                var productionPlan = await _context.ProductionPlans
                    .FirstOrDefaultAsync(pp =>
                        pp.SubdivisionId == subdivisionId &&
                        pp.MaterialId == techCard.FinishedProductId &&
                        pp.Date.Year == month.Year &&
                        pp.Date.Month == month.Month);

                if (productionPlan != null)
                {
                    // Предполагаем коэффициент 1:1 (по умолчанию)
                    // В реальном проекте нужно учитывать коэффициент из технологической карты
                    decimal conversionRate = 1.0m; // techCard.ConversionRate если есть такое поле
                    totalProductionPlans += productionPlan.Quantity * conversionRate;

                    productionDetails.Add($"{productionPlan.Quantity} ед. ({techCard.FinishedProduct?.Name})");
                }
            }

            // 4. Рассчитываем план закупа по формуле
            // План закупа = План запасов на начало следующего месяца - 
            //                План запасов на начало текущего месяца + 
            //                сумма планов производства за текущий месяц
            var purchasePlan = _calculator.CalculateMonthlyPlan(new MonthlyCalculationRequest
            {
                Date = month,
                SubdivisionName = (await _context.Subdivisions.FindAsync(subdivisionId))?.Name ?? "",
                MaterialName = (await _context.Materials.FindAsync(rawMaterialId))?.Name ?? "",
                NextMonthRawMaterialInventory = nextMonthInventory,
                CurrentMonthRawMaterialInventory = currentMonthInventory,
                TotalProductionForPurchase = totalProductionPlans
            }).RawMaterialPurchasePlan ?? 0;

            // Формируем информацию о расчете
            var calculationFormula = $"Закуп = {nextMonthInventory:F2} (запасы след. мес.) - " +
                                   $"{currentMonthInventory:F2} (запасы тек. мес.) + " +
                                   $"{totalProductionPlans:F2} (сумма произв. планов) = " +
                                   $"{purchasePlan:F2}";

            var note = productionDetails.Any()
                ? $"Производственные планы: {string.Join(", ", productionDetails)}"
                : "Нет производственных планов на этот месяц";

            return new MonthlyPurchasePlanDto
            {
                SubdivisionId = subdivisionId,
                RawMaterialId = rawMaterialId,
                Date = month,
                PurchasePlanQuantity = purchasePlan,
                CurrentMonthInventory = currentMonthInventory,
                NextMonthInventory = nextMonthInventory,
                TotalProductionPlans = totalProductionPlans,
                CalculationFormula = calculationFormula,
                Note = note
            };
        }

        // Вспомогательный метод для генерации сводки расчета
        private string GenerateCalculationSummary(List<MonthlyPurchasePlanDto> monthlyPlans)
        {
            if (!monthlyPlans.Any())
                return "Нет данных для расчета";

            var totalPurchase = monthlyPlans.Sum(p => p.PurchasePlanQuantity);
            var avgPurchase = monthlyPlans.Average(p => p.PurchasePlanQuantity);
            var maxPurchaseMonth = monthlyPlans.OrderByDescending(p => p.PurchasePlanQuantity).First();
            var minPurchaseMonth = monthlyPlans.OrderBy(p => p.PurchasePlanQuantity).First();

            return $"Общий объем закупа на год: {totalPurchase:F2} ед.\n" +
                   $"Среднемесячный объем: {avgPurchase:F2} ед.\n" +
                   $"Максимальный закуп: {maxPurchaseMonth.PurchasePlanQuantity:F2} ед. ({maxPurchaseMonth.Date:MMM})\n" +
                   $"Минимальный закуп: {minPurchaseMonth.PurchasePlanQuantity:F2} ед. ({minPurchaseMonth.Date:MMM})";
        }

        // GET: api/RawMaterialPurchases/GetExistingPlans/{subdivisionId}/{rawMaterialId}/{year}
        [HttpGet("GetExistingPlans/{subdivisionId}/{rawMaterialId}/{year}")]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialPurchaseResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<RawMaterialPurchaseResponseDto>>> GetExistingPlans(
            int subdivisionId, int rawMaterialId, int year)
        {
            try
            {
                var existingPlans = await _context.RawMaterialPurchases
                    .Include(p => p.Subdivision)
                    .Include(p => p.RawMaterial)
                    .Where(p => p.SubdivisionId == subdivisionId &&
                                p.RawMaterialId == rawMaterialId &&
                                p.PurchaseDate.Year == year)
                    .OrderBy(p => p.PurchaseDate)
                    .ToListAsync();

                var planDtos = _mapper.Map<List<RawMaterialPurchaseResponseDto>>(existingPlans);
                return Ok(planDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Ошибка получения существующих планов для SubdivisionId: {subdivisionId}, RawMaterialId: {rawMaterialId}, Year: {year}");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/RawMaterialPurchases/RecalculatePlans
        [HttpPost("RecalculatePlans")]
        [ProducesResponseType(typeof(RawMaterialPurchaseYearPlanResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<RawMaterialPurchaseYearPlanResultDto>> RecalculatePlans(
            RawMaterialPurchaseYearPlanRequestDto request)
        {
            try
            {
                // Проверяем, есть ли сохраненные планы
                var existingPlans = await _context.RawMaterialPurchases
                    .Where(p => p.SubdivisionId == request.SubdivisionId &&
                                p.RawMaterialId == request.RawMaterialId &&
                                p.PurchaseDate.Year == request.Year)
                    .ToListAsync();

                if (existingPlans.Any())
                {
                    // Удаляем существующие планы перед перерасчетом
                    _context.RawMaterialPurchases.RemoveRange(existingPlans);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        $"Удалено {existingPlans.Count} существующих планов перед перерасчетом. " +
                        $"SubdivisionId: {request.SubdivisionId}, RawMaterialId: {request.RawMaterialId}, Year: {request.Year}");
                }

                // Выполняем расчет
                return await CalculateYearPlan(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перерасчета планов");
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }
    }
}