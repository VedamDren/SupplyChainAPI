using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using AutoMapper;
using System.Net;
using SupplyChainAPI.Models.ProductionPlan;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductionPlansController> _logger;

        public ProductionPlansController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<ProductionPlansController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/ProductionPlans
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductionPlanResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ProductionPlanResponseDto>>> GetProductionPlans()
        {
            try
            {
                var productionPlans = await _context.ProductionPlans
                    .Include(pp => pp.Subdivision)
                    .Include(pp => pp.Material)
                    .ToListAsync();

                var productionPlanDtos = _mapper.Map<List<ProductionPlanResponseDto>>(productionPlans);
                return Ok(productionPlanDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting production plans");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/ProductionPlans/GetAll
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<ProductionPlanResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ProductionPlanResponseDto>>> GetAllProductionPlans()
        {
            try
            {
                var productionPlans = await _context.ProductionPlans
                    .Include(pp => pp.Subdivision)
                    .Include(pp => pp.Material)
                    .ToListAsync();

                var productionPlanDtos = _mapper.Map<List<ProductionPlanResponseDto>>(productionPlans);
                return Ok(productionPlanDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting production plans via POST");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/ProductionPlans/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductionPlanResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<ProductionPlanResponseDto>> GetProductionPlan(int id)
        {
            try
            {
                var productionPlan = await _context.ProductionPlans
                    .Include(pp => pp.Subdivision)
                    .Include(pp => pp.Material)
                    .FirstOrDefaultAsync(pp => pp.Id == id);

                if (productionPlan == null)
                {
                    return NotFound();
                }

                var productionPlanDto = _mapper.Map<ProductionPlanResponseDto>(productionPlan);
                return Ok(productionPlanDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting production plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/ProductionPlans
        [HttpPost]
        [ProducesResponseType(typeof(ProductionPlanResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ProductionPlanResponseDto>> CreateProductionPlan(ProductionPlanCreateDto productionPlanCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем, существует ли уже план на эту дату для этого подразделения и материала
                var existingPlan = await _context.ProductionPlans
                    .FirstOrDefaultAsync(pp =>
                        pp.SubdivisionId == productionPlanCreateDto.SubdivisionId &&
                        pp.MaterialId == productionPlanCreateDto.MaterialId &&
                        pp.Date == productionPlanCreateDto.Date);

                if (existingPlan != null)
                {
                    return BadRequest("Production plan for this subdivision, material and date already exists");
                }

                var productionPlan = _mapper.Map<ProductionPlan>(productionPlanCreateDto);

                _context.ProductionPlans.Add(productionPlan);
                await _context.SaveChangesAsync();

                // Загружаем связанные данные для ответа
                await _context.Entry(productionPlan)
                    .Reference(pp => pp.Subdivision)
                    .LoadAsync();

                await _context.Entry(productionPlan)
                    .Reference(pp => pp.Material)
                    .LoadAsync();

                var productionPlanDto = _mapper.Map<ProductionPlanResponseDto>(productionPlan);

                return CreatedAtAction(nameof(GetProductionPlan), new { id = productionPlanDto.Id }, productionPlanDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating production plan");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/ProductionPlans/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProductionPlan(int id, ProductionPlanUpdateDto productionPlanUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var productionPlan = await _context.ProductionPlans.FindAsync(id);
                if (productionPlan == null)
                {
                    return NotFound();
                }

                // Проверяем, не существует ли другого плана с такими же параметрами
                if (productionPlanUpdateDto.SubdivisionId.HasValue ||
                    productionPlanUpdateDto.MaterialId.HasValue ||
                    productionPlanUpdateDto.Date.HasValue)
                {
                    int subdivisionId = productionPlanUpdateDto.SubdivisionId ?? productionPlan.SubdivisionId;
                    int materialId = productionPlanUpdateDto.MaterialId ?? productionPlan.MaterialId;
                    DateTime date = productionPlanUpdateDto.Date ?? productionPlan.Date;

                    var existingPlan = await _context.ProductionPlans
                        .FirstOrDefaultAsync(pp =>
                            pp.Id != id &&
                            pp.SubdivisionId == subdivisionId &&
                            pp.MaterialId == materialId &&
                            pp.Date == date);

                    if (existingPlan != null)
                    {
                        return BadRequest("Another production plan with these parameters already exists");
                    }
                }

                _mapper.Map(productionPlanUpdateDto, productionPlan);
                _context.Entry(productionPlan).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ProductionPlanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating production plan with id: {Id}", id);
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating production plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/ProductionPlans/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductionPlan(int id)
        {
            try
            {
                var productionPlan = await _context.ProductionPlans.FindAsync(id);
                if (productionPlan == null)
                {
                    return NotFound();
                }

                _context.ProductionPlans.Remove(productionPlan);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting production plan with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private bool ProductionPlanExists(int id)
        {
            return _context.ProductionPlans.Any(e => e.Id == id);
        }
    }
}