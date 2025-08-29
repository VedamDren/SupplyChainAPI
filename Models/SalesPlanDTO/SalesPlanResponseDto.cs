using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.DTO
{
    public class SalesPlanResponseDto
    {
        public int Id { get; set; }
        public string SubdivisionName { get; set; }
        public string MaterialName { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }

}