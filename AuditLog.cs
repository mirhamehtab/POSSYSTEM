namespace POSSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }       // "Complete", "Void", "Receive", "Delete" waghera
        public string EntityType { get; set; }    // "Sale", "Purchase", "Expense" waghera
        public string? EntityId { get; set; }
        public string Summary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
