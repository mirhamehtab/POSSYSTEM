namespace POSSystem.Models
{
    public class BusinessSetting
    {
        public int Id { get; set; }
        public string BusinessName { get; set; } = "My Business";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxNumber { get; set; }
        public decimal TaxRate { get; set; } = 0;
        public bool TaxInclusive { get; set; } = false;
        public string CurrencySymbol { get; set; } = "Rs.";
        public string? ReceiptHeader { get; set; }
        public string? ReceiptFooter { get; set; }
        public string InvoicePrefix { get; set; } = "INV-";
    }
}