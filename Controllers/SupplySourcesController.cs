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
    }
}