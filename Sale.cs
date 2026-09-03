namespace POSSystem.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public string CashierId { get; set; }   // Identity user Id 
        public DateTime DateTime { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal AmountReceived { get; set; }
        public decimal ChangeDue { get; set; }
        public string? Note { get; set; }

        public List<SaleItem> Items { get; set; } = new();
    }
}