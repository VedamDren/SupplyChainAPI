using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.TransferPlanDTO;
using AutoMapper;
using SupplyChainMathLib;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly InventoryCalculator _calculator;

        public TransferPlansController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _calculator = new InventoryCalculator();
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<TransferPlanDto>>> GetAllTransferPlans()
        {
            var transferPlans = await _context.TransferPlans
                .Include(tp => tp.SourceSubdivision)
                .Include(tp => tp.DestinationSubdivision)
                .Include(tp => tp.Material)
                .ToListAsync();

            return Ok(_mapper.Map<List<TransferPlanDto>>(transferPlans));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransferPlanDto>> GetTransferPlan(int id)
        {
            var transferPlan = await _context.TransferPlans
                .Include(tp => tp.SourceSubdivision)
                .Include(tp => tp.DestinationSubdivision)
                .Include(tp => tp.Material)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (transferPlan == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TransferPlanDto>(transferPlan));
        }

        [HttpPost]
        public async Task<ActionResult<TransferPlanDto>> CreateTransferPlan(TransferPlanCreateDto transferPlanCreateDto)
        {
            // Проверка на уникальность комбинации SourceSubdivisionId, DestinationSubdivisionId, MaterialId и TransferDate
            var existingTransferPlan = await _context.TransferPlans
                .FirstOrDefaultAsync(tp =>
                    tp.SourceSubdivisionId == transferPlanCreateDto.SourceSubdivisionId &&
                    tp.DestinationSubdivisionId == transferPlanCreateDto.DestinationSubdivisionId &&
                    tp.MaterialId == transferPlanCreateDto.MaterialId &&
                    tp.TransferDate == transferPlanCreateDto.TransferDate);

            if (existingTransferPlan != null)
            {
                return BadRequest("План перемещений с такими параметрами уже существует");
            }

            var transferPlan = _mapper.Map<TransferPlan>(transferPlanCreateDto);

            _context.TransferPlans.Add(transferPlan);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для возврата
            await _context.Entry(transferPlan)
                .Reference(tp => tp.SourceSubdivision).LoadAsync();
            await _context.Entry(transferPlan)
                .Reference(tp => tp.DestinationSubdivision).LoadAsync();
            await _context.Entry(transferPlan)
                .Reference(tp => tp.Material).LoadAsync();

            return CreatedAtAction(nameof(GetTransferPlan),
                new { id = transferPlan.Id },
                _mapper.Map<TransferPlanDto>(transferPlan));
        }

        [HttpPost("calculate-year")]
        public async Task<ActionResult<TransferPlanCalculationResult>> CalculateYearlyTransferPlan([FromBody] YearlyCalculationRequest request)
        {
            try
            {
                // Валидация года
                if (request.Year < 2000 || request.Year > 2100)
                {
                    return BadRequest("Некорректный год. Допустимый диапазон: 2000-2100");
                }

                // 1. Получить все производственные подразделения
                var productionSubdivisions = await _context.Subdivisions
                    .Where(s => s.Type == SubdivisionType.Production)
                    .ToListAsync();

                if (!productionSubdivisions.Any())
                {
                    return BadRequest("Не найдено производственных подразделений");
                }

                // 2. Получить все торговые подразделения
                var tradingSubdivisions = await _context.Subdivisions
                    .Where(s => s.Type == SubdivisionType.Trading)
                    .ToListAsync();

                if (!tradingSubdivisions.Any())
                {
                    return BadRequest("Не найдено торговых подразделений");
                }

                // 3. Получить все готовые продукты
                var finishedProducts = await _context.Materials
                    .Where(m => m.Type == MaterialType.FinishedProduct)
                    .ToListAsync();

                if (!finishedProducts.Any())
                {
                    return BadRequest("Не найдено готовой продукции");
                }

                // 4. Удалить существующие планы за этот год
                var startDate = new DateTime(request.Year, 1, 1);
                var endDate = new DateTime(request.Year, 12, 31);

                var existingPlans = await _context.TransferPlans
                    .Where(tp => tp.TransferDate >= startDate && tp.TransferDate <= endDate)
                    .ToListAsync();

                if (existingPlans.Any())
                {
                    _context.TransferPlans.RemoveRange(existingPlans);
                    await _context.SaveChangesAsync();
                }

                var calculatedPlans = new List<TransferPlan>();
                var calculator = new InventoryCalculator();

                // 5. Для каждой комбинации: производственное -> торговое подразделение
                foreach (var production in productionSubdivisions)
                {
                    foreach (var trading in tradingSubdivisions)
                    {
                        // 6. Для каждой готовой продукции
                        foreach (var product in finishedProducts)
                        {
                            // 7. Для каждого месяца в году
                            for (int month = 1; month <= 12; month++)
                            {
                                var currentMonthDate = new DateTime(request.Year, month, 1);
                                var nextMonthDate = month == 12
                                    ? new DateTime(request.Year + 1, 1, 1)
                                    : new DateTime(request.Year, month + 1, 1);

                                try
                                {
                                    // 8. Получить план продаж на текущий месяц
                                    var currentMonthSalesPlan = await _context.SalesPlans
                                        .FirstOrDefaultAsync(sp =>
                                            sp.SubdivisionId == trading.Id &&
                                            sp.MaterialId == product.Id &&
                                            sp.Date.Year == request.Year &&
                                            sp.Date.Month == month);

                                    // 9. Получить план продаж на следующий месяц
                                    var nextMonthSalesPlan = await _context.SalesPlans
                                        .FirstOrDefaultAsync(sp =>
                                            sp.SubdivisionId == trading.Id &&
                                            sp.MaterialId == product.Id &&
                                            sp.Date.Year == nextMonthDate.Year &&
                                            sp.Date.Month == nextMonthDate.Month);

                                    // 10. Получить норматив обеспеченности запасом на текущий месяц
                                    var currentMonthRegulation = await _context.Regulations
                                        .FirstOrDefaultAsync(r =>
                                            r.SubdivisionId == trading.Id &&
                                            r.MaterialId == product.Id &&
                                            r.Date.Year == request.Year &&
                                            r.Date.Month == month);

                                    // 11. Получить норматив обеспеченности запасом на следующий месяц
                                    var nextMonthRegulation = await _context.Regulations
                                        .FirstOrDefaultAsync(r =>
                                            r.SubdivisionId == trading.Id &&
                                            r.MaterialId == product.Id &&
                                            r.Date.Year == nextMonthDate.Year &&
                                            r.Date.Month == nextMonthDate.Month);

                                    // 12. Получить план запасов на начало текущего месяца из базы
                                    var currentMonthInventoryPlan = await _context.InventoryPlans
                                        .FirstOrDefaultAsync(ip =>
                                            ip.SubdivisionId == trading.Id &&
                                            ip.MaterialId == product.Id &&
                                            ip.Date == currentMonthDate);

                                    decimal currentMonthInventory;

                                    // Если в базе есть план запасов на начало месяца, используем его
                                    if (currentMonthInventoryPlan != null)
                                    {
                                        currentMonthInventory = currentMonthInventoryPlan.Quantity;
                                        Console.WriteLine($"Для {product.Name}, месяц {month}: используется план запасов из базы = {currentMonthInventory}");
                                    }
                                    else
                                    {
                                        // Если нет плана в базе, рассчитываем по формуле
                                        if (currentMonthSalesPlan != null && currentMonthRegulation != null)
                                        {
                                            currentMonthInventory = calculator.CalculateTradingInventoryPlan(
                                                currentMonthSalesPlan.Quantity,
                                                currentMonthRegulation.DaysCount);
                                            Console.WriteLine($"Для {product.Name}, месяц {month}: рассчитан план запасов = {currentMonthInventory}");
                                        }
                                        else
                                        {
                                            // Если нет данных, пропускаем расчет для этого месяца
                                            continue;
                                        }
                                    }

                                    // 13. Определить план запасов на начало следующего месяца
                                    decimal nextMonthInventory;

                                    if (nextMonthSalesPlan != null && nextMonthRegulation != null)
                                    {
                                        // Рассчитываем по формуле: План продаж следующего месяца × Норматив следующего месяца / 30
                                        nextMonthInventory = calculator.CalculateTradingInventoryPlan(
                                            nextMonthSalesPlan.Quantity,
                                            nextMonthRegulation.DaysCount);
                                        Console.WriteLine($"Для {product.Name}, месяц {month + 1}: рассчитан план запасов = {nextMonthInventory}");
                                    }
                                    else
                                    {
                                        // Если нет данных на следующий месяц, пропускаем расчет
                                        continue;
                                    }

                                    // 14. Получить план продаж текущего месяца
                                    decimal currentMonthSales = currentMonthSalesPlan?.Quantity ?? 0;

                                    // 15. Рассчитать план перемещений по формуле из Excel: =D8-C8+C3
                                    // где: D8 = nextMonthInventory, C8 = currentMonthInventory, C3 = currentMonthSales
                                    decimal transferQuantity = nextMonthInventory - currentMonthInventory + currentMonthSales;

                                    Console.WriteLine($"Расчет для {product.Name}, месяц {month}:");
                                    Console.WriteLine($"  nextMonthInventory (D8) = {nextMonthInventory}");
                                    Console.WriteLine($"  currentMonthInventory (C8) = {currentMonthInventory}");
                                    Console.WriteLine($"  currentMonthSales (C3) = {currentMonthSales}");
                                    Console.WriteLine($"  transferQuantity (D8-C8+C3) = {transferQuantity}");

                                    // 16. Если количество положительное, создаем план перемещения
                                    if (transferQuantity > 0)
                                    {
                                        var transferPlan = new TransferPlan
                                        {
                                            SourceSubdivisionId = production.Id,
                                            DestinationSubdivisionId = trading.Id,
                                            MaterialId = product.Id,
                                            TransferDate = currentMonthDate,
                                            Quantity = (int)Math.Ceiling(transferQuantity)
                                        };

                                        calculatedPlans.Add(transferPlan);
                                        Console.WriteLine($"Создан план перемещений: {transferQuantity} -> {Math.Ceiling(transferQuantity)}");
                                    }
                                    else if (transferQuantity <= 0)
                                    {
                                        Console.WriteLine($"План перемещений не создан: transferQuantity = {transferQuantity} <= 0");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Логируем ошибку, но продолжаем расчет для других месяцев
                                    Console.WriteLine($"Ошибка при расчете для {product.Name}, месяц {month}: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // 17. Сохранить все рассчитанные планы
                if (calculatedPlans.Any())
                {
                    await _context.TransferPlans.AddRangeAsync(calculatedPlans);
                    await _context.SaveChangesAsync();

                    // 18. Загрузить связанные данные для возврата
                    var savedPlans = await _context.TransferPlans
                        .Include(tp => tp.SourceSubdivision)
                        .Include(tp => tp.DestinationSubdivision)
                        .Include(tp => tp.Material)
                        .Where(tp => tp.TransferDate.Year == request.Year)
                        .ToListAsync();

                    var result = new TransferPlanCalculationResult
                    {
                        Success = true,
                        Message = $"План перемещений на {request.Year} год успешно рассчитан",
                        CalculatedPlans = _mapper.Map<List<TransferPlanDto>>(savedPlans),
                        Year = request.Year,
                        PlansCount = savedPlans.Count,
                        Details = new CalculationDetails
                        {
                            ProductionSubdivisionsCount = productionSubdivisions.Count,
                            TradingSubdivisionsCount = tradingSubdivisions.Count,
                            FinishedProductsCount = finishedProducts.Count,
                            MonthsCalculated = 12
                        }
                    };

                    return Ok(result);
                }
                else
                {
                    return Ok(new TransferPlanCalculationResult
                    {
                        Success = true,
                        Message = "Нет данных для расчета плана перемещений",
                        Year = request.Year,
                        PlansCount = 0,
                        CalculatedPlans = new List<TransferPlanDto>()
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TransferPlanCalculationResult
                {
                    Success = false,
                    Message = $"Ошибка при расчете: {ex.Message}",
                    Year = request.Year,
                    PlansCount = 0
                });
            }
        }

        [HttpPost("calculate-test")]
        public async Task<ActionResult<TransferPlanCalculationResult>> TestCalculation([FromBody] TestCalculationRequest request)
        {
            try
            {
                // Тестовый расчет для демонстрации
                var calculator = new InventoryCalculator();

                // Пример расчета для января 2023 года
                // Для "Готовая продукция 1" в Excel: =D8-C8+C3
                // D8 = 165 (110 * 45 / 30)
                // C8 = 150 (фиксированное значение)
                // C3 = 100 (план продаж)
                // Результат: 165 - 150 + 100 = 115

                var testPlans = new List<TransferPlanDto>
                {
                    new TransferPlanDto
                    {
                        Id = 1,
                        SourceSubdivisionId = 1,
                        SourceSubdivisionName = "Производственное подразделение 1",
                        DestinationSubdivisionId = 2,
                        DestinationSubdivisionName = "Торговое подразделение 1",
                        MaterialId = 1,
                        MaterialName = "Готовая продукция 1",
                        TransferDate = new DateTime(request.Year, 1, 1),
                        Quantity = 115
                    },
                    new TransferPlanDto
                    {
                        Id = 2,
                        SourceSubdivisionId = 1,
                        SourceSubdivisionName = "Производственное подразделение 1",
                        DestinationSubdivisionId = 2,
                        DestinationSubdivisionName = "Торговое подразделение 1",
                        MaterialId = 2,
                        MaterialName = "Готовая продукция 2",
                        TransferDate = new DateTime(request.Year, 1, 1),
                        Quantity = 230
                    }
                };

                return Ok(new TransferPlanCalculationResult
                {
                    Success = true,
                    Message = $"Тестовый расчет для {request.Year} года выполнен успешно",
                    CalculatedPlans = testPlans,
                    Year = request.Year,
                    PlansCount = testPlans.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TransferPlanCalculationResult
                {
                    Success = false,
                    Message = $"Ошибка при тестовом расчете: {ex.Message}"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransferPlan(int id, TransferPlanUpdateDto transferPlanUpdateDto)
        {
            var transferPlan = await _context.TransferPlans.FindAsync(id);
            if (transferPlan == null)
            {
                return NotFound();
            }

            // Проверка на уникальность при обновлении
            if (transferPlanUpdateDto.SourceSubdivisionId.HasValue ||
                transferPlanUpdateDto.DestinationSubdivisionId.HasValue ||
                transferPlanUpdateDto.MaterialId.HasValue ||
                transferPlanUpdateDto.TransferDate.HasValue)
            {
                var sourceSubdivisionId = transferPlanUpdateDto.SourceSubdivisionId ?? transferPlan.SourceSubdivisionId;
                var destinationSubdivisionId = transferPlanUpdateDto.DestinationSubdivisionId ?? transferPlan.DestinationSubdivisionId;
                var materialId = transferPlanUpdateDto.MaterialId ?? transferPlan.MaterialId;
                var transferDate = transferPlanUpdateDto.TransferDate ?? transferPlan.TransferDate;

                var existingTransferPlan = await _context.TransferPlans
                    .FirstOrDefaultAsync(tp =>
                        tp.Id != id &&
                        tp.SourceSubdivisionId == sourceSubdivisionId &&
                        tp.DestinationSubdivisionId == destinationSubdivisionId &&
                        tp.MaterialId == materialId &&
                        tp.TransferDate == transferDate);

                if (existingTransferPlan != null)
                {
                    return BadRequest("План перемещений с такими параметрами уже существует");
                }
            }

            _mapper.Map(transferPlanUpdateDto, transferPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransferPlan(int id)
        {
            var transferPlan = await _context.TransferPlans.FindAsync(id);
            if (transferPlan == null)
            {
                return NotFound();
            }

            _context.TransferPlans.Remove(transferPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}