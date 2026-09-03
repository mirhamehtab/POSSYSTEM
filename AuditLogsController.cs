using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    // sirf Administrator dekh sake -- SRS ke role table mein "View audit log" Manager ke liye
    // bhi Allowed hai, lekin Administrator-only rakha hai kyunki yeh sabse sensitive screen hai
    [Authorize(Roles = "Administrator")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AuditLogsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /AuditLogs -- search/filter -able list
        public async Task<IActionResult> Index(string? entityType, string? action)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(200)
                .ToListAsync();

            // UserId Identity ka GUID hota hai, readable email ke saath dikhane ke liye lookup karte hain
            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            ViewBag.UserEmails = users;
            ViewBag.EntityType = entityType;
            ViewBag.Action = action;

            return View(logs);
        }
    }
}
