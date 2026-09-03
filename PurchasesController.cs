using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditLogger _audit;

        public PurchasesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, AuditLogger audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        // GET: /Purchases -- list
        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
            return View(purchases);
        }

        // GET: /Purchases/Create -- new draft purchase screen
        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View();
        }

        public class PurchaseLineDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitCost { get; set; }
        }

        public class CreatePurchaseDto
        {
            public int SupplierId { get; set; }
            public string? SupplierInvoiceNo { get; set; }
            public string? Notes { get; set; }
            public List<PurchaseLineDto> Items { get; set; } = new();
        }

        // POST: /Purchases/SaveDraft -- saves as Draft, does NOT touch stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] CreatePurchaseDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "Add at least one item." });

            var items = dto.Items.Select(i => new PurchaseItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                LineTotal = i.Quantity * i.UnitCost
            }).ToList();

            var subtotal = items.Sum(i => i.LineTotal);

            var purchaseCount = await _context.Purchases.CountAsync();
            var purchase = new Purchase
            {
                PurchaseNo = $"PO-{(purchaseCount + 1).ToString("D5")}",
                SupplierId = dto.SupplierId,
                SupplierInvoiceNo = dto.SupplierInvoiceNo,
                Notes = dto.Notes,
                Status = "Draft",
                Subtotal = subtotal,
                Total = subtotal,   // koi tax/discount abhi purchase pe nahi laga rahe, simple rakha hai
                Items = items
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            return Ok(new { purchaseId = purchase.Id, purchaseNo = purchase.PurchaseNo });
        }

        // POST: /Purchases/Receive/5 -- THIS is what actually adds stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();

            if (purchase.Status == "Received")
            {
                TempData["Error"] = "This purchase was already received.";
                return RedirectToAction(nameof(Index));
            }

            var productIds = purchase.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // stock increase karo har line ke liye, aur product ka cost price bhi update karo
            foreach (var item in purchase.Items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    product.CurrentStock += item.Quantity;
                    product.CostPrice = item.UnitCost;   // FR-080: latest cost price update hota hai
                }
            }

            purchase.Status = "Received";
            purchase.ReceivedAt = DateTime.Now;

            _audit.Log(_userManager.GetUserId(User)!, "Receive", "Purchase", purchase.PurchaseNo, $"Received purchase {purchase.PurchaseNo}, stock updated");

            // ek hi SaveChangesAsync -- stock updates + purchase status change + audit entry dono atomic hain
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Purchase {purchase.PurchaseNo} received. Stock updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Purchases/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();
            return View(purchase);
        }
    }
}