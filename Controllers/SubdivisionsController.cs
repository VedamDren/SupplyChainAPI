using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.SubdivisionDTO;
using AutoMapper;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubdivisionsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public SubdivisionsController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<SubdivisionDto>>> GetAllSubdivisions()
        {
            var subdivisions = await _context.Subdivisions.ToListAsync();
            return Ok(_mapper.Map<List<SubdivisionDto>>(subdivisions));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubdivisionDto>> GetSubdivision(int id)
        {
            var subdivision = await _context.Subdivisions.FindAsync(id);

            if (subdivision == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<SubdivisionDto>(subdivision));
        }

        [HttpPost]
        public async Task<ActionResult<SubdivisionDto>> CreateSubdivision(SubdivisionCreateDto subdivisionCreateDto)
        {
            // Проверка на уникальность имени
            var existingSubdivision = await _context.Subdivisions
                .FirstOrDefaultAsync(s => s.Name == subdivisionCreateDto.Name);

            if (existingSubdivision != null)
            {
                return BadRequest("Подразделение с таким именем уже существует");
            }

            var subdivision = _mapper.Map<Subdivision>(subdivisionCreateDto);
            _context.Subdivisions.Add(subdivision);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubdivision),
                new { id = subdivision.Id },
                _mapper.Map<SubdivisionDto>(subdivision));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubdivision(int id, SubdivisionUpdateDto subdivisionUpdateDto)
        {
            var subdivision = await _context.Subdivisions.FindAsync(id);
            if (subdivision == null)
            {
                return NotFound();
            }

            // Проверка на уникальность имени при обновлении
            if (!string.IsNullOrEmpty(subdivisionUpdateDto.Name) && subdivisionUpdateDto.Name != subdivision.Name)
            {
                var existingSubdivision = await _context.Subdivisions
                    .FirstOrDefaultAsync(s => s.Name == subdivisionUpdateDto.Name && s.Id != id);

                if (existingSubdivision != null)
                {
                    return BadRequest("Подразделение с таким именем уже существует");
                }
            }

            _mapper.Map(subdivisionUpdateDto, subdivision);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubdivision(int id)
        {
            var subdivision = await _context.Subdivisions
                .Include(s => s.SalesPlans)
                .Include(s => s.InventoryPlans)
                .Include(s => s.SupplySourcesAsSource)
                .Include(s => s.SupplySourcesAsDestination)
                .Include(s => s.TransferPlansAsSource)
                .Include(s => s.TransferPlansAsDestination)
                .Include(s => s.RawMaterialPurchases)
                .Include(s => s.RawMaterialWriteOffs)
                .Include(s => s.Regulations)
                .Include(s => s.TechnologicalCards)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subdivision == null)
            {
                return NotFound();
            }

            // Проверка на наличие связанных данных
            if (subdivision.SalesPlans.Any() ||
                subdivision.InventoryPlans.Any() ||
                subdivision.SupplySourcesAsSource.Any() ||
                subdivision.SupplySourcesAsDestination.Any() ||
                subdivision.TransferPlansAsSource.Any() ||
                subdivision.TransferPlansAsDestination.Any() ||
                subdivision.RawMaterialPurchases.Any() ||
                subdivision.RawMaterialWriteOffs.Any() ||
                subdivision.Regulations.Any() ||
                subdivision.TechnologicalCards.Any())
            {
                return BadRequest("Невозможно удалить подразделение, так как с ним связаны другие данные");
            }

            _context.Subdivisions.Remove(subdivision);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}