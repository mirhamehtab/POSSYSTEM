namespace POSSystem.Models
{
    public class SaleReturn
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public string ProcessedBy { get; set; }
        public string Reason { get; set; }
        public string? Notes { get; set; }
        public decimal RefundTotal { get; set; }
        public string RefundMethod { get; set; } = "Cash";
        public DateTime DateTime { get; set; } = DateTime.Now;

        public List<SaleReturnItem> Items { get; set; } = new();
    }
}