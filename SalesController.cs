using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditLogger _audit;

        public SalesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, AuditLogger audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        // GET: /Sales/Pos -- the checkout screen itself
        public async Task<IActionResult> Pos()
        {
            // Walk-in Customer alag se pass karte hain (default selected), baaki list mein woh dobara nahi aata
            var walkIn = await _context.Customers.FirstOrDefaultAsync(c => c.IsWalkIn);
            ViewBag.WalkInCustomerId = walkIn?.Id;
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive && !c.IsWalkIn).OrderBy(c => c.Name).ToListAsync();
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
            ViewBag.TaxRate = settings?.TaxRate ?? 0;
            ViewBag.CurrencySymbol = settings?.CurrencySymbol ?? "Rs.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            var query = _context.Products.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || (p.Barcode != null && p.Barcode == term));

            var results = await query
                .OrderBy(p => p.Name)
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Sku,
                    p.Barcode,
                    p.SellingPrice,
                    p.IsTaxable,
                    p.CurrentStock
                })
                .ToListAsync();

            return Ok(results);
        }

        public class CartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class CompleteSaleDto
        {
            public List<CartItemDto> Items { get; set; } = new();
            public int? CustomerId { get; set; }
            public string PaymentMethod { get; set; } = "Cash";
            public decimal AmountReceived { get; set; }
            public decimal OrderDiscount { get; set; } = 0;
            public string? Note { get; set; }
        }

        // POST: /Sales/Complete -- this is where the actual transaction happens
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete([FromBody] CompleteSaleDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "Cart is empty." });

            var settings = await _context.BusinessSettings.FirstOrDefaultAsync()
                ?? new BusinessSetting(); // fallback defaults if none configured yet

            // load all needed products in one query (not one query per line -- efficient)
            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.IsActive)
                .ToDictionaryAsync(p => p.Id);

            decimal subtotal = 0;
            decimal taxTotal = 0;
            var saleItems = new List<SaleItem>();

            // --- VALIDATION PASS: check everything before touching the database ---
            foreach (var line in dto.Items)
            {
                if (!products.TryGetValue(line.ProductId, out var product))
                    return BadRequest(new { message = $"Product {line.ProductId} not found or inactive." });

                if (line.Quantity <= 0)
                    return BadRequest(new { message = $"Invalid quantity for {product.Name}." });

                if (product.CurrentStock < line.Quantity)
                    return BadRequest(new { message = $"Insufficient stock for {product.Name}. Available: {product.CurrentStock}." });

                var lineTotal = product.SellingPrice * line.Quantity;
                subtotal += lineTotal;

                if (product.IsTaxable)
                    taxTotal += lineTotal * (settings.TaxRate / 100m);

                saleItems.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,   // BR-009: snapshot, survives future edits
                    Quantity = line.Quantity,
                    UnitPrice = product.SellingPrice,
                    LineTotal = lineTotal
                });
            }

            var discount = Math.Min(dto.OrderDiscount, subtotal); // discount can't exceed subtotal
            var total = subtotal - discount + taxTotal;

            if (dto.PaymentMethod == "Cash" && dto.AmountReceived < total)
                return BadRequest(new { message = "Received amount is less than the total due." });

            var changeDue = dto.PaymentMethod == "Cash" ? dto.AmountReceived - total : 0;

            // invoice number: prefix + next sequential number
            var lastInvoiceCount = await _context.Sales.CountAsync();
            var invoiceNo = $"{settings.InvoicePrefix}{(lastInvoiceCount + 1).ToString("D5")}";

            var sale = new Sale
            {
                InvoiceNo = invoiceNo,
                CustomerId = dto.CustomerId,
                CashierId = _userManager.GetUserId(User)!,
                Subtotal = subtotal,
                Discount = discount,
                Tax = taxTotal,
                Total = total,
                PaymentMethod = dto.PaymentMethod,
                AmountReceived = dto.AmountReceived,
                ChangeDue = changeDue,
                Note = dto.Note,
                Items = saleItems
            };

            // if anything fails, NOTHING commits (BR-006 / FR-057 requirement).
            foreach (var line in dto.Items)
            {
                products[line.ProductId].CurrentStock -= line.Quantity;
            }

            _context.Sales.Add(sale);

            // audit entry isi SaveChangesAsync ke saath jayegi -- atomic rehta hai
            _audit.Log(_userManager.GetUserId(User)!, "Complete", "Sale", invoiceNo, $"Completed sale {invoiceNo}, total Rs.{total:N2}");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                saleId = sale.Id,
                invoiceNo = sale.InvoiceNo,
                subtotal,
                discount,
                tax = taxTotal,
                total,
                changeDue
            });
        }

        // GET: /Sales -- sales history list
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.DateTime)
                .Take(100)
                .ToListAsync();
            return View(sales);
        }

        // GET: /Sales/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();
            return View(sale);
        }
    }
}