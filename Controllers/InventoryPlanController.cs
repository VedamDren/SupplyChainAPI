using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.InventoryPlan;
using SupplyChainAPI.Models.InventoryCalculation;
using System.Net;
using SupplyChainAPI.Services;
using SupplyChainMathLib;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly ILogger<InventoryPlansController> _logger;
        private readonly InventoryCalculator _inventoryCalculator;

        public InventoryPlansController(
            SupplyChainContext context,
            ILogger<InventoryPlansController> logger)
        {
            _context = context;
            _logger = logger;
            _inventoryCalculator = new InventoryCalculator();
        }

        // ============================================
        // 1. ОСНОВНОЙ МЕТОД РАСЧЕТА (универсальный, для обратной совместимости)
        // ============================================
        [HttpPost("calculate")]
        [ApiExplorerSettings(IgnoreApi = true)] // Скрыть из Swagger
        public async Task<ActionResult<InventoryCalculationResult>> CalculateInventoryPlan(
            [FromBody] InventoryCalculationRequest request)
        {
            try
            {
                _logger.LogWarning("Используется универсальный метод расчета. Рекомендуется использовать специализированные методы.");

                // Определяем тип подразделения и вызываем соответствующий метод
                var subdivision = await _context.Subdivisions.FindAsync(request.SubdivisionId);
                if (subdivision == null)
                    return NotFound(new { message = $"Подразделение с ID {request.SubdivisionId} не найдено" });

                if (subdivision.Type == SubdivisionType.Trading)
                {
                    return await CalculateTradingPlan(request);
                }
                else if (subdivision.Type == SubdivisionType.Production)
                {
                    return await CalculateProductionPlan(request);
                }
                else
                {
                    return BadRequest(new { message = "Неподдерживаемый тип подразделения" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в универсальном методе расчета");
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        // ============================================
        // 2. РАСЧЕТ ПЛАНА ЗАПАСОВ ДЛЯ ТОРГОВОГО ПОДРАЗДЕЛЕНИЯ
        // Формула: (План продаж × Норматив) ÷ 30
        // ============================================
        [HttpPost("calculate-trading")]
        public async Task<ActionResult<InventoryCalculationResult>> CalculateTradingPlan(
            [FromBody] InventoryCalculationRequest request)
        {
            try
            {
                _logger.LogInformation("РАСЧЕТ ДЛЯ ТОРГОВОГО ПОДРАЗДЕЛЕНИЯ: SubdivisionId={SubdivisionId}, MaterialId={MaterialId}, Date={Date}",
                    request.SubdivisionId, request.MaterialId, request.Date);

                // Валидация
                if (request == null)
                    return BadRequest(new { message = "Запрос не может быть пустым" });

                if (request.Date == default)
                    return BadRequest(new { message = "Дата обязательна" });

                // Получаем данные из БД
                var subdivision = await _context.Subdivisions.FindAsync(request.SubdivisionId);
                var material = await _context.Materials.FindAsync(request.MaterialId);

                if (subdivision == null)
                    return NotFound(new { message = $"Подразделение с ID {request.SubdivisionId} не найдено" });

                if (material == null)
                    return NotFound(new { message = $"Материал с ID {request.MaterialId} не найден" });

                // Проверяем, что подразделение действительно торговое
                if (subdivision.Type != SubdivisionType.Trading)
                {
                    return BadRequest(new
                    {
                        message = "Этот метод предназначен только для торговых подразделений",
                        actualType = subdivision.Type.ToString(),
                        suggestion = "Используйте метод calculate-production для производственных подразделений"
                    });
                }

                // Получаем начало месяца для даты запроса
                var monthStart = new DateTime(request.Date.Year, request.Date.Month, 1);

                // Проверяем фиксированные значения для января 2023
                if (InitialValues2023.IsJanuary2023(monthStart))
                {
                    var fixedValue = InitialValues2023.GetFixedJanuary2023Value(subdivision.Name, material.Name);
                    return Ok(new InventoryCalculationResult
                    {
                        Date = monthStart,
                        InventoryPlan = fixedValue,
                        SalesPlan = 0,
                        TransferPlan = null,
                        StockNorm = 30,
                        DaysInMonth = DateTime.DaysInMonth(request.Date.Year, request.Date.Month),
                        CalculatedQuantity = fixedValue,
                        IsFixedPlan = true,
                        CalculationType = "Фиксированный план (январь 2023)",
                        Formula = $"Фиксированное значение: {fixedValue}",
                        SubdivisionName = subdivision.Name,
                        MaterialName = material.Name,
                        Message = $"Фиксированное значение для января 2023: {fixedValue}"
                    });
                }

                // Получаем норматив (Regulation) для расчета
                var regulation = await _context.Regulations
                    .FirstOrDefaultAsync(r => r.SubdivisionId == request.SubdivisionId &&
                                              r.MaterialId == request.MaterialId &&
                                              r.Date.Year == monthStart.Year &&
                                              r.Date.Month == monthStart.Month);

                decimal stockNorm = regulation?.DaysCount ?? 30;

                // Ищем план продаж для торгового подразделения
                var salesPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp => sp.SubdivisionId == request.SubdivisionId &&
                                               sp.MaterialId == request.MaterialId &&
                                               sp.Date.Year == monthStart.Year &&
                                               sp.Date.Month == monthStart.Month);

                if (salesPlan == null)
                {
                    return NotFound(new
                    {
                        message = "Не найден план продаж для расчета",
                        details = $"Подразделение: {subdivision.Name}, Материал: {material.Name}, Месяц: {monthStart.ToString("MMMM yyyy")}",
                        suggestion = "Сначала создайте план продаж для выбранных параметров"
                    });
                }

                decimal salesPlanQuantity = salesPlan.Quantity;

                // Формула для торгового подразделения: (План продаж × Норматив) ÷ 30
                decimal calculatedValue = (salesPlanQuantity * stockNorm) / 30;
                string formula = $"({salesPlanQuantity} × {stockNorm}) ÷ 30 = {calculatedValue:F2}";

                // Формируем результат
                var result = new InventoryCalculationResult
                {
                    Date = monthStart,
                    InventoryPlan = calculatedValue,
                    SalesPlan = salesPlanQuantity, // Возвращаем план продаж для информации
                    TransferPlan = null,
                    StockNorm = stockNorm,
                    DaysInMonth = DateTime.DaysInMonth(request.Date.Year, request.Date.Month),
                    CalculatedQuantity = calculatedValue,
                    IsFixedPlan = false,
                    CalculationType = "Расчет для торгового подразделения",
                    Formula = formula,
                    SubdivisionName = subdivision.Name,
                    MaterialName = material.Name,
                    Message = $"Расчет выполнен по формуле: {formula}"
                };

                _logger.LogInformation("Расчет для торгового подразделения выполнен: {Formula}", formula);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете плана запасов для торгового подразделения");
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при расчете плана запасов",
                    error = ex.Message,
                    details = ex.StackTrace
                });
            }
        }

        // ============================================
        // 3. РАСЧЕТ ПЛАНА ЗАПАСОВ ДЛЯ ПРОИЗВОДСТВЕННОГО ПОДРАЗДЕЛЕНИЯ
        // Формула: (Сумма планов перемещений предыдущего месяца × Норматив текущего месяца) ÷ 30
        // ============================================
        [HttpPost("calculate-production")]
        public async Task<ActionResult<InventoryCalculationResult>> CalculateProductionPlan(
            [FromBody] InventoryCalculationRequest request)
        {
            try
            {
                _logger.LogInformation("РАСЧЕТ ДЛЯ ПРОИЗВОДСТВЕННОГО ПОДРАЗДЕЛЕНИЯ: SubdivisionId={SubdivisionId}, MaterialId={MaterialId}, Date={Date}",
                    request.SubdivisionId, request.MaterialId, request.Date);

                // Валидация
                if (request == null)
                    return BadRequest(new { message = "Запрос не может быть пустым" });

                if (request.Date == default)
                    return BadRequest(new { message = "Дата обязательна" });

                // Получаем данные из БД
                var subdivision = await _context.Subdivisions.FindAsync(request.SubdivisionId);
                var material = await _context.Materials.FindAsync(request.MaterialId);

                if (subdivision == null)
                    return NotFound(new { message = $"Подразделение с ID {request.SubdivisionId} не найдено" });

                if (material == null)
                    return NotFound(new { message = $"Материал с ID {request.MaterialId} не найден" });

                // Проверяем, что подразделение действительно производственное
                if (subdivision.Type != SubdivisionType.Production)
                {
                    return BadRequest(new
                    {
                        message = "Этот метод предназначен только для производственных подразделений",
                        actualType = subdivision.Type.ToString(),
                        suggestion = "Используйте метод calculate-trading для торговых подразделений"
                    });
                }

                // Получаем начало месяца для даты запроса
                var monthStart = new DateTime(request.Date.Year, request.Date.Month, 1);

                // Проверяем фиксированные значения для января 2023
                if (InitialValues2023.IsJanuary2023(monthStart))
                {
                    var fixedValue = InitialValues2023.GetFixedJanuary2023Value(subdivision.Name, material.Name);
                    return Ok(new InventoryCalculationResult
                    {
                        Date = monthStart,
                        InventoryPlan = fixedValue,
                        SalesPlan = 0,
                        TransferPlan = null,
                        StockNorm = 30,
                        DaysInMonth = DateTime.DaysInMonth(request.Date.Year, request.Date.Month),
                        CalculatedQuantity = fixedValue,
                        IsFixedPlan = true,
                        CalculationType = "Фиксированный план (январь 2023)",
                        Formula = $"Фиксированное значение: {fixedValue}",
                        SubdivisionName = subdivision.Name,
                        MaterialName = material.Name,
                        Message = $"Фиксированное значение для января 2023: {fixedValue}"
                    });
                }

                // Получаем норматив (Regulation) для расчета ТЕКУЩЕГО месяца
                var regulation = await _context.Regulations
                    .FirstOrDefaultAsync(r => r.SubdivisionId == request.SubdivisionId &&
                                              r.MaterialId == request.MaterialId &&
                                              r.Date.Year == monthStart.Year &&
                                              r.Date.Month == monthStart.Month);

                decimal stockNorm = regulation?.DaysCount ?? 30;

                // Получаем предыдущий месяц
                var previousMonthStart = monthStart.AddMonths(-1);
                _logger.LogInformation("Для расчета используются данные предыдущего месяца: {PreviousMonth}", previousMonthStart.ToString("yyyy-MM"));

                // Ищем планы перемещений где это производственное подразделение является источником В ПРЕДЫДУЩЕМ МЕСЯЦЕ
                var transferPlans = await _context.TransferPlans
                    .Where(tp => tp.SourceSubdivisionId == request.SubdivisionId &&
                                 tp.MaterialId == request.MaterialId &&
                                 tp.TransferDate.Year == previousMonthStart.Year &&
                                 tp.TransferDate.Month == previousMonthStart.Month)
                    .ToListAsync();

                // Также можно искать планы перемещений, где TransferDate в пределах предыдущего месяца
                // Альтернативный вариант фильтрации
                var transferPlansAlternative = await _context.TransferPlans
                    .Where(tp => tp.SourceSubdivisionId == request.SubdivisionId &&
                                 tp.MaterialId == request.MaterialId &&
                                 tp.TransferDate >= previousMonthStart &&
                                 tp.TransferDate < monthStart)
                    .ToListAsync();

                // Используем альтернативный вариант для большей точности
                decimal transferPlanQuantity = transferPlansAlternative.Sum(tp => tp.Quantity);

                _logger.LogInformation("Найдено {Count} планов перемещений для производственного подразделения {SubdivisionId}, материала {MaterialId}, предыдущий месяц {PreviousMonth}, норматив текущего месяца={StockNorm}",
                    transferPlansAlternative.Count, request.SubdivisionId, request.MaterialId, previousMonthStart.ToString("yyyy-MM"), stockNorm);

                // Проверяем, есть ли данные за предыдущий месяц
                if (transferPlanQuantity == 0)
                {
                    _logger.LogWarning("Нет данных по планам перемещений за предыдущий месяц. Проверка альтернативных источников...");

                    // Можно также проверить наличие данных за предыдущий месяц в других таблицах
                    // или использовать какой-то запасной вариант расчета
                }

                // Формула для производственного подразделения: (Сумма планов перемещений предыдущего месяца × Норматив текущего месяца) ÷ 30
                decimal calculatedValue = (transferPlanQuantity * stockNorm) / 30;
                string formula = $"({transferPlanQuantity} (план перемещений за {previousMonthStart.ToString("MMMM yyyy")}) × {stockNorm}) ÷ 30 = {calculatedValue:F2}";

                // Формируем результат
                var result = new InventoryCalculationResult
                {
                    Date = monthStart,
                    InventoryPlan = calculatedValue,
                    SalesPlan = 0, // Для производственного подразделения план продаж не используется
                    TransferPlan = transferPlanQuantity, // Возвращаем сумму планов перемещений предыдущего месяца для информации
                    StockNorm = stockNorm,
                    DaysInMonth = DateTime.DaysInMonth(request.Date.Year, request.Date.Month),
                    CalculatedQuantity = calculatedValue,
                    IsFixedPlan = false,
                    CalculationType = "Расчет для производственного подразделения",
                    Formula = formula,
                    SubdivisionName = subdivision.Name,
                    MaterialName = material.Name,
                    Message = $"Расчет выполнен по формуле: {formula}. Использованы планы перемещений за предыдущий месяц ({previousMonthStart.ToString("MMMM yyyy")})"
                };

                _logger.LogInformation("Расчет для производственного подразделения выполнен: {Formula}", formula);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете плана запасов для производственного подразделения");
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при расчете плана запасов",
                    error = ex.Message,
                    details = ex.StackTrace
                });
            }
        }

        // ============================================
        // 4. ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (с небольшими улучшениями)
        // ============================================

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "InventoryPlansController доступен",
                timestamp = DateTime.Now
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryPlanResponseDto>>> GetInventoryPlans()
        {
            try
            {
                var inventoryPlans = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .OrderByDescending(ip => ip.Date)
                    .ThenBy(ip => ip.Subdivision.Name)
                    .ThenBy(ip => ip.Material.Name)
                    .ToListAsync();

                var result = inventoryPlans.Select(ip => new InventoryPlanResponseDto
                {
                    Id = ip.Id,
                    SubdivisionName = ip.Subdivision?.Name ?? "Неизвестно",
                    MaterialName = ip.Material?.Name ?? "Неизвестно",
                    Date = ip.Date,
                    Quantity = ip.Quantity
                }).ToList();

                _logger.LogInformation("Получено {Count} планов запасов", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении планов запасов");
                return StatusCode(500, new { message = "Ошибка сервера при получении планов запасов", error = ex.Message });
            }
        }

        [HttpGet("subdivisions")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubdivisions()
        {
            try
            {
                var subdivisions = await _context.Subdivisions
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        Type = s.Type.ToString()
                    })
                    .ToListAsync();

                _logger.LogInformation("Получено {Count} подразделений", subdivisions.Count);
                return Ok(subdivisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении подразделений");
                return StatusCode(500, new { message = "Ошибка сервера при получении подразделений", error = ex.Message });
            }
        }

        [HttpGet("materials")]
        public async Task<ActionResult<IEnumerable<object>>> GetMaterials()
        {
            try
            {
                var materials = await _context.Materials
                    .OrderBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        Type = m.Type.ToString()
                    })
                    .ToListAsync();

                _logger.LogInformation("Получено {Count} материалов", materials.Count);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении материалов");
                return StatusCode(500, new { message = "Ошибка сервера при получении материалов", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<InventoryPlanResponseDto>> CreateInventoryPlan(
            [FromBody] InventoryPlanCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("Создание плана запасов: SubdivisionId={SubdivisionId}, MaterialId={MaterialId}, Date={Date}, Quantity={Quantity}",
                    createDto.SubdivisionId, createDto.MaterialId, createDto.Date, createDto.Quantity);

                // Проверяем, существует ли уже план на эту дату
                var existing = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == createDto.SubdivisionId &&
                                               ip.MaterialId == createDto.MaterialId &&
                                               ip.Date == createDto.Date);

                if (existing != null)
                    return BadRequest(new { message = "План запасов на этот месяц уже существует для данного подразделения и материала" });

                // Проверяем существование подразделения и материала
                var subdivision = await _context.Subdivisions.FindAsync(createDto.SubdivisionId);
                if (subdivision == null)
                    return NotFound(new { message = $"Подразделение с ID {createDto.SubdivisionId} не найдено" });

                var material = await _context.Materials.FindAsync(createDto.MaterialId);
                if (material == null)
                    return NotFound(new { message = $"Материал с ID {createDto.MaterialId} не найден" });

                var inventoryPlan = new InventoryPlan
                {
                    SubdivisionId = createDto.SubdivisionId,
                    MaterialId = createDto.MaterialId,
                    Date = createDto.Date,
                    Quantity = createDto.Quantity
                };

                _context.InventoryPlans.Add(inventoryPlan);
                await _context.SaveChangesAsync();

                // Загружаем связанные данные для ответа
                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Subdivision)
                    .LoadAsync();
                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Material)
                    .LoadAsync();

                var result = new InventoryPlanResponseDto
                {
                    Id = inventoryPlan.Id,
                    SubdivisionName = inventoryPlan.Subdivision?.Name ?? "Неизвестно",
                    MaterialName = inventoryPlan.Material?.Name ?? "Неизвестно",
                    Date = inventoryPlan.Date,
                    Quantity = inventoryPlan.Quantity
                };

                _logger.LogInformation("План запасов создан успешно, ID={Id}", result.Id);
                return CreatedAtAction(nameof(GetInventoryPlans), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании плана запасов");
                return StatusCode(500, new { message = "Ошибка сервера при создании плана запасов", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InventoryPlanResponseDto>> UpdateInventoryPlan(int id,
            [FromBody] InventoryPlanUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("Обновление плана запасов ID={Id}", id);

                var inventoryPlan = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .FirstOrDefaultAsync(ip => ip.Id == id);

                if (inventoryPlan == null)
                    return NotFound(new { message = $"План запасов с ID {id} не найден" });

                // Обновляем только те поля, которые переданы
                if (updateDto.SubdivisionId.HasValue)
                {
                    var subdivision = await _context.Subdivisions.FindAsync(updateDto.SubdivisionId.Value);
                    if (subdivision == null)
                        return NotFound(new { message = $"Подразделение с ID {updateDto.SubdivisionId} не найдено" });
                    inventoryPlan.SubdivisionId = updateDto.SubdivisionId.Value;
                }

                if (updateDto.MaterialId.HasValue)
                {
                    var material = await _context.Materials.FindAsync(updateDto.MaterialId.Value);
                    if (material == null)
                        return NotFound(new { message = $"Материал с ID {updateDto.MaterialId} не найден" });
                    inventoryPlan.MaterialId = updateDto.MaterialId.Value;
                }

                if (updateDto.Date.HasValue)
                    inventoryPlan.Date = updateDto.Date.Value;

                if (updateDto.Quantity.HasValue)
                    inventoryPlan.Quantity = updateDto.Quantity.Value;

                // Проверяем уникальность после обновления
                var duplicate = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.Id != id &&
                                               ip.SubdivisionId == inventoryPlan.SubdivisionId &&
                                               ip.MaterialId == inventoryPlan.MaterialId &&
                                               ip.Date == inventoryPlan.Date);

                if (duplicate != null)
                    return BadRequest(new { message = "План запасов с такими параметрами уже существует" });

                await _context.SaveChangesAsync();

                // Загружаем обновленные связанные данные
                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Subdivision)
                    .LoadAsync();
                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Material)
                    .LoadAsync();

                var result = new InventoryPlanResponseDto
                {
                    Id = inventoryPlan.Id,
                    SubdivisionName = inventoryPlan.Subdivision?.Name ?? "Неизвестно",
                    MaterialName = inventoryPlan.Material?.Name ?? "Неизвестно",
                    Date = inventoryPlan.Date,
                    Quantity = inventoryPlan.Quantity
                };

                _logger.LogInformation("План запасов ID={Id} успешно обновлен", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении плана запасов ID={Id}", id);
                return StatusCode(500, new { message = "Ошибка сервера при обновлении плана запасов", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryPlan(int id)
        {
            try
            {
                _logger.LogInformation("Удаление плана запасов ID={Id}", id);

                var inventoryPlan = await _context.InventoryPlans.FindAsync(id);

                if (inventoryPlan == null)
                    return NotFound(new { message = $"План запасов с ID {id} не найден" });

                _context.InventoryPlans.Remove(inventoryPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("План запасов ID={Id} успешно удален", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении плана запасов ID={Id}", id);
                return StatusCode(500, new { message = "Ошибка сервера при удалении плана запасов", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryPlanResponseDto>> GetInventoryPlanById(int id)
        {
            try
            {
                _logger.LogInformation("Получение плана запасов по ID={Id}", id);

                var inventoryPlan = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .FirstOrDefaultAsync(ip => ip.Id == id);

                if (inventoryPlan == null)
                    return NotFound(new { message = $"План запасов с ID {id} не найден" });

                var result = new InventoryPlanResponseDto
                {
                    Id = inventoryPlan.Id,
                    SubdivisionName = inventoryPlan.Subdivision?.Name ?? "Неизвестно",
                    MaterialName = inventoryPlan.Material?.Name ?? "Неизвестно",
                    Date = inventoryPlan.Date,
                    Quantity = inventoryPlan.Quantity
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении плана запасов по ID={Id}", id);
                return StatusCode(500, new { message = "Ошибка сервера при получении плана запасов", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<InventoryPlanResponseDto>>> SearchInventoryPlans(
            [FromQuery] int? subdivisionId,
            [FromQuery] int? materialId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("Поиск планов запасов: SubdivisionId={SubdivisionId}, MaterialId={MaterialId}, StartDate={StartDate}, EndDate={EndDate}",
                    subdivisionId, materialId, startDate, endDate);

                var query = _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .AsQueryable();

                if (subdivisionId.HasValue)
                    query = query.Where(ip => ip.SubdivisionId == subdivisionId.Value);

                if (materialId.HasValue)
                    query = query.Where(ip => ip.MaterialId == materialId.Value);

                if (startDate.HasValue)
                    query = query.Where(ip => ip.Date >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(ip => ip.Date <= endDate.Value);

                var inventoryPlans = await query
                    .OrderByDescending(ip => ip.Date)
                    .ThenBy(ip => ip.Subdivision.Name)
                    .ThenBy(ip => ip.Material.Name)
                    .ToListAsync();

                var result = inventoryPlans.Select(ip => new InventoryPlanResponseDto
                {
                    Id = ip.Id,
                    SubdivisionName = ip.Subdivision?.Name ?? "Неизвестно",
                    MaterialName = ip.Material?.Name ?? "Неизвестно",
                    Date = ip.Date,
                    Quantity = ip.Quantity
                }).ToList();

                _logger.LogInformation("Найдено {Count} планов запасов по критериям поиска", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске планов запасов");
                return StatusCode(500, new { message = "Ошибка сервера при поиске планов запасов", error = ex.Message });
            }
        }
    }
}