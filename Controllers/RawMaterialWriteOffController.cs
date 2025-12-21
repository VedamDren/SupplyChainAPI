using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainMathLib;
using AutoMapper;
using System.Net;
using System.Linq;
using System.Globalization;
using SupplyChainAPI.Models.RawMaterialWriteOff;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawMaterialWriteOffsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RawMaterialWriteOffsController> _logger;

        public RawMaterialWriteOffsController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<RawMaterialWriteOffsController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/RawMaterialWriteOffs
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialWriteOffResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<RawMaterialWriteOffResponseDto>>> GetRawMaterialWriteOffs()
        {
            try
            {
                var writeOffs = await _context.RawMaterialWriteOffs
                    .Include(w => w.Subdivision)
                    .Include(w => w.RawMaterial)
                    .ToListAsync();

                var writeOffDtos = _mapper.Map<List<RawMaterialWriteOffResponseDto>>(writeOffs);
                return Ok(writeOffDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material write-offs");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/RawMaterialWriteOffs/GetAll
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialWriteOffResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<RawMaterialWriteOffResponseDto>>> GetAllRawMaterialWriteOffs()
        {
            try
            {
                var writeOffs = await _context.RawMaterialWriteOffs
                    .Include(w => w.Subdivision)
                    .Include(w => w.RawMaterial)
                    .ToListAsync();

                var writeOffDtos = _mapper.Map<List<RawMaterialWriteOffResponseDto>>(writeOffs);
                return Ok(writeOffDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material write-offs via POST");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/RawMaterialWriteOffs/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RawMaterialWriteOffResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<RawMaterialWriteOffResponseDto>> GetRawMaterialWriteOff(int id)
        {
            try
            {
                var writeOff = await _context.RawMaterialWriteOffs
                    .Include(w => w.Subdivision)
                    .Include(w => w.RawMaterial)
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (writeOff == null)
                {
                    return NotFound();
                }

                var writeOffDto = _mapper.Map<RawMaterialWriteOffResponseDto>(writeOff);
                return Ok(writeOffDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material write-off with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/RawMaterialWriteOffs
        [HttpPost]
        [ProducesResponseType(typeof(RawMaterialWriteOffResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<RawMaterialWriteOffResponseDto>> CreateRawMaterialWriteOff(RawMaterialWriteOffCreateDto writeOffCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var writeOff = _mapper.Map<RawMaterialWriteOff>(writeOffCreateDto);

                _context.RawMaterialWriteOffs.Add(writeOff);
                await _context.SaveChangesAsync();

                // Load related data for response
                await _context.Entry(writeOff)
                    .Reference(w => w.Subdivision)
                    .LoadAsync();

                await _context.Entry(writeOff)
                    .Reference(w => w.RawMaterial)
                    .LoadAsync();

                var writeOffDto = _mapper.Map<RawMaterialWriteOffResponseDto>(writeOff);

                return CreatedAtAction(nameof(GetRawMaterialWriteOff), new { id = writeOffDto.Id }, writeOffDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating raw material write-off");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/RawMaterialWriteOffs/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateRawMaterialWriteOff(int id, RawMaterialWriteOffUpdateDto writeOffUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var writeOff = await _context.RawMaterialWriteOffs.FindAsync(id);
                if (writeOff == null)
                {
                    return NotFound();
                }

                _mapper.Map(writeOffUpdateDto, writeOff);
                _context.Entry(writeOff).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!RawMaterialWriteOffExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating raw material write-off with id: {Id}", id);
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating raw material write-off with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/RawMaterialWriteOffs/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteRawMaterialWriteOff(int id)
        {
            try
            {
                var writeOff = await _context.RawMaterialWriteOffs.FindAsync(id);
                if (writeOff == null)
                {
                    return NotFound();
                }

                _context.RawMaterialWriteOffs.Remove(writeOff);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting raw material write-off with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Расчет плана списания сырья по формуле: сумма планов производства по всем материалам за месяц
        /// </summary>
        [HttpPost("Calculate")]
        [ProducesResponseType(typeof(RawMaterialWriteOffCalculationResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<RawMaterialWriteOffCalculationResultDto>> CalculateWriteOffPlan(
            [FromBody] RawMaterialWriteOffCalculationDto request)
        {
            try
            {
                _logger.LogInformation("Received calculation request: Month={Month}, Year={Year}, SubdivisionId={SubdivisionId}, RawMaterialId={RawMaterialId}",
                    request?.Month, request?.Year, request?.SubdivisionId, request?.RawMaterialId);

                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid request",
                        Detail = "Request body is required",
                        Status = 400
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Model validation errors: {Errors}", string.Join(", ", errors));

                    return ValidationProblem(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Validation failed",
                        Detail = "Please check the request parameters",
                        Status = 400
                    });
                }

                // Валидация входных данных
                if (request.Month < 1 || request.Month > 12)
                {
                    _logger.LogWarning("Invalid month: {Month}", request.Month);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "Month must be between 1 and 12",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["month"] = new[] { "Month must be between 1 and 12" }
                        }
                    });
                }

                if (request.Year < 2023 || request.Year > 2100)
                {
                    _logger.LogWarning("Invalid year: {Year}", request.Year);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "Year must be between 2023 and 2100",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["year"] = new[] { "Year must be between 2023 and 2100" }
                        }
                    });
                }

                if (request.SubdivisionId <= 0)
                {
                    _logger.LogWarning("Invalid subdivisionId: {SubdivisionId}", request.SubdivisionId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "SubdivisionId must be a positive number",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["subdivisionId"] = new[] { "SubdivisionId must be a positive number" }
                        }
                    });
                }

                if (request.RawMaterialId <= 0)
                {
                    _logger.LogWarning("Invalid rawMaterialId: {RawMaterialId}", request.RawMaterialId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "RawMaterialId must be a positive number",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["rawMaterialId"] = new[] { "RawMaterialId must be a positive number" }
                        }
                    });
                }

                var targetMonth = new DateTime(request.Year, request.Month, 1);

                // Получаем подразделение
                var subdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Id == request.SubdivisionId);

                if (subdivision == null)
                {
                    _logger.LogWarning("Subdivision not found: {SubdivisionId}", request.SubdivisionId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = $"Subdivision with ID {request.SubdivisionId} not found",
                        Status = 400
                    });
                }

                // Получаем сырье
                var rawMaterial = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == request.RawMaterialId);

                if (rawMaterial == null)
                {
                    _logger.LogWarning("Raw material not found: {RawMaterialId}", request.RawMaterialId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = $"Raw material with ID {request.RawMaterialId} not found",
                        Status = 400
                    });
                }

                // Получаем все планы производства для подразделения за указанный месяц
                var productionPlans = await _context.ProductionPlans
                    .Include(pp => pp.Material)
                    .Where(pp => pp.SubdivisionId == request.SubdivisionId &&
                                 pp.Date.Year == targetMonth.Year &&
                                 pp.Date.Month == targetMonth.Month)
                    .ToListAsync();

                if (!productionPlans.Any())
                {
                    _logger.LogInformation("No production plans found for subdivision {SubdivisionId} in {Year}-{Month}",
                        request.SubdivisionId, request.Year, request.Month);

                    return Ok(new RawMaterialWriteOffCalculationResultDto
                    {
                        SubdivisionId = request.SubdivisionId,
                        SubdivisionName = subdivision.Name,
                        RawMaterialId = request.RawMaterialId,
                        RawMaterialName = rawMaterial.Name,
                        WriteOffDate = targetMonth,
                        CalculatedQuantity = 0,
                        CalculationFormula = "Нет планов производства на указанный месяц",
                        Note = $"Не найдены планы производства для подразделения '{subdivision.Name}' за {targetMonth:MMMM yyyy}"
                    });
                }

                // Используем математическую библиотеку для расчета
                var productionCalculator = new ProductionCalculator();
                var productionQuantities = productionPlans.Select(pp => (decimal)pp.Quantity).ToArray();
                decimal totalQuantity = productionCalculator.CalculateRawMaterialConsumption(productionQuantities);

                // Формируем детали расчета
                var productionPlanDetails = productionPlans.Select(pp => new ProductionPlanDetailDto
                {
                    MaterialId = pp.MaterialId,
                    MaterialName = pp.Material?.Name ?? "Неизвестный материал",
                    ProductionQuantity = pp.Quantity,
                    PlanDate = pp.Date  // Добавили дату плана
                }).ToList();

                // Формируем формулу
                var formulaParts = productionPlans.Select(pp => $"{pp.Quantity}");
                string formula = $"План списания = {string.Join(" + ", formulaParts)} = {totalQuantity}";

                var result = new RawMaterialWriteOffCalculationResultDto
                {
                    SubdivisionId = request.SubdivisionId,
                    SubdivisionName = subdivision.Name,
                    RawMaterialId = request.RawMaterialId,
                    RawMaterialName = rawMaterial.Name,
                    WriteOffDate = targetMonth,
                    CalculatedQuantity = totalQuantity,
                    ProductionPlans = productionPlanDetails,
                    CalculationFormula = formula,
                    Note = $"Расчет выполнен на основе {productionPlans.Count} планов производства за {targetMonth:MMMM yyyy}"
                };

                _logger.LogInformation("Calculation completed successfully: {Quantity} units", totalQuantity);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете плана списания сырья");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while calculating the write-off plan",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Расчет и сохранение плана списания сырья
        /// </summary>
        [HttpPost("CalculateAndSave")]
        [ProducesResponseType(typeof(RawMaterialWriteOffResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<RawMaterialWriteOffResponseDto>> CalculateAndSaveWriteOffPlan(
            [FromBody] RawMaterialWriteOffCalculationDto request)
        {
            try
            {
                _logger.LogInformation("CalculateAndSave request: Month={Month}, Year={Year}, SubdivisionId={SubdivisionId}, RawMaterialId={RawMaterialId}",
                    request?.Month, request?.Year, request?.SubdivisionId, request?.RawMaterialId);

                if (!ModelState.IsValid)
                {
                    return ValidationProblem(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Validation failed",
                        Detail = "Please check the request parameters",
                        Status = 400
                    });
                }

                // Сначала рассчитываем план
                var calculateResult = await CalculateWriteOffPlan(request);

                if (calculateResult.Result is OkObjectResult okResult && okResult.Value is RawMaterialWriteOffCalculationResultDto calculationResult)
                {
                    // Проверяем, что расчет был успешным и есть планы производства
                    if (calculationResult.CalculatedQuantity <= 0)
                    {
                        _logger.LogWarning("No production plans found for calculation");
                        return BadRequest(new ValidationProblemDetails
                        {
                            Title = "No data for calculation",
                            Detail = calculationResult.Note,
                            Status = 400
                        });
                    }

                    // Создаем запись списания
                    var writeOffCreateDto = new RawMaterialWriteOffCreateDto
                    {
                        SubdivisionId = calculationResult.SubdivisionId,
                        RawMaterialId = calculationResult.RawMaterialId,
                        WriteOffDate = calculationResult.WriteOffDate,
                        Quantity = (int)Math.Round(calculationResult.CalculatedQuantity, MidpointRounding.AwayFromZero),
                        IsCalculated = true,
                        CalculationNote = calculationResult.Note
                    };

                    var writeOff = _mapper.Map<RawMaterialWriteOff>(writeOffCreateDto);

                    _context.RawMaterialWriteOffs.Add(writeOff);
                    await _context.SaveChangesAsync();

                    // Загружаем связанные данные для ответа
                    await _context.Entry(writeOff)
                        .Reference(w => w.Subdivision)
                        .LoadAsync();

                    await _context.Entry(writeOff)
                        .Reference(w => w.RawMaterial)
                        .LoadAsync();

                    var writeOffDto = _mapper.Map<RawMaterialWriteOffResponseDto>(writeOff);

                    _logger.LogInformation("Calculated write-off saved with ID: {Id}, Quantity: {Quantity}",
                        writeOffDto.Id, writeOffDto.Quantity);

                    return Ok(writeOffDto);
                }
                else if (calculateResult.Result is ObjectResult errorResult)
                {
                    // Возвращаем ошибку из метода расчета
                    return StatusCode(errorResult.StatusCode ?? 400, errorResult.Value);
                }
                else
                {
                    _logger.LogWarning("Unexpected result type from CalculateWriteOffPlan");
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Calculation failed",
                        Detail = "Failed to calculate write-off plan",
                        Status = 400
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете и сохранении плана списания");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while calculating and saving the write-off plan",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Расчет годового плана списания сырья
        /// </summary>
        [HttpPost("CalculateYearly")]
        [ProducesResponseType(typeof(RawMaterialWriteOffYearlyCalculationResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<RawMaterialWriteOffYearlyCalculationResultDto>> CalculateYearlyWriteOffPlan(
            [FromBody] RawMaterialWriteOffYearlyCalculationDto request)
        {
            try
            {
                _logger.LogInformation("Received yearly calculation request: Year={Year}, SubdivisionId={SubdivisionId}, RawMaterialId={RawMaterialId}",
                    request?.Year, request?.SubdivisionId, request?.RawMaterialId);

                if (request == null)
                {
                    _logger.LogWarning("Yearly request body is null");
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid request",
                        Detail = "Request body is required",
                        Status = 400
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Yearly model validation errors: {Errors}", string.Join(", ", errors));

                    return ValidationProblem(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Validation failed",
                        Detail = "Please check the request parameters",
                        Status = 400
                    });
                }

                // Валидация входных данных
                if (request.Year < 2023 || request.Year > 2100)
                {
                    _logger.LogWarning("Invalid year: {Year}", request.Year);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "Year must be between 2023 and 2100",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["year"] = new[] { "Year must be between 2023 and 2100" }
                        }
                    });
                }

                if (request.SubdivisionId <= 0)
                {
                    _logger.LogWarning("Invalid subdivisionId: {SubdivisionId}", request.SubdivisionId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "SubdivisionId must be a positive number",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["subdivisionId"] = new[] { "SubdivisionId must be a positive number" }
                        }
                    });
                }

                if (request.RawMaterialId <= 0)
                {
                    _logger.LogWarning("Invalid rawMaterialId: {RawMaterialId}", request.RawMaterialId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameter",
                        Detail = "RawMaterialId must be a positive number",
                        Status = 400,
                        Errors = new Dictionary<string, string[]>
                        {
                            ["rawMaterialId"] = new[] { "RawMaterialId must be a positive number" }
                        }
                    });
                }

                // Получаем подразделение
                var subdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Id == request.SubdivisionId);

                if (subdivision == null)
                {
                    _logger.LogWarning("Subdivision not found: {SubdivisionId}", request.SubdivisionId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = $"Subdivision with ID {request.SubdivisionId} not found",
                        Status = 400
                    });
                }

                // Получаем сырье
                var rawMaterial = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == request.RawMaterialId);

                if (rawMaterial == null)
                {
                    _logger.LogWarning("Raw material not found: {RawMaterialId}", request.RawMaterialId);
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = $"Raw material with ID {request.RawMaterialId} not found",
                        Status = 400
                    });
                }

                // Получаем все планы производства за указанный год для подразделения
                var productionPlans = await _context.ProductionPlans
                    .Include(pp => pp.Material)
                    .Where(pp => pp.SubdivisionId == request.SubdivisionId &&
                                 pp.Date.Year == request.Year)
                    .ToListAsync();

                var monthlyResults = new List<MonthlyCalculationResultDto>();
                decimal totalYearlyQuantity = 0;
                int monthsWithData = 0;

                // Создаем culture для русского названия месяца
                var russianCulture = new CultureInfo("ru-RU");

                // Выполняем расчет для каждого месяца (1-12)
                for (int month = 1; month <= 12; month++)
                {
                    var targetMonth = new DateTime(request.Year, month, 1);
                    var monthName = targetMonth.ToString("MMMM", russianCulture);

                    // Фильтруем планы производства по месяцу
                    var monthProductionPlans = productionPlans
                        .Where(pp => pp.Date.Month == month)
                        .ToList();

                    if (!monthProductionPlans.Any())
                    {
                        monthlyResults.Add(new MonthlyCalculationResultDto
                        {
                            Month = month,
                            MonthName = monthName,
                            Year = request.Year,
                            CalculatedQuantity = 0,
                            ProductionPlansCount = 0,
                            CalculationFormula = $"Нет планов производства на {monthName.ToLower()}",
                            Note = $"Не найдены планы производства для подразделения '{subdivision.Name}' за {monthName} {request.Year}",
                            ProductionPlans = new List<ProductionPlanDetailDto>()
                        });
                        continue;
                    }

                    // Используем математическую библиотеку для расчета
                    var productionCalculator = new ProductionCalculator();
                    var productionQuantities = monthProductionPlans.Select(pp => (decimal)pp.Quantity).ToArray();
                    decimal monthQuantity = productionCalculator.CalculateRawMaterialConsumption(productionQuantities);

                    // Формируем детали расчета
                    var productionPlanDetails = monthProductionPlans.Select(pp => new ProductionPlanDetailDto
                    {
                        MaterialId = pp.MaterialId,
                        MaterialName = pp.Material?.Name ?? "Неизвестный материал",
                        ProductionQuantity = pp.Quantity,
                        PlanDate = pp.Date
                    }).ToList();

                    // Формируем формулу
                    var formulaParts = monthProductionPlans.Select(pp => $"{pp.Quantity}");
                    string formula = $"План списания = {string.Join(" + ", formulaParts)} = {monthQuantity}";

                    monthlyResults.Add(new MonthlyCalculationResultDto
                    {
                        Month = month,
                        MonthName = monthName,
                        Year = request.Year,
                        CalculatedQuantity = monthQuantity,
                        ProductionPlansCount = monthProductionPlans.Count,
                        CalculationFormula = formula,
                        Note = $"Расчет выполнен на основе {monthProductionPlans.Count} планов производства за {monthName} {request.Year}",
                        ProductionPlans = productionPlanDetails
                    });

                    totalYearlyQuantity += monthQuantity;
                    if (monthQuantity > 0)
                    {
                        monthsWithData++;
                    }
                }

                // Рассчитываем среднемесячное количество
                decimal averageMonthlyQuantity = monthsWithData > 0 ? totalYearlyQuantity / monthsWithData : 0;

                // Формируем сводку расчета
                string calculationSummary;
                if (monthsWithData == 0)
                {
                    calculationSummary = $"За {request.Year} год не найдено планов производства для подразделения '{subdivision.Name}'";
                }
                else if (monthsWithData == 12)
                {
                    calculationSummary = $"За {request.Year} год рассчитано списание по всем 12 месяцам";
                }
                else
                {
                    calculationSummary = $"За {request.Year} год рассчитано списание по {monthsWithData} месяцам из 12";
                }

                var result = new RawMaterialWriteOffYearlyCalculationResultDto
                {
                    SubdivisionId = request.SubdivisionId,
                    SubdivisionName = subdivision.Name,
                    RawMaterialId = request.RawMaterialId,
                    RawMaterialName = rawMaterial.Name,
                    Year = request.Year,
                    TotalQuantity = totalYearlyQuantity,
                    AverageMonthlyQuantity = averageMonthlyQuantity,
                    MonthlyResults = monthlyResults,
                    CalculationSummary = calculationSummary
                };

                _logger.LogInformation("Yearly calculation completed successfully: Year={Year}, Total={Total}, MonthsWithData={MonthsWithData}",
                    request.Year, totalYearlyQuantity, monthsWithData);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете годового плана списания сырья");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while calculating the yearly write-off plan",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Расчет и сохранение годового плана списания сырья
        /// </summary>
        [HttpPost("CalculateAndSaveYearly")]
        [ProducesResponseType(typeof(List<RawMaterialWriteOffResponseDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<List<RawMaterialWriteOffResponseDto>>> CalculateAndSaveYearlyWriteOffPlan(
            [FromBody] RawMaterialWriteOffYearlyCalculationDto request)
        {
            try
            {
                _logger.LogInformation("CalculateAndSaveYearly request: Year={Year}, SubdivisionId={SubdivisionId}, RawMaterialId={RawMaterialId}",
                    request?.Year, request?.SubdivisionId, request?.RawMaterialId);

                if (!ModelState.IsValid)
                {
                    return ValidationProblem(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Validation failed",
                        Detail = "Please check the request parameters",
                        Status = 400
                    });
                }

                // Сначала рассчитываем годовой план
                var calculateResult = await CalculateYearlyWriteOffPlan(request);

                if (calculateResult.Result is OkObjectResult okResult && okResult.Value is RawMaterialWriteOffYearlyCalculationResultDto calculationResult)
                {
                    // Проверяем, что есть данные для сохранения
                    if (calculationResult.TotalQuantity <= 0)
                    {
                        _logger.LogWarning("No production plans found for yearly calculation");
                        return BadRequest(new ValidationProblemDetails
                        {
                            Title = "No data for calculation",
                            Detail = calculationResult.CalculationSummary,
                            Status = 400
                        });
                    }

                    var writeOffsToSave = new List<SupplyChainData.RawMaterialWriteOff>();

                    // Создаем записи списания для месяцев с данными
                    foreach (var monthResult in calculationResult.MonthlyResults)
                    {
                        if (monthResult.CalculatedQuantity > 0)
                        {
                            var writeOffCreateDto = new RawMaterialWriteOffCreateDto
                            {
                                SubdivisionId = calculationResult.SubdivisionId,
                                RawMaterialId = calculationResult.RawMaterialId,
                                WriteOffDate = new DateTime(calculationResult.Year, monthResult.Month, 1),
                                Quantity = (int)Math.Round(monthResult.CalculatedQuantity, MidpointRounding.AwayFromZero),
                                IsCalculated = true,
                                CalculationNote = monthResult.Note
                            };

                            var writeOff = _mapper.Map<RawMaterialWriteOff>(writeOffCreateDto);
                            writeOffsToSave.Add(writeOff);
                        }
                    }

                    if (!writeOffsToSave.Any())
                    {
                        _logger.LogWarning("No monthly data to save");
                        return BadRequest(new ValidationProblemDetails
                        {
                            Title = "No monthly data",
                            Detail = "Не найдено данных по месяцам для сохранения",
                            Status = 400
                        });
                    }

                    _context.RawMaterialWriteOffs.AddRange(writeOffsToSave);
                    await _context.SaveChangesAsync();

                    // Загружаем связанные данные для ответа
                    foreach (var writeOff in writeOffsToSave)
                    {
                        await _context.Entry(writeOff)
                            .Reference(w => w.Subdivision)
                            .LoadAsync();

                        await _context.Entry(writeOff)
                            .Reference(w => w.RawMaterial)
                            .LoadAsync();
                    }

                    var writeOffDtos = _mapper.Map<List<RawMaterialWriteOffResponseDto>>(writeOffsToSave);

                    _logger.LogInformation("Yearly calculated write-offs saved successfully. Count: {Count}, Total Quantity: {TotalQuantity}",
                        writeOffsToSave.Count, writeOffsToSave.Sum(w => w.Quantity));

                    return Ok(writeOffDtos);
                }
                else if (calculateResult.Result is ObjectResult errorResult)
                {
                    // Возвращаем ошибку из метода расчета
                    return StatusCode(errorResult.StatusCode ?? 400, errorResult.Value);
                }
                else
                {
                    _logger.LogWarning("Unexpected result type from CalculateYearlyWriteOffPlan");
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Calculation failed",
                        Detail = "Failed to calculate yearly write-off plan",
                        Status = 400
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете и сохранении годового плана списания");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while calculating and saving the yearly write-off plan",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Получение расчетных списаний
        /// </summary>
        [HttpGet("Calculated")]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialWriteOffResponseDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<IEnumerable<RawMaterialWriteOffResponseDto>>> GetCalculatedWriteOffs()
        {
            try
            {
                var calculatedWriteOffs = await _context.RawMaterialWriteOffs
                    .Include(w => w.Subdivision)
                    .Include(w => w.RawMaterial)
                    .Where(w => w.IsCalculated)
                    .OrderByDescending(w => w.WriteOffDate)
                    .ToListAsync();

                var writeOffDtos = _mapper.Map<List<RawMaterialWriteOffResponseDto>>(calculatedWriteOffs);

                _logger.LogInformation("Retrieved {Count} calculated write-offs", calculatedWriteOffs.Count);
                return Ok(writeOffDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расчетных списаний");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while retrieving calculated write-offs",
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Получение расчетных списаний по фильтру (год, подразделение, сырье)
        /// </summary>
        [HttpGet("Calculated/Filter")]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialWriteOffResponseDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IEnumerable<RawMaterialWriteOffResponseDto>>> GetCalculatedWriteOffsByFilter(
            [FromQuery] int? year = null,
            [FromQuery] int? subdivisionId = null,
            [FromQuery] int? rawMaterialId = null)
        {
            try
            {
                // Валидация параметров
                var validationErrors = new Dictionary<string, string[]>();

                if (subdivisionId.HasValue && subdivisionId <= 0)
                {
                    validationErrors["subdivisionId"] = new[] { "SubdivisionId must be a positive number" };
                }

                if (rawMaterialId.HasValue && rawMaterialId <= 0)
                {
                    validationErrors["rawMaterialId"] = new[] { "RawMaterialId must be a positive number" };
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new ValidationProblemDetails
                    {
                        Title = "Invalid parameters",
                        Detail = "Please check the request parameters",
                        Status = 400,
                        Errors = validationErrors
                    });
                }

                var query = _context.RawMaterialWriteOffs
                    .Include(w => w.Subdivision)
                    .Include(w => w.RawMaterial)
                    .Where(w => w.IsCalculated);

                // Применяем фильтры
                if (year.HasValue)
                {
                    query = query.Where(w => w.WriteOffDate.Year == year.Value);
                }

                if (subdivisionId.HasValue)
                {
                    query = query.Where(w => w.SubdivisionId == subdivisionId.Value);
                }

                if (rawMaterialId.HasValue)
                {
                    query = query.Where(w => w.RawMaterialId == rawMaterialId.Value);
                }

                var calculatedWriteOffs = await query
                    .OrderByDescending(w => w.WriteOffDate)
                    .ToListAsync();

                var writeOffDtos = _mapper.Map<List<RawMaterialWriteOffResponseDto>>(calculatedWriteOffs);

                _logger.LogInformation("Retrieved {Count} calculated write-offs with filters: Year={Year}, SubdivisionId={SubdivisionId}, RawMaterialId={RawMaterialId}",
                    calculatedWriteOffs.Count, year, subdivisionId, rawMaterialId);

                return Ok(writeOffDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расчетных списаний по фильтру");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while retrieving calculated write-offs",
                    Status = 500
                });
            }
        }

        private bool RawMaterialWriteOffExists(int id)
        {
            return _context.RawMaterialWriteOffs.Any(e => e.Id == id);
        }
    }
}