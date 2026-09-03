using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Settings
        public async Task<IActionResult> Index()
        {
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

            // agar abhi tak koi settings record nahi bana, ek default bana do
            if (settings == null)
            {
                settings = new BusinessSetting();
                _context.BusinessSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        // POST: /Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(BusinessSetting model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var settings = await _context.BusinessSettings.FindAsync(model.Id);
            if (settings == null) return NotFound();

            settings.BusinessName = model.BusinessName;
            settings.Address = model.Address;
            settings.Phone = model.Phone;
            settings.TaxNumber = model.TaxNumber;
            settings.TaxRate = model.TaxRate;
            settings.TaxInclusive = model.TaxInclusive;
            settings.CurrencySymbol = model.CurrencySymbol;
            settings.ReceiptHeader = model.ReceiptHeader;
            settings.ReceiptFooter = model.ReceiptFooter;
            settings.InvoicePrefix = model.InvoicePrefix;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Settings saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}