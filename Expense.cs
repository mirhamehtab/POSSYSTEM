namespace POSSystem.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Description { get; set; }
        public string? Reference { get; set; }
        public bool IsVoided { get; set; } = false;
        public string? VoidReason { get; set; }
        public string CreatedBy { get; set; }
    }
}