using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.MaterialDTO;
using AutoMapper;
using System.Net;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MaterialsController> _logger;

        public MaterialsController(
            SupplyChainContext context,
            IMapper mapper,
            ILogger<MaterialsController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Materials
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MaterialDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<MaterialDto>>> GetMaterials()
        {
            try
            {
                var materials = await _context.Materials.ToListAsync();
                var materialDtos = _mapper.Map<List<MaterialDto>>(materials);
                return Ok(materialDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting materials");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Materials/GetAll
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<MaterialDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<MaterialDto>>> GetAllMaterials()
        {
            try
            {
                var materials = await _context.Materials.ToListAsync();
                var materialDtos = _mapper.Map<List<MaterialDto>>(materials);
                return Ok(materialDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting materials via POST");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Materials/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MaterialDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MaterialDto>> GetMaterial(int id)
        {
            try
            {
                var material = await _context.Materials.FindAsync(id);

                if (material == null)
                {
                    return NotFound();
                }

                var materialDto = _mapper.Map<MaterialDto>(material);
                return Ok(materialDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting material with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Materials
        [HttpPost]
        [ProducesResponseType(typeof(MaterialDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<MaterialDto>> CreateMaterial(MaterialCreateDto materialCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем, существует ли материал с таким именем
                var existingMaterial = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Name == materialCreateDto.Name);

                if (existingMaterial != null)
                {
                    return BadRequest("Material with this name already exists");
                }

                var material = _mapper.Map<Material>(materialCreateDto);

                _context.Materials.Add(material);
                await _context.SaveChangesAsync();

                var materialDto = _mapper.Map<MaterialDto>(material);

                return CreatedAtAction(nameof(GetMaterial), new { id = materialDto.Id }, materialDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating material");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Materials/5
        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateMaterial(int id, MaterialUpdateDto materialUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var material = await _context.Materials.FindAsync(id);
                if (material == null)
                {
                    return NotFound();
                }

                // Проверяем, не существует ли другого материала с таким именем
                if (!string.IsNullOrEmpty(materialUpdateDto.Name))
                {
                    var existingMaterial = await _context.Materials
                        .FirstOrDefaultAsync(m => m.Name == materialUpdateDto.Name && m.Id != id);

                    if (existingMaterial != null)
                    {
                        return BadRequest("Another material with this name already exists");
                    }
                }

                _mapper.Map(materialUpdateDto, material);
                _context.Entry(material).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!MaterialExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating material with id: {Id}", id);
                    return StatusCode(500, "Internal server error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating material with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Materials/5
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            try
            {
                var material = await _context.Materials.FindAsync(id);
                if (material == null)
                {
                    return NotFound();
                }

                // Проверяем, есть ли связанные записи
                var hasRelatedRecords = await CheckForRelatedRecords(id);
                if (hasRelatedRecords)
                {
                    return BadRequest("Cannot delete material because it has related records");
                }

                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material with id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private bool MaterialExists(int id)
        {
            return _context.Materials.Any(e => e.Id == id);
        }

        private async Task<bool> CheckForRelatedRecords(int materialId)
        {
            // Проверяем все возможные связи материала с другими таблицами
            return await _context.SalesPlans.AnyAsync(sp => sp.MaterialId == materialId) ||
                   await _context.InventoryPlans.AnyAsync(ip => ip.MaterialId == materialId) ||
                   await _context.Regulations.AnyAsync(r => r.MaterialId == materialId) ||
                   await _context.SupplySources.AnyAsync(ss => ss.MaterialId == materialId) ||
                   await _context.TransferPlans.AnyAsync(tp => tp.MaterialId == materialId) ||
                   await _context.RawMaterialPurchases.AnyAsync(rmp => rmp.RawMaterialId == materialId) ||
                   await _context.RawMaterialWriteOffs.AnyAsync(rmw => rmw.RawMaterialId == materialId) ||
                   await _context.TechnologicalCards.AnyAsync(tc => tc.FinishedProductId == materialId || tc.RawMaterialId == materialId) ||
                   await _context.ProductionPlans.AnyAsync(pp => pp.MaterialId == materialId);
        }
    }
}