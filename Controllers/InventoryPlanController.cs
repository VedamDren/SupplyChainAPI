using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.InventoryPlan;
using AutoMapper;
using System.Net;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<InventoryPlansController> _logger;

        public InventoryPlansController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<InventoryPlansController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/InventoryPlans
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<InventoryPlanResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<InventoryPlanResponseDto>>> GetInventoryPlans()
        {
            try
            {
                var inventoryPlans = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .ToListAsync();

                var inventoryPlanDtos = _mapper.Map<List<InventoryPlanResponseDto>>(inventoryPlans);
                return Ok(inventoryPlanDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory plans");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/InventoryPlans/GetAll
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<InventoryPlanResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<InventoryPlanResponseDto>>> GetAllInventoryPlans()
        {
            try
            {
                var inventoryPlans = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .ToListAsync();

                var inventoryPlanDtos = _mapper.Map<List<InventoryPlanResponseDto>>(inventoryPlans);
                return Ok(inventoryPlanDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory plans via POST");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/InventoryPlans/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryPlanResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<InventoryPlanResponseDto>> GetInventoryPlan(int id)
        {
            try
            {
                var inventoryPlan = await _context.InventoryPlans
                    .Include(ip => ip.Subdivision)
                    .Include(ip => ip.Material)
                    .FirstOrDefaultAsync(ip => ip.Id == id);

                if (inventoryPlan == null)
                {
                    return NotFound();
                }

                var inventoryPlanDto = _mapper.Map<InventoryPlanResponseDto>(inventoryPlan);
                return Ok(inventoryPlanDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/InventoryPlans
        [HttpPost]
        [ProducesResponseType(typeof(InventoryPlanResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<InventoryPlanResponseDto>> CreateInventoryPlan(InventoryPlanCreateDto inventoryPlanCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if inventory plan already exists for this subdivision, material and date
                var existingPlan = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip =>
                        ip.SubdivisionId == inventoryPlanCreateDto.SubdivisionId &&
                        ip.MaterialId == inventoryPlanCreateDto.MaterialId &&
                        ip.Date == inventoryPlanCreateDto.Date);

                if (existingPlan != null)
                {
                    return BadRequest("Inventory plan for this subdivision, material and date already exists");
                }

                var inventoryPlan = _mapper.Map<InventoryPlan>(inventoryPlanCreateDto);

                _context.InventoryPlans.Add(inventoryPlan);
                await _context.SaveChangesAsync();

                // Load related data for response
                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Subdivision)
                    .LoadAsync();

                await _context.Entry(inventoryPlan)
                    .Reference(ip => ip.Material)
                    .LoadAsync();

                var inventoryPlanDto = _mapper.Map<InventoryPlanResponseDto>(inventoryPlan);

                return CreatedAtAction(nameof(GetInventoryPlan), new { id = inventoryPlanDto.Id }, inventoryPlanDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory plan");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/InventoryPlans/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateInventoryPlan(int id, InventoryPlanUpdateDto inventoryPlanUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var inventoryPlan = await _context.InventoryPlans.FindAsync(id);
                if (inventoryPlan == null)
                {
                    return NotFound();
                }

                // Check if another inventory plan with the same parameters exists
                if (inventoryPlanUpdateDto.SubdivisionId.HasValue ||
                    inventoryPlanUpdateDto.MaterialId.HasValue ||
                    inventoryPlanUpdateDto.Date.HasValue)
                {
                    int subdivisionId = inventoryPlanUpdateDto.SubdivisionId ?? inventoryPlan.SubdivisionId;
                    int materialId = inventoryPlanUpdateDto.MaterialId ?? inventoryPlan.MaterialId;
                    DateTime date = inventoryPlanUpdateDto.Date ?? inventoryPlan.Date;

                    var existingPlan = await _context.InventoryPlans
                        .FirstOrDefaultAsync(ip =>
                            ip.Id != id &&
                            ip.SubdivisionId == subdivisionId &&
                            ip.MaterialId == materialId &&
                            ip.Date == date);

                    if (existingPlan != null)
                    {
                        return BadRequest("Another inventory plan with these parameters already exists");
                    }
                }

                _mapper.Map(inventoryPlanUpdateDto, inventoryPlan);
                _context.Entry(inventoryPlan).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!InventoryPlanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating inventory plan with id: {Id}", id);
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/InventoryPlans/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteInventoryPlan(int id)
        {
            try
            {
                var inventoryPlan = await _context.InventoryPlans.FindAsync(id);
                if (inventoryPlan == null)
                {
                    return NotFound();
                }

                _context.InventoryPlans.Remove(inventoryPlan);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private bool InventoryPlanExists(int id)
        {
            return _context.InventoryPlans.Any(e => e.Id == id);
        }
    }
}