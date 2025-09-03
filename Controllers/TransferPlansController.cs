using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.TransferPlanDTO;
using AutoMapper;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferPlansController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public TransferPlansController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<TransferPlanDto>>> GetAllTransferPlans()
        {
            var transferPlans = await _context.TransferPlans
                .Include(tp => tp.SourceSubdivision)
                .Include(tp => tp.DestinationSubdivision)
                .Include(tp => tp.Material)
                .ToListAsync();

            return Ok(_mapper.Map<List<TransferPlanDto>>(transferPlans));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransferPlanDto>> GetTransferPlan(int id)
        {
            var transferPlan = await _context.TransferPlans
                .Include(tp => tp.SourceSubdivision)
                .Include(tp => tp.DestinationSubdivision)
                .Include(tp => tp.Material)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (transferPlan == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TransferPlanDto>(transferPlan));
        }

        [HttpPost]
        public async Task<ActionResult<TransferPlanDto>> CreateTransferPlan(TransferPlanCreateDto transferPlanCreateDto)
        {
            // Проверка на уникальность комбинации SourceSubdivisionId, DestinationSubdivisionId, MaterialId и TransferDate
            var existingTransferPlan = await _context.TransferPlans
                .FirstOrDefaultAsync(tp =>
                    tp.SourceSubdivisionId == transferPlanCreateDto.SourceSubdivisionId &&
                    tp.DestinationSubdivisionId == transferPlanCreateDto.DestinationSubdivisionId &&
                    tp.MaterialId == transferPlanCreateDto.MaterialId &&
                    tp.TransferDate == transferPlanCreateDto.TransferDate);

            if (existingTransferPlan != null)
            {
                return BadRequest("План перемещений с такими параметрами уже существует");
            }

            var transferPlan = _mapper.Map<TransferPlan>(transferPlanCreateDto);

            _context.TransferPlans.Add(transferPlan);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для возврата
            await _context.Entry(transferPlan)
                .Reference(tp => tp.SourceSubdivision).LoadAsync();
            await _context.Entry(transferPlan)
                .Reference(tp => tp.DestinationSubdivision).LoadAsync();
            await _context.Entry(transferPlan)
                .Reference(tp => tp.Material).LoadAsync();

            return CreatedAtAction(nameof(GetTransferPlan),
                new { id = transferPlan.Id },
                _mapper.Map<TransferPlanDto>(transferPlan));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransferPlan(int id, TransferPlanUpdateDto transferPlanUpdateDto)
        {
            var transferPlan = await _context.TransferPlans.FindAsync(id);
            if (transferPlan == null)
            {
                return NotFound();
            }

            // Проверка на уникальность при обновлении
            if (transferPlanUpdateDto.SourceSubdivisionId.HasValue ||
                transferPlanUpdateDto.DestinationSubdivisionId.HasValue ||
                transferPlanUpdateDto.MaterialId.HasValue ||
                transferPlanUpdateDto.TransferDate.HasValue)
            {
                var sourceSubdivisionId = transferPlanUpdateDto.SourceSubdivisionId ?? transferPlan.SourceSubdivisionId;
                var destinationSubdivisionId = transferPlanUpdateDto.DestinationSubdivisionId ?? transferPlan.DestinationSubdivisionId;
                var materialId = transferPlanUpdateDto.MaterialId ?? transferPlan.MaterialId;
                var transferDate = transferPlanUpdateDto.TransferDate ?? transferPlan.TransferDate;

                var existingTransferPlan = await _context.TransferPlans
                    .FirstOrDefaultAsync(tp =>
                        tp.Id != id &&
                        tp.SourceSubdivisionId == sourceSubdivisionId &&
                        tp.DestinationSubdivisionId == destinationSubdivisionId &&
                        tp.MaterialId == materialId &&
                        tp.TransferDate == transferDate);

                if (existingTransferPlan != null)
                {
                    return BadRequest("План перемещений с такими параметрами уже существует");
                }
            }

            _mapper.Map(transferPlanUpdateDto, transferPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransferPlan(int id)
        {
            var transferPlan = await _context.TransferPlans.FindAsync(id);
            if (transferPlan == null)
            {
                return NotFound();
            }

            _context.TransferPlans.Remove(transferPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}