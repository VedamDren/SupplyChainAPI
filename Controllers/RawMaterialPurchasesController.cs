using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.RawMaterialPurchaseDTO;
using AutoMapper;
using System.Net;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawMaterialPurchasesController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RawMaterialPurchasesController> _logger;

        public RawMaterialPurchasesController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<RawMaterialPurchasesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/RawMaterialPurchases
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialPurchaseResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<RawMaterialPurchaseResponseDto>>> GetRawMaterialPurchases()
        {
            try
            {
                var purchases = await _context.RawMaterialPurchases
                    .Include(p => p.Subdivision)
                    .Include(p => p.RawMaterial)
                    .ToListAsync();

                var purchaseDtos = _mapper.Map<List<RawMaterialPurchaseResponseDto>>(purchases);
                return Ok(purchaseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material purchases");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/RawMaterialPurchases/GetAll
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<RawMaterialPurchaseResponseDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<RawMaterialPurchaseResponseDto>>> GetAllRawMaterialPurchases()
        {
            try
            {
                var purchases = await _context.RawMaterialPurchases
                    .Include(p => p.Subdivision)
                    .Include(p => p.RawMaterial)
                    .ToListAsync();

                var purchaseDtos = _mapper.Map<List<RawMaterialPurchaseResponseDto>>(purchases);
                return Ok(purchaseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material purchases via POST");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/RawMaterialPurchases/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RawMaterialPurchaseResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<RawMaterialPurchaseResponseDto>> GetRawMaterialPurchase(int id)
        {
            try
            {
                var purchase = await _context.RawMaterialPurchases
                    .Include(p => p.Subdivision)
                    .Include(p => p.RawMaterial)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (purchase == null)
                {
                    return NotFound();
                }

                var purchaseDto = _mapper.Map<RawMaterialPurchaseResponseDto>(purchase);
                return Ok(purchaseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting raw material purchase with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/RawMaterialPurchases
        [HttpPost]
        [ProducesResponseType(typeof(RawMaterialPurchaseResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<RawMaterialPurchaseResponseDto>> CreateRawMaterialPurchase(RawMaterialPurchaseCreateDto purchaseCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var purchase = _mapper.Map<RawMaterialPurchase>(purchaseCreateDto);

                _context.RawMaterialPurchases.Add(purchase);
                await _context.SaveChangesAsync();

                // Load related data for response
                await _context.Entry(purchase)
                    .Reference(p => p.Subdivision)
                    .LoadAsync();

                await _context.Entry(purchase)
                    .Reference(p => p.RawMaterial)
                    .LoadAsync();

                var purchaseDto = _mapper.Map<RawMaterialPurchaseResponseDto>(purchase);

                return CreatedAtAction(nameof(GetRawMaterialPurchase), new { id = purchaseDto.Id }, purchaseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating raw material purchase");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/RawMaterialPurchases/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateRawMaterialPurchase(int id, RawMaterialPurchaseUpdateDto purchaseUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var purchase = await _context.RawMaterialPurchases.FindAsync(id);
                if (purchase == null)
                {
                    return NotFound();
                }

                _mapper.Map(purchaseUpdateDto, purchase);
                _context.Entry(purchase).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!RawMaterialPurchaseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating raw material purchase with id: {Id}", id);
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating raw material purchase with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/RawMaterialPurchases/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteRawMaterialPurchase(int id)
        {
            try
            {
                var purchase = await _context.RawMaterialPurchases.FindAsync(id);
                if (purchase == null)
                {
                    return NotFound();
                }

                _context.RawMaterialPurchases.Remove(purchase);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting raw material purchase with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private bool RawMaterialPurchaseExists(int id)
        {
            return _context.RawMaterialPurchases.Any(e => e.Id == id);
        }
    }
}