using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports -- menu page
        public IActionResult Index() => View();

        // GET: /Reports/Sales?from=&to=
        public async Task<IActionResult> Sales(DateTime? from, DateTime? to)
        {
            var start = from ?? DateTime.Today.AddDays(-7);
            var end = (to ?? DateTime.Today).AddDays(1); // inclusive of the "to" day

            var sales = await _context.Sales
                .Where(s => s.DateTime >= start && s.DateTime < end)
                .ToListAsync();

            var returns = await _context.SaleReturns
                .Where(r => r.DateTime >= start && r.DateTime < end)
                .ToListAsync();

            ViewBag.From = start.ToString("yyyy-MM-dd");
            ViewBag.To = (end.AddDays(-1)).ToString("yyyy-MM-dd");
            ViewBag.GrossSales = sales.Sum(s => s.Subtotal);
            ViewBag.TotalDiscount = sales.Sum(s => s.Discount);
            ViewBag.TotalTax = sales.Sum(s => s.Tax);
            ViewBag.TotalReturns = returns.Sum(r => r.RefundTotal);
            ViewBag.NetSales = sales.Sum(s => s.Total) - returns.Sum(r => r.RefundTotal);
            ViewBag.TransactionCount = sales.Count;
            ViewBag.AverageSale = sales.Count > 0 ? sales.Average(s => s.Total) : 0;

            return View();
        }

        // GET: /Reports/Stock
        public async Task<IActionResult> Stock()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }
    }
}