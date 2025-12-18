namespace SupplyChainAPI.Models.SupplySourceDTO;

public class SupplySourceMatrixItemDto
{
    public int DestinationSubdivisionId { get; set; }
    public string DestinationSubdivisionName { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; }
    public Dictionary<string, string> MonthlySources { get; set; }
}