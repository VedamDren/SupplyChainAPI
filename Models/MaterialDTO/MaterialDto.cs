using SupplyChainData;
using System.ComponentModel.DataAnnotations;

public class MaterialResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // "RawMaterial" или "FinishedProduct"
}

public class MaterialCreateDto
{
    [Required] public string Name { get; set; }
    [Required] public MaterialType Type { get; set; }
}