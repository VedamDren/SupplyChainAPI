using System.ComponentModel.DataAnnotations;

public class YearlyCalculationRequest
{
    [Required]
    [Range(2000, 2100, ErrorMessage = "Некорректный год")]
    public int Year { get; set; }
}