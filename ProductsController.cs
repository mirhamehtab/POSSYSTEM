
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using POSSystem.Services;

[Authorize(Roles = "Administrator,Manager")]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AuditLogger _audit;

    public ProductsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, AuditLogger audit)
    {
        _context = context;
        _userManager = userManager;
        _audit = audit;
    }

    // GET: PRODUCTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Products.ToListAsync());
    }

    // GET: PRODUCTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // GET: PRODUCTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PRODUCTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Sku,Barcode,Unit,CostPrice,SellingPrice,IsTaxable,CurrentStock,ReorderLevel,ImagePath,IsActive,CategoryId,Category")] Product product)
    {
        // blank barcode -> null, not "" -- the unique index allows many NULLs but not many blank strings
        if (string.IsNullOrWhiteSpace(product.Barcode)) product.Barcode = null;

        if (ModelState.IsValid)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: PRODUCTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // POST: PRODUCTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Sku,Barcode,Unit,CostPrice,SellingPrice,IsTaxable,CurrentStock,ReorderLevel,ImagePath,IsActive,CategoryId,Category")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        // blank barcode -> null, not "" -- the unique index allows many NULLs but not many blank strings
        if (string.IsNullOrWhiteSpace(product.Barcode)) product.Barcode = null;

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: PRODUCTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: PRODUCTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        // check karo koi SaleItem is product ko reference karti hai ya nahi
        bool hasSales = await _context.SaleItems.AnyAsync(si => si.ProductId == id);

        if (hasSales)
        {
            // delete nahi kar sakte -- historical sale records isay reference karte hain
            // isliye sirf deactivate karo
            product.IsActive = false;
            _audit.Log(_userManager.GetUserId(User)!, "Deactivate", "Product", product.Id.ToString(), $"Deactivated {product.Name} (referenced in past sales, could not delete)");
            await _context.SaveChangesAsync();
            TempData["Message"] = "This product is used in past sales, so it was deactivated instead of deleted.";
        }
        else
        {
            _audit.Log(_userManager.GetUserId(User)!, "Delete", "Product", product.Id.ToString(), $"Deleted {product.Name}");
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int? id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
