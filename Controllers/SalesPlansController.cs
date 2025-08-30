using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.DTO;
using SupplyChainAPI.Models.SalesPlanDTO;
using AutoMapper;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public SalesPlansController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<SalesPlanResponseDto>>> GetAllSalesPlans()
        {
            var salesPlans = await _context.SalesPlans
                .Include(sp => sp.Subdivision)
                .Include(sp => sp.Material)
                .ToListAsync();

            return Ok(_mapper.Map<List<SalesPlanResponseDto>>(salesPlans));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesPlanResponseDto>> GetSalesPlan(int id)
        {
            var salesPlan = await _context.SalesPlans
                .Include(sp => sp.Subdivision)
                .Include(sp => sp.Material)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (salesPlan == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<SalesPlanResponseDto>(salesPlan));
        }

        [HttpPost]
        public async Task<ActionResult<SalesPlanResponseDto>> CreateSalesPlan(SalesPlanCreateDto salesPlanCreateDto)
        {
            // Проверка на уникальность комбинации SubdivisionId, MaterialId и Date
            var existingPlan = await _context.SalesPlans
                .FirstOrDefaultAsync(sp =>
                    sp.SubdivisionId == salesPlanCreateDto.SubdivisionId &&
                    sp.MaterialId == salesPlanCreateDto.MaterialId &&
                    sp.Date == salesPlanCreateDto.Date);

            if (existingPlan != null)
            {
                return BadRequest("План продаж с такими параметрами уже существует");
            }

            var salesPlan = _mapper.Map<SalesPlan>(salesPlanCreateDto);

            _context.SalesPlans.Add(salesPlan);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для возврата
            await _context.Entry(salesPlan)
                .Reference(sp => sp.Subdivision).LoadAsync();
            await _context.Entry(salesPlan)
                .Reference(sp => sp.Material).LoadAsync();

            return CreatedAtAction(nameof(GetSalesPlan),
                new { id = salesPlan.Id },
                _mapper.Map<SalesPlanResponseDto>(salesPlan));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalesPlan(int id, SalesPlanUpdateDto salesPlanUpdateDto)
        {
            var salesPlan = await _context.SalesPlans.FindAsync(id);
            if (salesPlan == null)
            {
                return NotFound();
            }

            // Проверка на уникальность при обновлении
            if (salesPlanUpdateDto.SubdivisionId.HasValue || salesPlanUpdateDto.MaterialId.HasValue || salesPlanUpdateDto.Date.HasValue)
            {
                var subdivisionId = salesPlanUpdateDto.SubdivisionId ?? salesPlan.SubdivisionId;
                var materialId = salesPlanUpdateDto.MaterialId ?? salesPlan.MaterialId;
                var date = salesPlanUpdateDto.Date ?? salesPlan.Date;

                var existingPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp =>
                        sp.Id != id &&
                        sp.SubdivisionId == subdivisionId &&
                        sp.MaterialId == materialId &&
                        sp.Date == date);

                if (existingPlan != null)
                {
                    return BadRequest("План продаж с такими параметрами уже существует");
                }
            }

            _mapper.Map(salesPlanUpdateDto, salesPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalesPlan(int id)
        {
            var salesPlan = await _context.SalesPlans.FindAsync(id);
            if (salesPlan == null)
            {
                return NotFound();
            }

            _context.SalesPlans.Remove(salesPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}