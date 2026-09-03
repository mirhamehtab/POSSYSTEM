namespace POSSystem.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public string PurchaseNo { get; set; }
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public string? SupplierInvoiceNo { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Draft";   // Draft or Received
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public DateTime? ReceivedAt { get; set; }

        public List<PurchaseItem> Items { get; set; } = new();
    }
}