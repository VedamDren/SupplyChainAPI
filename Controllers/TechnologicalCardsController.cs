using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Models.TechnologicalCardDTO;
using AutoMapper;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechnologicalCardsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly IMapper _mapper;

        public TechnologicalCardsController(SupplyChainContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost("getAll")]
        public async Task<ActionResult<IEnumerable<TechnologicalCardDto>>> GetAllTechnologicalCards()
        {
            var technologicalCards = await _context.TechnologicalCards
                .Include(tc => tc.Subdivision)
                .Include(tc => tc.FinishedProduct)
                .Include(tc => tc.RawMaterial)
                .ToListAsync();

            return Ok(_mapper.Map<List<TechnologicalCardDto>>(technologicalCards));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TechnologicalCardDto>> GetTechnologicalCard(int id)
        {
            var technologicalCard = await _context.TechnologicalCards
                .Include(tc => tc.Subdivision)
                .Include(tc => tc.FinishedProduct)
                .Include(tc => tc.RawMaterial)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (technologicalCard == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TechnologicalCardDto>(technologicalCard));
        }

        [HttpPost]
        public async Task<ActionResult<TechnologicalCardDto>> CreateTechnologicalCard(TechnologicalCardCreateDto technologicalCardCreateDto)
        {
            // Проверка на уникальность комбинации SubdivisionId, FinishedProductId и RawMaterialId
            var existingTechnologicalCard = await _context.TechnologicalCards
                .FirstOrDefaultAsync(tc =>
                    tc.SubdivisionId == technologicalCardCreateDto.SubdivisionId &&
                    tc.FinishedProductId == technologicalCardCreateDto.FinishedProductId &&
                    tc.RawMaterialId == technologicalCardCreateDto.RawMaterialId);

            if (existingTechnologicalCard != null)
            {
                return BadRequest("Технологическая карта с такими параметрами уже существует");
            }

            // Проверка, что FinishedProduct действительно является готовой продукцией
            var finishedProduct = await _context.Materials.FindAsync(technologicalCardCreateDto.FinishedProductId);
            if (finishedProduct == null || finishedProduct.Type != MaterialType.FinishedProduct)
            {
                return BadRequest("FinishedProductId должен ссылаться на готовую продукцию");
            }

            // Проверка, что RawMaterial действительно является сырьем
            var rawMaterial = await _context.Materials.FindAsync(technologicalCardCreateDto.RawMaterialId);
            if (rawMaterial == null || rawMaterial.Type != MaterialType.RawMaterial)
            {
                return BadRequest("RawMaterialId должен ссылаться на сырье");
            }

            var technologicalCard = _mapper.Map<TechnologicalCard>(technologicalCardCreateDto);

            _context.TechnologicalCards.Add(technologicalCard);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для возврата
            await _context.Entry(technologicalCard)
                .Reference(tc => tc.Subdivision).LoadAsync();
            await _context.Entry(technologicalCard)
                .Reference(tc => tc.FinishedProduct).LoadAsync();
            await _context.Entry(technologicalCard)
                .Reference(tc => tc.RawMaterial).LoadAsync();

            return CreatedAtAction(nameof(GetTechnologicalCard),
                new { id = technologicalCard.Id },
                _mapper.Map<TechnologicalCardDto>(technologicalCard));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTechnologicalCard(int id, TechnologicalCardUpdateDto technologicalCardUpdateDto)
        {
            var technologicalCard = await _context.TechnologicalCards.FindAsync(id);
            if (technologicalCard == null)
            {
                return NotFound();
            }

            // Проверка на уникальность при обновлении
            if (technologicalCardUpdateDto.SubdivisionId.HasValue ||
                technologicalCardUpdateDto.FinishedProductId.HasValue ||
                technologicalCardUpdateDto.RawMaterialId.HasValue)
            {
                var subdivisionId = technologicalCardUpdateDto.SubdivisionId ?? technologicalCard.SubdivisionId;
                var finishedProductId = technologicalCardUpdateDto.FinishedProductId ?? technologicalCard.FinishedProductId;
                var rawMaterialId = technologicalCardUpdateDto.RawMaterialId ?? technologicalCard.RawMaterialId;

                var existingTechnologicalCard = await _context.TechnologicalCards
                    .FirstOrDefaultAsync(tc =>
                        tc.Id != id &&
                        tc.SubdivisionId == subdivisionId &&
                        tc.FinishedProductId == finishedProductId &&
                        tc.RawMaterialId == rawMaterialId);

                if (existingTechnologicalCard != null)
                {
                    return BadRequest("Технологическая карта с такими параметрами уже существует");
                }
            }

            // Проверка типов материалов при обновлении
            if (technologicalCardUpdateDto.FinishedProductId.HasValue)
            {
                var finishedProduct = await _context.Materials.FindAsync(technologicalCardUpdateDto.FinishedProductId.Value);
                if (finishedProduct == null || finishedProduct.Type != MaterialType.FinishedProduct)
                {
                    return BadRequest("FinishedProductId должен ссылаться на готовую продукцию");
                }
            }

            if (technologicalCardUpdateDto.RawMaterialId.HasValue)
            {
                var rawMaterial = await _context.Materials.FindAsync(technologicalCardUpdateDto.RawMaterialId.Value);
                if (rawMaterial == null || rawMaterial.Type != MaterialType.RawMaterial)
                {
                    return BadRequest("RawMaterialId должен ссылаться на сырье");
                }
            }

            _mapper.Map(technologicalCardUpdateDto, technologicalCard);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTechnologicalCard(int id)
        {
            var technologicalCard = await _context.TechnologicalCards.FindAsync(id);
            if (technologicalCard == null)
            {
                return NotFound();
            }

            _context.TechnologicalCards.Remove(technologicalCard);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}