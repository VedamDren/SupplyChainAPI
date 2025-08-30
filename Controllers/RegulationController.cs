using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using AutoMapper;
using SupplyChainAPI.Models.Regulation;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegulationsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public RegulationsController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<RegulationDto>>> GetAllRegulations()
        {
            var regulations = await _context.Regulations
                .Include(r => r.Subdivision)
                .Include(r => r.Material)
                .ToListAsync();

            return Ok(_mapper.Map<List<RegulationDto>>(regulations));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RegulationDto>> GetRegulation(int id)
        {
            var regulation = await _context.Regulations
                .Include(r => r.Subdivision)
                .Include(r => r.Material)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (regulation == null)
            {
                return NotFound();
            }

            return _mapper.Map<RegulationDto>(regulation);
        }

        [HttpPost]
        public async Task<ActionResult<RegulationDto>> CreateRegulation(RegulationCreateDto regulationDto)
        {
            var regulation = _mapper.Map<Regulation>(regulationDto);

            _context.Regulations.Add(regulation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRegulation),
                new { id = regulation.Id },
                _mapper.Map<RegulationDto>(regulation));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegulation(int id, RegulationUpdateDto regulationDto)
        {
            var regulation = await _context.Regulations.FindAsync(id);
            if (regulation == null)
            {
                return NotFound();
            }

            _mapper.Map(regulationDto, regulation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegulation(int id)
        {
            var regulation = await _context.Regulations.FindAsync(id);
            if (regulation == null)
            {
                return NotFound();
            }

            _context.Regulations.Remove(regulation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}