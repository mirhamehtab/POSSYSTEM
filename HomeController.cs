using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaySales = await _context.Sales
                .Where(s => s.DateTime >= today && s.DateTime < tomorrow)
                .ToListAsync();

            ViewBag.TodaySalesCount = todaySales.Count;
            ViewBag.TodayNetSales = todaySales.Sum(s => s.Total);

            ViewBag.LowStockProducts = await _context.Products
                .Where(p => p.IsActive && p.CurrentStock <= p.ReorderLevel)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            ViewBag.RecentSales = await _context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.DateTime)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public IActionResult Privacy() => View();
    }
}