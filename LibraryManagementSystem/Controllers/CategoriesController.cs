using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian,Admin")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    BooksCount = c.Books.Count
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Server-side rule: name must be unique
            bool exists = await _context.Categories.AnyAsync(c => c.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError("Name", "This category already exists");
                return View(model);
            }

            _context.Categories.Add(new Category { Name = model.Name });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var model = new CategoryViewModel { Id = category.Id, Name = category.Name };
            return View(model);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            bool exists = await _context.Categories
                .AnyAsync(c => c.Name == model.Name && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "This category already exists");
                return View(model);
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = model.Name;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.Categories
                .Select(c => new DeleteCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    BooksCount = c.Books.Count
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (model == null)
                return NotFound();

            // Block delete if the category contains books
            if (model.BooksCount > 0)
            {
                model.CanDelete = false;
                model.BlockMessage = "This category cannot be deleted because it contains books.";
            }

            return View(model);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            if (category.Books.Any())
            {
                TempData["Error"] = "This category cannot be deleted because it contains books.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
