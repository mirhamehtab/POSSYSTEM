using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;


[Authorize(Roles = "Administrator,Manager")]
public class ExpenseCategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    public ExpenseCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: EXPENSECATEGORYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.ExpenseCategories.ToListAsync());
    }

    // GET: EXPENSECATEGORYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expensecategory = await _context.ExpenseCategories
            .FirstOrDefaultAsync(m => m.Id == id);
        if (expensecategory == null)
        {
            return NotFound();
        }

        return View(expensecategory);
    }

    // GET: EXPENSECATEGORYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: EXPENSECATEGORYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,IsActive")] ExpenseCategory expensecategory)
    {
        if (ModelState.IsValid)
        {
            _context.Add(expensecategory);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(expensecategory);
    }

    // GET: EXPENSECATEGORYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expensecategory = await _context.ExpenseCategories.FindAsync(id);
        if (expensecategory == null)
        {
            return NotFound();
        }
        return View(expensecategory);
    }

    // POST: EXPENSECATEGORYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,IsActive")] ExpenseCategory expensecategory)
    {
        if (id != expensecategory.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(expensecategory);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpenseCategoryExists(expensecategory.Id))
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
        return View(expensecategory);
    }

    // GET: EXPENSECATEGORYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expensecategory = await _context.ExpenseCategories
            .FirstOrDefaultAsync(m => m.Id == id);
        if (expensecategory == null)
        {
            return NotFound();
        }

        return View(expensecategory);
    }

    // POST: EXPENSECATEGORYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var expensecategory = await _context.ExpenseCategories.FindAsync(id);
        if (expensecategory != null)
        {
            _context.ExpenseCategories.Remove(expensecategory);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ExpenseCategoryExists(int? id)
    {
        return _context.ExpenseCategories.Any(e => e.Id == id);
    }
}
