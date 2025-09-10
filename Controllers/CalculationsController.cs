using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainMathLib;
using SupplyChainAPI.Models.InventoryCalculation;
using SupplyChainAPI.Models.ProductionCalculation;
using SupplyChainAPI.Models.TransferCalculation;
using SupplyChainAPI.Models.RawMaterialCalculation;
using System;
using System.Threading.Tasks;

namespace SupplyChainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculationsController : ControllerBase
    {
        private readonly SupplyChainContext _context;
        private readonly InventoryCalculator _inventoryCalculator;
        private readonly ProductionCalculator _productionCalculator;
        private readonly SupplyCalculator _supplyCalculator;

        public CalculationsController(
            SupplyChainContext context,
            InventoryCalculator inventoryCalculator,
            ProductionCalculator productionCalculator,
            SupplyCalculator supplyCalculator)
        {
            _context = context;
            _inventoryCalculator = inventoryCalculator;
            _productionCalculator = productionCalculator;
            _supplyCalculator = supplyCalculator;
        }

        #region расчет плана запасов [HttpPost("Inventory")]
        // POST: api/Calculations/Inventory
        [HttpPost("Inventory")]
        public async Task<ActionResult<InventoryCalculationResult>> CalculateInventoryPlan(
            [FromBody] InventoryCalculationRequest request)
        {
            try
            {
                // Получаем данные из БД
                var salesPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp => sp.SubdivisionId == request.SubdivisionId
                                            && sp.MaterialId == request.MaterialId
                                            && sp.Date == request.Date);

                var regulation = await _context.Regulations
                    .FirstOrDefaultAsync(r => r.SubdivisionId == request.SubdivisionId
                                           && r.MaterialId == request.MaterialId);

                if (salesPlan == null || regulation == null)
                    return NotFound("Required data not found");

                // Выполняем расчет с помощью математической библиотеки
                int daysInMonth = DateTime.DaysInMonth(request.Date.Year, request.Date.Month);
                decimal inventoryPlan = _inventoryCalculator.CalculateInventoryPlan(
                    salesPlan.Quantity,
                    regulation.DaysCount,
                    daysInMonth);

                return Ok(new InventoryCalculationResult
                {
                    Date = request.Date,
                    InventoryPlan = inventoryPlan,
                    SalesPlan = salesPlan.Quantity,
                    StockNorm = regulation.DaysCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Calculation error: {ex.Message}");
            }
        }
        #endregion расчет плана запасов [HttpPost("Inventory")]

        #region расчет производственного плана [HttpPost("Production")]
        // POST: api/Calculations/Production
        [HttpPost("Production")]
        public async Task<ActionResult<ProductionCalculationResult>> CalculateProductionPlan(
            [FromBody] ProductionCalculationRequest request)
        {
            try
            {
                // Получаем данные из БД
                var inventoryPlan = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date);

                var previousInventoryPlan = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date.AddMonths(-1));

                var transferPlan = await _context.TransferPlans
                    .FirstOrDefaultAsync(tp => tp.DestinationSubdivisionId == request.SubdivisionId
                                            && tp.MaterialId == request.MaterialId
                                            && tp.TransferDate == request.Date);

                if (inventoryPlan == null || previousInventoryPlan == null || transferPlan == null)
                    return NotFound("Required data not found");

                // Выполняем расчет с помощью математической библиотеки
                decimal productionPlan = _productionCalculator.CalculateProductionPlan(
                    inventoryPlan.Quantity,
                    previousInventoryPlan.Quantity,
                    transferPlan.Quantity);

                return Ok(new ProductionCalculationResult
                {
                    Date = request.Date,
                    ProductionPlan = productionPlan,
                    CurrentInventory = inventoryPlan.Quantity,
                    PreviousInventory = previousInventoryPlan.Quantity,
                    TransferQuantity = transferPlan.Quantity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Calculation error: {ex.Message}");
            }
        }
        #endregion расчет производственного плана [HttpPost("Production")]

        #region расчет плана перемещений [HttpPost("Transfer")]
        // POST: api/Calculations/Transfer
        [HttpPost("Transfer")]
        public async Task<ActionResult<TransferCalculationResult>> CalculateTransferPlan(
            [FromBody] TransferCalculationRequest request)
        {
            try
            {
                // Получаем данные из БД
                var currentInventory = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date);

                var previousInventory = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date.AddMonths(-1));

                var salesPlan = await _context.SalesPlans
                    .FirstOrDefaultAsync(sp => sp.SubdivisionId == request.SubdivisionId
                                            && sp.MaterialId == request.MaterialId
                                            && sp.Date == request.Date);

                if (currentInventory == null || previousInventory == null || salesPlan == null)
                    return NotFound("Required data not found");

                // Выполняем расчет с помощью математической библиотеки
                decimal transferPlan = _inventoryCalculator.CalculateTransferPlan(
                    currentInventory.Quantity,
                    previousInventory.Quantity,
                    salesPlan.Quantity);

                return Ok(new TransferCalculationResult
                {
                    Date = request.Date,
                    TransferPlan = transferPlan,
                    CurrentInventory = currentInventory.Quantity,
                    PreviousInventory = previousInventory.Quantity,
                    SalesAmount = salesPlan.Quantity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Calculation error: {ex.Message}");
            }
        }
        #endregion расчет плана перемещений [HttpPost("Transfer")]

        #region расчета плана закупа сырья [HttpPost("RawMaterial")]
        // POST: api/Calculations/RawMaterial
        [HttpPost("RawMaterial")]
        public async Task<ActionResult<RawMaterialCalculationResult>> CalculateRawMaterialPlan(
            [FromBody] RawMaterialCalculationRequest request)
        {
            try
            {
                // Получаем данные из БД
                var currentInventory = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date);

                var previousInventory = await _context.InventoryPlans
                    .FirstOrDefaultAsync(ip => ip.SubdivisionId == request.SubdivisionId
                                            && ip.MaterialId == request.MaterialId
                                            && ip.Date == request.Date.AddMonths(-1));

                var productionPlan = await _context.ProductionPlans
                    .FirstOrDefaultAsync(pp => pp.SubdivisionId == request.SubdivisionId
                                            && pp.MaterialId == request.MaterialId
                                            && pp.Date == request.Date);

                if (currentInventory == null || previousInventory == null || productionPlan == null)
                    return NotFound("Required data not found");

                // Выполняем расчет с помощью математической библиотеки
                decimal rawMaterialPlan = _supplyCalculator.CalculateRawMaterialPurchasePlan(
                    currentInventory.Quantity,
                    previousInventory.Quantity,
                    productionPlan.Quantity);

                return Ok(new RawMaterialCalculationResult
                {
                    Date = request.Date,
                    RawMaterialPlan = rawMaterialPlan,
                    CurrentInventory = currentInventory.Quantity,
                    PreviousInventory = previousInventory.Quantity,
                    ProductionQuantity = productionPlan.Quantity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Calculation error: {ex.Message}");
            }
            #endregion расчета плана закупа сырья [HttpPost("RawMaterial")]
        }
    }
}