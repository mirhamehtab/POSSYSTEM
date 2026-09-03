using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using POSSystem.Services;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class ReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditLogger _audit;

        public ReturnsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, AuditLogger audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

        // GET: /Returns -- list of processed returns
        public async Task<IActionResult> Index()
        {
            var returns = await _context.SaleReturns
                .Include(r => r.Sale)
                .OrderByDescending(r => r.DateTime)
                .ToListAsync();
            return View(returns);
        }

        // GET: /Returns/Find -- search box to find the original sale by invoice number
        public IActionResult Find()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchSale(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return Ok(new List<object>());

            var sale = await _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.InvoiceNo == invoiceNo);

            if (sale == null)
                return NotFound(new { message = "No sale found with that invoice number." });

            // har item ke liye batao ab tak kitni quantity return ho chuki hai,
            // taake dobara return karte waqt eligible quantity theek dikhe
            var alreadyReturned = await _context.SaleReturnItems
                .Where(ri => ri.SaleReturn != null && ri.SaleReturn.SaleId == sale.Id)
                .GroupBy(ri => ri.SaleItemId)
                .Select(g => new { SaleItemId = g.Key, Returned = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.SaleItemId, x => x.Returned);

            var result = new
            {
                saleId = sale.Id,
                invoiceNo = sale.InvoiceNo,
                items = sale.Items.Select(i => new
                {
                    saleItemId = i.Id,
                    productId = i.ProductId,
                    name = i.ProductNameSnapshot,
                    unitPrice = i.UnitPrice,
                    soldQty = i.Quantity,
                    alreadyReturned = alreadyReturned.ContainsKey(i.Id) ? alreadyReturned[i.Id] : 0,
                    eligibleQty = i.Quantity - (alreadyReturned.ContainsKey(i.Id) ? alreadyReturned[i.Id] : 0)
                })
            };

            return Ok(result);
        }

        public class ReturnLineDto
        {
            public int SaleItemId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        public class ProcessReturnDto
        {
            public int SaleId { get; set; }
            public string Reason { get; set; }
            public string? Notes { get; set; }
            public string RefundMethod { get; set; } = "Cash";
            public List<ReturnLineDto> Items { get; set; } = new();
        }

        // POST: /Returns/Process -- this is where stock gets restored
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process([FromBody] ProcessReturnDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "Select at least one item to return." });

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "A reason is required." });

            // --- VALIDATION: har line ke liye check karo ke return quantity eligible se zyada na ho ---
            var saleItemIds = dto.Items.Select(i => i.SaleItemId).ToList();
            var saleItems = await _context.SaleItems
                .Where(si => saleItemIds.Contains(si.Id))
                .ToDictionaryAsync(si => si.Id);

            var alreadyReturned = await _context.SaleReturnItems
                .Where(ri => saleItemIds.Contains(ri.SaleItemId))
                .GroupBy(ri => ri.SaleItemId)
                .Select(g => new { SaleItemId = g.Key, Returned = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.SaleItemId, x => x.Returned);

            decimal refundTotal = 0;
            var returnItems = new List<SaleReturnItem>();

            foreach (var line in dto.Items)
            {
                if (!saleItems.TryGetValue(line.SaleItemId, out var saleItem))
                    return BadRequest(new { message = "Invalid sale item." });

                var returnedSoFar = alreadyReturned.ContainsKey(line.SaleItemId) ? alreadyReturned[line.SaleItemId] : 0;
                var eligible = saleItem.Quantity - returnedSoFar;

                if (line.Quantity <= 0 || line.Quantity > eligible)
                    return BadRequest(new { message = $"Return quantity exceeds the eligible amount ({eligible}) for {saleItem.ProductNameSnapshot}." });

                var amount = saleItem.UnitPrice * line.Quantity;
                refundTotal += amount;

                returnItems.Add(new SaleReturnItem
                {
                    SaleItemId = line.SaleItemId,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    Amount = amount
                });
            }

            // --- stock wapas add karo ---
            var productIds = returnItems.Select(ri => ri.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var ri in returnItems)
            {
                if (products.TryGetValue(ri.ProductId, out var product))
                    product.CurrentStock += ri.Quantity;
            }

            var returnCount = await _context.SaleReturns.CountAsync();
            var saleReturn = new SaleReturn
            {
                ReturnNo = $"RET-{(returnCount + 1).ToString("D5")}",
                SaleId = dto.SaleId,
                ProcessedBy = _userManager.GetUserId(User)!,
                Reason = dto.Reason,
                Notes = dto.Notes,
                RefundMethod = dto.RefundMethod,
                RefundTotal = refundTotal,
                Items = returnItems
            };

            _context.SaleReturns.Add(saleReturn);

            _audit.Log(_userManager.GetUserId(User)!, "Process", "SaleReturn", saleReturn.ReturnNo, $"Processed return {saleReturn.ReturnNo}, refund Rs.{refundTotal:N2}");

            // ek hi SaveChangesAsync -- return record + stock restoration + audit entry dono atomic
            await _context.SaveChangesAsync();

            return Ok(new { returnNo = saleReturn.ReturnNo, refundTotal });
        }

        // GET: /Returns/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ret = await _context.SaleReturns
                .Include(r => r.Sale)
                .Include(r => r.Items)
                    .ThenInclude(i => i.SaleItem)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ret == null) return NotFound();
            return View(ret);
        }
    }
}