public class SalesPlanMatrixDto
{
    public int SubdivisionId { get; set; }
    public string SubdivisionName { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public Dictionary<int, decimal> MonthlyPlans { get; set; } = new Dictionary<int, decimal>();
}