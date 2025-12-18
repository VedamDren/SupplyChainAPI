using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainData
{
    public class RawMaterialConsumptionPlan
    {
        [Key]
        public int Id { get; set; }

        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }

        // Навигационные свойства
        public Subdivision Subdivision { get; set; }
        public Material Material { get; set; }
    }
}