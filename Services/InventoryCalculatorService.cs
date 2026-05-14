using Microsoft.Extensions.Logging;

namespace SupplyChainAPI.Services
{
    public class InventoryCalculatorService
    {
        private readonly ILogger<InventoryCalculatorService> _logger;

        public InventoryCalculatorService(ILogger<InventoryCalculatorService> logger)
        {
            _logger = logger;
        }

        // Простой метод расчета
        public decimal CalculateInventoryPlan(decimal planAmount, decimal stockNorm)
        {
            // Простая формула: План × Норматив ÷ 30
            return (planAmount * stockNorm) / 30;
        }
    }
}