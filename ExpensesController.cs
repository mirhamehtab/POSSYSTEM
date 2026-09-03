using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditLogger _audit;

        public ExpensesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, AuditLogger audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        // GET: /Expenses -- with optional filters
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? categoryId)
        {
            var query = _context.Expenses.Include(e => e.ExpenseCategory).AsQueryable();

            if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
            if (to.HasValue) query = query.Where(e => e.Date <= to.Value);
            if (categoryId.HasValue) query = query.Where(e => e.ExpenseCategoryId == categoryId);

            ViewBag.Categories = await _context.ExpenseCategories.Where(c => c.IsActive).ToListAsync();
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.CategoryId = categoryId;
            ViewBag.Total = await query.Where(e => !e.IsVoided).SumAsync(e => e.Amount);

            var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();
            return View(expenses);
        }

        // GET: /Expenses/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.ExpenseCategories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        // POST: /Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.ExpenseCategories.Where(c => c.IsActive).ToListAsync();
                return View(expense);
            }

            expense.CreatedBy = _userManager.GetUserId(User)!;
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Expenses/Void/5 -- delete nahi karte, void karte hain (FR-087)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Void(int id, string voidReason)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            expense.IsVoided = true;
            expense.VoidReason = voidReason;

            _audit.Log(_userManager.GetUserId(User)!, "Void", "Expense", expense.Id.ToString(), $"Voided expense of Rs.{expense.Amount:N2} -- reason: {voidReason}");

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}