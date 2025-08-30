using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.RawMaterialWriteOffDTO;
using AutoMapper;
using System.Net;

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

        private bool RawMaterialWriteOffExists(int id)
        {
            return _context.RawMaterialWriteOffs.Any(e => e.Id == id);
        }
    }
}