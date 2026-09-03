using POSSystem.Models;

namespace POSSystem.Services
{
    public class AuditLogger
    {
        private readonly ApplicationDbContext _context;

        public AuditLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        // NOTE: yeh khud SaveChanges nahi karta -- entry sirf _context mein add hoti hai,
        // caller ke apne SaveChangesAsync() ke saath hi save hoti hai, taake audit entry
        // aur asal action dono ek hi transaction mein atomic rahein (dono ya koi nahi)
        public void Log(string userId, string action, string entityType, string? entityId, string summary)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary
            });
        }
    }
}
