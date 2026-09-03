namespace POSSystem.Models
{
    public class SaleReturnItem
    {
        public int Id { get; set; }
        public int SaleReturnId { get; set; }
        public SaleReturn? SaleReturn { get; set; }

        public int SaleItemId { get; set; }
        public SaleItem? SaleItem { get; set; }

        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}