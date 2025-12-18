using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.SupplySourceDTO;
using AutoMapper;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplySourcesController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public SupplySourcesController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<SupplySourceDto>>> GetAllSupplySources()
        {
            var supplySources = await _context.SupplySources
                .Include(ss => ss.SourceSubdivision)
                .Include(ss => ss.DestinationSubdivision)
                .Include(ss => ss.Material)
                .ToListAsync();

            return Ok(_mapper.Map<List<SupplySourceDto>>(supplySources));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupplySourceDto>> GetSupplySource(int id)
        {
            var supplySource = await _context.SupplySources
                .Include(ss => ss.SourceSubdivision)
                .Include(ss => ss.DestinationSubdivision)
                .Include(ss => ss.Material)
                .FirstOrDefaultAsync(ss => ss.Id == id);

            if (supplySource == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<SupplySourceDto>(supplySource));
        }

        [HttpPost]
        public async Task<ActionResult<SupplySourceDto>> CreateSupplySource(SupplySourceCreateDto supplySourceCreateDto)
        {
            // Проверка на уникальность комбинации SourceSubdivisionId, DestinationSubdivisionId, MaterialId и StartDate
            var existingSupplySource = await _context.SupplySources
                .FirstOrDefaultAsync(ss =>
                    ss.SourceSubdivisionId == supplySourceCreateDto.SourceSubdivisionId &&
                    ss.DestinationSubdivisionId == supplySourceCreateDto.DestinationSubdivisionId &&
                    ss.MaterialId == supplySourceCreateDto.MaterialId &&
                    ss.StartDate == supplySourceCreateDto.StartDate);

            if (existingSupplySource != null)
            {
                return BadRequest("Источник поставок с такими параметрами уже существует");
            }

            var supplySource = _mapper.Map<SupplySource>(supplySourceCreateDto);

            _context.SupplySources.Add(supplySource);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для возврата
            await _context.Entry(supplySource)
                .Reference(ss => ss.SourceSubdivision).LoadAsync();
            await _context.Entry(supplySource)
                .Reference(ss => ss.DestinationSubdivision).LoadAsync();
            await _context.Entry(supplySource)
                .Reference(ss => ss.Material).LoadAsync();

            return CreatedAtAction(nameof(GetSupplySource),
                new { id = supplySource.Id },
                _mapper.Map<SupplySourceDto>(supplySource));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplySource(int id, SupplySourceUpdateDto supplySourceUpdateDto)
        {
            var supplySource = await _context.SupplySources.FindAsync(id);
            if (supplySource == null)
            {
                return NotFound();
            }

            // Проверка на уникальность при обновлении
            if (supplySourceUpdateDto.SourceSubdivisionId.HasValue ||
                supplySourceUpdateDto.DestinationSubdivisionId.HasValue ||
                supplySourceUpdateDto.MaterialId.HasValue ||
                supplySourceUpdateDto.StartDate.HasValue)
            {
                var sourceSubdivisionId = supplySourceUpdateDto.SourceSubdivisionId ?? supplySource.SourceSubdivisionId;
                var destinationSubdivisionId = supplySourceUpdateDto.DestinationSubdivisionId ?? supplySource.DestinationSubdivisionId;
                var materialId = supplySourceUpdateDto.MaterialId ?? supplySource.MaterialId;
                var startDate = supplySourceUpdateDto.StartDate ?? supplySource.StartDate;

                var existingSupplySource = await _context.SupplySources
                    .FirstOrDefaultAsync(ss =>
                        ss.Id != id &&
                        ss.SourceSubdivisionId == sourceSubdivisionId &&
                        ss.DestinationSubdivisionId == destinationSubdivisionId &&
                        ss.MaterialId == materialId &&
                        ss.StartDate == startDate);

                if (existingSupplySource != null)
                {
                    return BadRequest("Источник поставок с такими параметрами уже существует");
                }
            }

            _mapper.Map(supplySourceUpdateDto, supplySource);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplySource(int id)
        {
            var supplySource = await _context.SupplySources.FindAsync(id);
            if (supplySource == null)
            {
                return NotFound();
            }

            _context.SupplySources.Remove(supplySource);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("matrix")]
        public async Task<ActionResult> GetSupplySourceMatrix(int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                // Получаем торговые подразделения
                var salesSubdivisions = await _context.Subdivisions
                    .Where(s => s.Type == SubdivisionType.Trading)
                    .ToListAsync();

                // Получаем материалы (готовая продукция)
                var materials = await _context.Materials
                    .Where(m => m.Type == MaterialType.FinishedProduct)
                    .ToListAsync();

                // Получаем источники поставок за указанный год
                var supplySources = await _context.SupplySources
                    .Include(ss => ss.SourceSubdivision)
                    .Include(ss => ss.DestinationSubdivision)
                    .Include(ss => ss.Material)
                    .Where(ss => ss.StartDate.Year == year || ss.EndDate.Year == year)
                    .ToListAsync();

                var result = new List<SupplySourceMatrixItemDto>();
                var months = new List<string>();

                // Создаем заголовки месяцев
                var culture = new System.Globalization.CultureInfo("ru-RU");
                for (int i = 1; i <= 12; i++)
                {
                    var date = new DateTime(year, i, 1);
                    months.Add(date.ToString("MMM.yy", culture));
                }

                foreach (var subdivision in salesSubdivisions)
                {
                    foreach (var material in materials)
                    {
                        var item = new SupplySourceMatrixItemDto
                        {
                            DestinationSubdivisionId = subdivision.Id,
                            DestinationSubdivisionName = subdivision.Name,
                            MaterialId = material.Id,
                            MaterialName = material.Name,
                            MonthlySources = new Dictionary<string, string>()
                        };

                        for (int month = 1; month <= 12; month++)
                        {
                            var monthStart = new DateTime(year, month, 1);
                            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                            var monthKey = monthStart.ToString("yyyy-MM");

                            var source = supplySources
                                .Where(ss => ss.DestinationSubdivisionId == subdivision.Id &&
                                           ss.MaterialId == material.Id &&
                                           ss.StartDate <= monthEnd &&
                                           ss.EndDate >= monthStart)
                                .OrderByDescending(ss => ss.StartDate)
                                .FirstOrDefault();

                            item.MonthlySources[monthKey] = source?.SourceSubdivision?.Name ?? "Не назначено";
                        }

                        result.Add(item);
                    }
                }

                return Ok(new { data = result, months });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении матрицы источников поставок: {ex.Message}");
            }
        }

        [HttpPost("updateMonthly")]
        public async Task<ActionResult> UpdateMonthlySupplySource([FromBody] MonthlySupplySourceUpdateDto updateDto)
        {
            try
            {
                var monthStart = new DateTime(updateDto.Year, updateDto.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Удаляем существующие источники для этого периода
                var existingSources = await _context.SupplySources
                    .Where(ss => ss.DestinationSubdivisionId == updateDto.DestinationSubdivisionId &&
                               ss.MaterialId == updateDto.MaterialId &&
                               ss.StartDate <= monthEnd &&
                               ss.EndDate >= monthStart)
                    .ToListAsync();

                _context.SupplySources.RemoveRange(existingSources);

                // Если указан новый источник, создаем его
                if (updateDto.SourceSubdivisionId.HasValue)
                {
                    var newSource = new SupplySource
                    {
                        DestinationSubdivisionId = updateDto.DestinationSubdivisionId,
                        MaterialId = updateDto.MaterialId,
                        SourceSubdivisionId = updateDto.SourceSubdivisionId.Value,
                        StartDate = monthStart,
                        EndDate = monthEnd
                    };

                    _context.SupplySources.Add(newSource);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обновлении источника поставки: {ex.Message}");
            }
        }
    }
}