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
        public async Task<IActionResult> Index(string? search = null, string? sort = null)
        {
            var query = _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(book => book.Borrowings)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            var categories = await query.ToListAsync();

            int totalBooksVolume = categories.Sum(c => c.Books.Count);

            var items = categories.Select(c =>
            {
                int count = c.Books.Count;
                double pct = totalBooksVolume > 0 ? Math.Round((double)count / totalBooksVolume * 100, 1) : 0;
                string name = c.Name;

                string icon = name.ToLower() switch
                {
                    "fiction" => "bi-book-half",
                    "science" => "bi-radioactive",
                    "history" => "bi-bank",
                    "technology" => "bi-laptop",
                    "biography" => "bi-person-bounding-box",
                    "children" => "bi-balloon",
                    _ => "bi-folder2-open"
                };

                int totalCopies = c.Books.Sum(book => book.TotalCopies);
                int activeLoans = c.Books.Sum(book => book.Borrowings.Count(loan => loan.ReturnDate == null));

                return new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    BooksCount = count,
                    Code = $"ID: {c.Id}",
                    DepartmentNote = $"{totalCopies} copies • {activeLoans} active loans",
                    Percentage = pct,
                    TotalCopies = totalCopies,
                    InCirculationLoans = activeLoans,
                    IconClass = icon,
                    ProgressColor = "#1e3a8a"
                };
            }).ToList();

            if (sort == "name")
            {
                items = items.OrderBy(i => i.Name).ToList();
            }
            else
            {
                items = items.OrderByDescending(i => i.BooksCount).ToList();
            }

            var largest = items.OrderByDescending(i => i.BooksCount).FirstOrDefault();

            var vm = new CategoryListViewModel
            {
                Categories = items,
                TotalCategories = categories.Count,
                CategorizedBooks = totalBooksVolume,
                LargestCategoryName = largest?.Name ?? "—",
                LargestCategoryCount = largest?.BooksCount ?? 0,
                LargestCategoryShare = largest?.Percentage ?? 0,
                Search = search,
                Sort = sort ?? "volume"
            };

            ViewData["Title"] = "Categories";
            ViewData["Section"] = "Categories";
            return View(vm);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Category";
            ViewData["Section"] = "Categories";
            return View(new CategoryViewModel());
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            ViewData["Title"] = "Create Category";
            ViewData["Section"] = "Categories";

            if (!ModelState.IsValid)
                return View(model);

            // Server-side rule: name must be unique (case-insensitive)
            bool exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == model.Name.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("Name", "This category already exists");
                return View(model);
            }

            _context.Categories.Add(new Category { Name = model.Name.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return NotFound();

            int booksCount = category.Books.Count;
            int totalCopies = category.Books.Sum(book => book.TotalCopies);
            int activeLoans = category.Books.SelectMany(book => book.Borrowings).Count(loan => loan.ReturnDate == null);

            string name = category.Name;

            var model = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                BooksCount = booksCount,
                TotalCopies = totalCopies,
                InCirculationLoans = activeLoans
            };

            ViewData["Title"] = "Edit Category";
            ViewData["Section"] = "Categories";
            return View(model);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            ViewData["Title"] = "Edit Category";
            ViewData["Section"] = "Categories";

            if (!ModelState.IsValid)
                return View(model);

            bool exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == model.Name.Trim().ToLower() && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "This category already exists");
                return View(model);
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = model.Name.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            int booksCount = category.Books.Count;
            int activeLoans = category.Books.SelectMany(b => b.Borrowings).Count(br => br.ReturnDate == null);
            int uniqueAuthors = category.Books
                .Select(book => book.Author)
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Distinct()
                .Count();

            string name = category.Name;
            string code = $"#CAT-{category.Id:D2}";

            var excerptList = new List<CategoryBookExcerptViewModel>();

            if (category.Books.Any())
            {
                excerptList = category.Books.Take(5).Select(b => new CategoryBookExcerptViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    HasActiveLoan = b.Borrowings.Any(br => br.ReturnDate == null)
                }).ToList();
            }

            bool canDelete = booksCount == 0 && activeLoans == 0;

            var model = new DeleteCategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                BooksCount = booksCount,
                ActiveLoansCount = activeLoans,
                UniqueAuthorsCount = uniqueAuthors,
                TaxonomyCode = code,
                CanDelete = canDelete,
                BlockMessage = canDelete ? null : "This category cannot be deleted because it contains books.",
                AssociatedBooks = excerptList
            };

            ViewData["Title"] = "Delete Category Confirmation";
            ViewData["Section"] = "Categories";
            return View(model);
        }

        // POST: /Categories/PurgeBooks/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeBooks(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            int purgedCount = category.Books.Count;

            if (category.Books.Any())
            {
                // Remove all borrowings associated with these books first to avoid FK constraints
                var borrowings = category.Books.SelectMany(b => b.Borrowings).ToList();
                if (borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(borrowings);
                }

                // Remove all books
                _context.Books.RemoveRange(category.Books);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = purgedCount > 0
                ? $"Successfully purged {purgedCount} associated books. Category '{category.Name}' is now unlinked and safe to delete."
                : $"Successfully purged all associated books. Category '{category.Name}' is now unlinked and safe to delete.";

            return RedirectToAction(nameof(Delete), new { id });
        }

        // POST: /Categories/DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int bookId, int categoryId)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.Id == bookId && b.CategoryId == categoryId);

            if (book != null)
            {
                if (book.Borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(book.Borrowings);
                }
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Book \"{book.Title}\" was successfully removed.";
            }
            else
            {
                TempData["Success"] = "Book was successfully removed from category.";
            }

            return RedirectToAction(nameof(Delete), new { id = categoryId });
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
                TempData["Error"] = "This category cannot be deleted because it still contains books in the database. Please purge them first.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Category '{category.Name}' was permanently removed from registry.";
            return RedirectToAction(nameof(Index));
        }
    }
}
