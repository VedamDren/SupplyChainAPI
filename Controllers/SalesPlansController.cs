using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.SalesPlanDTO;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SalesPlansController> _logger;

        public SalesPlansController(SupplyChainContext context, IMapper mapper, ILogger<SalesPlansController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ============================================
        // ПОЛУЧЕНИЕ ВСЕХ ПЛАНОВ ПРОДАЖ
        // ============================================
        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<SalesPlanResponseDto>>> GetAllSalesPlans()
        {
            try
            {
                _logger.LogInformation("Запрос на получение всех планов продаж");

                // Загружаем все планы продаж с связанными данными
                var salesPlans = await _context.SalesPlans
                    .Include(sp => sp.Subdivision)           // Подразделение
                    .Include(sp => sp.Material)              // Материал
                    .Include(sp => sp.CreatedByUser)         // Кто создал (если есть)
                    .Include(sp => sp.LastModifiedByUser)    // Кто последний редактировал (если есть)
                    .OrderByDescending(sp => sp.Date)        // Сортируем по дате
                    .ToListAsync();

                _logger.LogInformation($"Получено {salesPlans.Count} планов продаж");

                // Преобразуем в DTO и возвращаем
                return Ok(_mapper.Map<List<SalesPlanResponseDto>>(salesPlans));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех планов продаж");

                // Логирование ошибки
                return StatusCode(500, new
                {
                    message = "Ошибка при получении планов продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // ============================================
        // ПОЛУЧЕНИЕ ПЛАНА ПРОДАЖ ПО ID
        // ============================================
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesPlanResponseDto>> GetSalesPlan(int id)
        {
            try
            {
                _logger.LogInformation($"Запрос плана продаж с ID: {id}");

                // Ищем план продаж с загрузкой всех связанных данных
                var salesPlan = await _context.SalesPlans
                    .Include(sp => sp.Subdivision)           // Подразделение
                    .Include(sp => sp.Material)              // Материал
                    .Include(sp => sp.CreatedByUser)         // Кто создал
                    .Include(sp => sp.LastModifiedByUser)    // Кто последний редактировал
                    .FirstOrDefaultAsync(sp => sp.Id == id);

                // Если не найден - 404
                if (salesPlan == null)
                {
                    _logger.LogWarning($"План продаж с ID {id} не найден");
                    return NotFound(new { message = $"План продаж с ID {id} не найден" });
                }

                _logger.LogInformation($"План продаж с ID {id} успешно получен");

                // Преобразуем в DTO и возвращаем
                return Ok(_mapper.Map<SalesPlanResponseDto>(salesPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении плана продаж с ID: {id}");

                return StatusCode(500, new
                {
                    message = "Ошибка при получении плана продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // ============================================
        // СОЗДАНИЕ НОВОГО ПЛАНА ПРОДАЖ
        // ============================================
        [HttpPost]
        public async Task<ActionResult<SalesPlanResponseDto>> CreateSalesPlan(
            [FromBody] SalesPlanCreateDto salesPlanCreateDto)
        {
            try
            {
                _logger.LogInformation($"Создание нового плана продаж: SubdivisionId={salesPlanCreateDto.SubdivisionId}, MaterialId={salesPlanCreateDto.MaterialId}, Date={salesPlanCreateDto.Date}");

                // Проверка на уникальность комбинации: Подразделение + Материал + Дата
                var existingPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp =>
                        sp.SubdivisionId == salesPlanCreateDto.SubdivisionId &&
                        sp.MaterialId == salesPlanCreateDto.MaterialId &&
                        sp.Date == salesPlanCreateDto.Date);

                if (existingPlan != null)
                {
                    _logger.LogWarning($"Попытка создания дубликата плана продаж: SubdivisionId={salesPlanCreateDto.SubdivisionId}, MaterialId={salesPlanCreateDto.MaterialId}, Date={salesPlanCreateDto.Date}");
                    return BadRequest(new { message = "План продаж с такими параметрами уже существует" });
                }

                // Проверяем существование связанных сущностей
                var subdivisionExists = await _context.Subdivisions.AnyAsync(s => s.Id == salesPlanCreateDto.SubdivisionId);
                if (!subdivisionExists)
                {
                    return BadRequest(new
                    {
                        message = $"Подразделение с ID {salesPlanCreateDto.SubdivisionId} не существует"
                    });
                }

                var materialExists = await _context.Materials.AnyAsync(m => m.Id == salesPlanCreateDto.MaterialId);
                if (!materialExists)
                {
                    return BadRequest(new
                    {
                        message = $"Материал с ID {salesPlanCreateDto.MaterialId} не существует"
                    });
                }

                // Создаем объект из DTO
                var salesPlan = _mapper.Map<SalesPlan>(salesPlanCreateDto);

                // Устанавливаем даты создания и изменения (маппер игнорирует эти поля)
                salesPlan.CreatedDate = DateTime.UtcNow;
                salesPlan.LastModifiedDate = DateTime.UtcNow;

                // Сохраняем в БД
                _context.SalesPlans.Add(salesPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Создан новый план продаж с ID: {salesPlan.Id}");

                // Загружаем связанные данные для возврата в ответе
                await _context.Entry(salesPlan)
                    .Reference(sp => sp.Subdivision).LoadAsync();
                await _context.Entry(salesPlan)
                    .Reference(sp => sp.Material).LoadAsync();
                await _context.Entry(salesPlan)
                    .Reference(sp => sp.CreatedByUser).LoadAsync();
                await _context.Entry(salesPlan)
                    .Reference(sp => sp.LastModifiedByUser).LoadAsync();

                // Возвращаем созданный объект с кодом 201 Created
                return CreatedAtAction(
                    nameof(GetSalesPlan),
                    new { id = salesPlan.Id },
                    _mapper.Map<SalesPlanResponseDto>(salesPlan)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании плана продаж");

                return StatusCode(500, new
                {
                    message = "Ошибка при создании плана продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // ============================================
        // ОБНОВЛЕНИЕ СУЩЕСТВУЮЩЕГО ПЛАНА ПРОДАЖ
        // ============================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalesPlan(
            int id,
            [FromBody] SalesPlanUpdateDto salesPlanUpdateDto)
        {
            try
            {
                _logger.LogInformation($"Обновление плана продаж с ID: {id}");

                // Находим существующий план продаж
                var salesPlan = await _context.SalesPlans.FindAsync(id);
                if (salesPlan == null)
                {
                    _logger.LogWarning($"План продаж с ID {id} не найден для обновления");
                    return NotFound(new { message = $"План продаж с ID {id} не найден" });
                }

                // Проверка на уникальность при обновлении
                if (salesPlanUpdateDto.SubdivisionId.HasValue ||
                    salesPlanUpdateDto.MaterialId.HasValue ||
                    salesPlanUpdateDto.Date.HasValue)
                {
                    // Определяем новые значения или оставляем старые
                    var subdivisionId = salesPlanUpdateDto.SubdivisionId ?? salesPlan.SubdivisionId;
                    var materialId = salesPlanUpdateDto.MaterialId ?? salesPlan.MaterialId;
                    var date = salesPlanUpdateDto.Date ?? salesPlan.Date;

                    // Ищем другой план с такими же параметрами
                    var existingPlan = await _context.SalesPlans
                        .FirstOrDefaultAsync(sp =>
                            sp.Id != id && // Исключаем текущий план
                            sp.SubdivisionId == subdivisionId &&
                            sp.MaterialId == materialId &&
                            sp.Date == date);

                    if (existingPlan != null)
                    {
                        _logger.LogWarning($"Найден дубликат плана продаж при обновлении ID: {id}");
                        return BadRequest(new { message = "План продаж с такими параметрами уже существует" });
                    }
                }

                // Проверяем существование связанных сущностей, если они обновляются
                if (salesPlanUpdateDto.SubdivisionId.HasValue)
                {
                    var subdivisionExists = await _context.Subdivisions.AnyAsync(s => s.Id == salesPlanUpdateDto.SubdivisionId.Value);
                    if (!subdivisionExists)
                    {
                        return BadRequest(new
                        {
                            message = $"Подразделение с ID {salesPlanUpdateDto.SubdivisionId} не существует"
                        });
                    }
                }

                if (salesPlanUpdateDto.MaterialId.HasValue)
                {
                    var materialExists = await _context.Materials.AnyAsync(m => m.Id == salesPlanUpdateDto.MaterialId.Value);
                    if (!materialExists)
                    {
                        return BadRequest(new
                        {
                            message = $"Материал с ID {salesPlanUpdateDto.MaterialId} не существует"
                        });
                    }
                }

                // Применяем обновления из DTO
                _mapper.Map(salesPlanUpdateDto, salesPlan);

                // Обновляем дату последнего изменения
                salesPlan.LastModifiedDate = DateTime.UtcNow;

                // Сохраняем изменения
                await _context.SaveChangesAsync();

                _logger.LogInformation($"План продаж с ID {id} успешно обновлен");

                // Возвращаем код 204 No Content (успешное обновление без возврата данных)
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении плана продаж с ID: {id}");

                return StatusCode(500, new
                {
                    message = "Ошибка при обновлении плана продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // ============================================
        // СОЗДАНИЕ ИЛИ ОБНОВЛЕНИЕ МЕСЯЧНОГО ПЛАНА ПРОДАЖ (UPSERT) - ИСПРАВЛЕННАЯ ВЕРСИЯ
        // ============================================
        [HttpPost("upsertMonthly")]
        public async Task<ActionResult<SalesPlanResponseDto>> UpsertMonthlySalesPlan(
            [FromBody] SalesPlanUpsertInputDto inputDto)
        {
            try
            {
                _logger.LogInformation($"Получен UPSERT запрос: SubdivisionId={inputDto.SubdivisionId}, MaterialId={inputDto.MaterialId}, MonthKey={inputDto.MonthKey}, Quantity={inputDto.Quantity}");

                // ПРЕОБРАЗОВАНИЕ: Преобразуем monthKey (формат "YYYY-MM") в DateTime
                DateTime firstDayOfMonth;

                if (DateTime.TryParseExact(inputDto.MonthKey, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    firstDayOfMonth = new DateTime(parsedDate.Year, parsedDate.Month, 1);
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Неверный формат MonthKey",
                        receivedValue = inputDto.MonthKey,
                        suggestion = "Используйте формат YYYY-MM (например, 2023-03)"
                    });
                }

                // Получаем ID текущего пользователя из контекста (предполагается, что у вас есть аутентификация)
                var currentUserId = GetCurrentUserId(); // Этот метод нужно реализовать

                // Если нет аутентификации, можно использовать значение по умолчанию
                // или передавать userId в DTO. Для примера, предположим, что userId=1
                int? userId = currentUserId ?? 1;

                // Проверяем существование связанных сущностей
                var subdivisionExists = await _context.Subdivisions.AnyAsync(s => s.Id == inputDto.SubdivisionId);
                if (!subdivisionExists)
                {
                    return BadRequest(new
                    {
                        message = $"Подразделение с ID {inputDto.SubdivisionId} не найдено"
                    });
                }

                var materialExists = await _context.Materials.AnyAsync(m => m.Id == inputDto.MaterialId);
                if (!materialExists)
                {
                    return BadRequest(new
                    {
                        message = $"Материал с ID {inputDto.MaterialId} не найден"
                    });
                }

                // Ищем существующую запись за этот месяц
                var existingPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp =>
                        sp.SubdivisionId == inputDto.SubdivisionId &&
                        sp.MaterialId == inputDto.MaterialId &&
                        sp.Date == firstDayOfMonth);

                if (existingPlan != null)
                {
                    // ОБНОВЛЕНИЕ существующей записи
                    _logger.LogInformation($"Найдена существующая запись с ID {existingPlan.Id}. Обновляем...");

                    // Обновляем поля
                    existingPlan.Quantity = inputDto.Quantity; // Теперь типы совпадают
                    existingPlan.LastModifiedDate = DateTime.UtcNow;
                    existingPlan.LastModifiedByUserId = userId;

                    // Если нужно обновить информацию о подготовившем
                    if (!string.IsNullOrEmpty(existingPlan.PreparedByInfo))
                    {
                        // Добавляем информацию о последнем редакторе
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            existingPlan.PreparedByInfo = $"{existingPlan.PreparedByInfo}; Редактор: {user.Name} ({DateTime.Now:dd.MM.yyyy})";
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Запись {existingPlan.Id} успешно обновлена");

                    // Загружаем связанные данные для возврата
                    await _context.Entry(existingPlan)
                        .Reference(sp => sp.Subdivision).LoadAsync();
                    await _context.Entry(existingPlan)
                        .Reference(sp => sp.Material).LoadAsync();
                    await _context.Entry(existingPlan)
                        .Reference(sp => sp.CreatedByUser).LoadAsync();
                    await _context.Entry(existingPlan)
                        .Reference(sp => sp.LastModifiedByUser).LoadAsync();

                    return Ok(_mapper.Map<SalesPlanResponseDto>(existingPlan));
                }
                else
                {
                    // СОЗДАНИЕ новой записи
                    _logger.LogInformation("Создаем новую запись...");

                    var newSalesPlan = new SalesPlan
                    {
                        SubdivisionId = inputDto.SubdivisionId,
                        MaterialId = inputDto.MaterialId,
                        Date = firstDayOfMonth,
                        Quantity = inputDto.Quantity, // Теперь типы совпадают
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow,
                        CreatedByUserId = userId,
                        LastModifiedByUserId = userId,
                        // Подготовка информации о пользователе
                        PreparedByInfo = PrepareUserInfo(userId)
                    };

                    _context.SalesPlans.Add(newSalesPlan);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Создана новая запись с ID {newSalesPlan.Id}");

                    // Загружаем связанные данные для возврата
                    await _context.Entry(newSalesPlan)
                        .Reference(sp => sp.Subdivision).LoadAsync();
                    await _context.Entry(newSalesPlan)
                        .Reference(sp => sp.Material).LoadAsync();
                    await _context.Entry(newSalesPlan)
                        .Reference(sp => sp.CreatedByUser).LoadAsync();
                    await _context.Entry(newSalesPlan)
                        .Reference(sp => sp.LastModifiedByUser).LoadAsync();

                    return CreatedAtAction(
                        nameof(GetSalesPlan),
                        new { id = newSalesPlan.Id },
                        _mapper.Map<SalesPlanResponseDto>(newSalesPlan)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка в методе UpsertMonthlySalesPlan: {ex.Message}");
                return BadRequest(new
                {
                    message = "Ошибка при сохранении плана продаж",
                    error = ex.Message
                });
            }
        }

        // Вспомогательный метод для получения ID текущего пользователя
        private int? GetCurrentUserId()
        {
            // Реализация зависит от вашей системы аутентификации
            // Например, если используется JWT с Claim "UserId":
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            // Или если используется Identity:
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return null; // или вернуть значение по умолчанию
        }

        // Вспомогательный метод для подготовки информации о пользователе
        private string PrepareUserInfo(int? userId)
        {
            if (!userId.HasValue)
                return "Неизвестный пользователь";

            // Здесь можно получить информацию о пользователе из БД
            // или использовать кэшированные данные
            return $"Пользователь ID: {userId.Value}";
        }

        // ============================================
        // УДАЛЕНИЕ ПЛАНА ПРОДАЖ
        // ============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalesPlan(int id)
        {
            try
            {
                _logger.LogInformation($"Удаление плана продаж с ID: {id}");

                // Находим план продаж
                var salesPlan = await _context.SalesPlans.FindAsync(id);
                if (salesPlan == null)
                {
                    _logger.LogWarning($"План продаж с ID {id} не найден для удаления");
                    return NotFound(new { message = $"План продаж с ID {id} не найден" });
                }

                // Удаляем из БД
                _context.SalesPlans.Remove(salesPlan);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"План продаж с ID {id} успешно удален");

                // Возвращаем код 204 No Content (успешное удаление)
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении плана продаж с ID: {id}");

                return StatusCode(500, new
                {
                    message = "Ошибка при удалении плана продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЙ МЕТОД: ПОЛУЧЕНИЕ ПЛАНОВ ПО ПАРАМЕТРАМ
        // ============================================
        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<SalesPlanResponseDto>>> SearchSalesPlans(
            [FromBody] SalesPlanSearchDto searchDto)
        {
            try
            {
                _logger.LogInformation($"Поиск планов продаж с параметрами: SubdivisionId={searchDto.SubdivisionId}, MaterialId={searchDto.MaterialId}, StartDate={searchDto.StartDate}, EndDate={searchDto.EndDate}");

                // Начинаем с базового запроса
                var query = _context.SalesPlans.AsQueryable();

                // Применяем фильтры, если они указаны
                if (searchDto.SubdivisionId.HasValue)
                    query = query.Where(sp => sp.SubdivisionId == searchDto.SubdivisionId.Value);

                if (searchDto.MaterialId.HasValue)
                    query = query.Where(sp => sp.MaterialId == searchDto.MaterialId.Value);

                if (searchDto.StartDate.HasValue)
                    query = query.Where(sp => sp.Date >= searchDto.StartDate.Value);

                if (searchDto.EndDate.HasValue)
                    query = query.Where(sp => sp.Date <= searchDto.EndDate.Value);

                // Загружаем связанные данные
                query = query
                    .Include(sp => sp.Subdivision)
                    .Include(sp => sp.Material)
                    .Include(sp => sp.CreatedByUser)
                    .Include(sp => sp.LastModifiedByUser);

                // Сортируем по дате
                query = query.OrderByDescending(sp => sp.Date);

                // Выполняем запрос
                var salesPlans = await query.ToListAsync();

                _logger.LogInformation($"Найдено {salesPlans.Count} записей по заданным критериям");

                // Преобразуем в DTO и возвращаем
                return Ok(_mapper.Map<List<SalesPlanResponseDto>>(salesPlans));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске планов продаж");

                return StatusCode(500, new
                {
                    message = "Ошибка при поиске планов продаж",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
    }
}