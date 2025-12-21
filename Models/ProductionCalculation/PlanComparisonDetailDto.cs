namespace SupplyChainAPI.Models.ProductionCalculation
{
    public class PlanComparisonDetailDto
    {
        public DateTime Date { get; set; }
        public decimal CalculatedQuantity { get; set; }
        public decimal SavedQuantity { get; set; }
        public bool HasSavedPlan { get; set; }
        public decimal Difference { get; set; }

        public string Status
        {
            get
            {
                if (!HasSavedPlan) return "Not Saved";

                // используем decimal литерал (0.01m) вместо double
                return Math.Abs(Difference) < 0.01m ? "Match" : "Different";
            }
        }
    }
}
