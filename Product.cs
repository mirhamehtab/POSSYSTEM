using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string? Barcode { get; set; }
        public string Unit { get; set; } = "pcs";

        [Range(0, double.MaxValue, ErrorMessage = "Cost price cannot be negative.")]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Selling price cannot be negative.")]
        public decimal SellingPrice { get; set; }

        public bool IsTaxable { get; set; } = true;
        public int CurrentStock { get; set; } = 0;
        public int ReorderLevel { get; set; } = 5;
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}