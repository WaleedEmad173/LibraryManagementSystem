using System.Text.RegularExpressions;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Helpers;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private const int PageSize = 8;
        private readonly AppDbContext _context;

        public BookController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Book?search=...&categoryId=...&sortOrder=...&page=2
        public async Task<IActionResult> Index(
            string? search, int? categoryId, bool availableOnly = false,
            string? sortOrder = null, int page = 1)
        {
            var query = _context.Books.AsNoTracking().Include(b => b.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(b =>
                    EF.Functions.Like(b.Title, $"%{term}%") ||
                    EF.Functions.Like(b.Author, $"%{term}%") ||
                    EF.Functions.Like(b.ISBN, $"%{term}%"));
            }

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId.Value);

            if (availableOnly)
                query = query.Where(b => b.AvailableCopies > 0);

            query = sortOrder switch
            {
                "title_desc" => query.OrderByDescending(b => b.Title),
                "author" => query.OrderBy(b => b.Author).ThenBy(b => b.Title),
                "author_desc" => query.OrderByDescending(b => b.Author).ThenBy(b => b.Title),
                _ => query.OrderBy(b => b.Title)
            };

            var books = await PaginatedList<Book>.CreateAsync(query, page, PageSize);

            // Stat cards always describe the whole catalog (not the filtered list)
            var totalCopies = await _context.Books.SumAsync(b => b.TotalCopies);
            var availableCopies = await _context.Books.SumAsync(b => b.AvailableCopies);

            var vm = new BookIndexViewModel
            {
                Books = books,
                Search = search,
                CategoryId = categoryId,
                AvailableOnly = availableOnly,
                SortOrder = sortOrder,
                Categories = await GetCategoriesSelectListAsync(categoryId),
                TotalTitles = await _context.Books.CountAsync(),
                CatalogCopies = totalCopies,
                AvailableCopies = availableCopies,
                CopiesOnLoan = totalCopies - availableCopies,
                ZeroAvailable = await _context.Books.CountAsync(b => b.AvailableCopies == 0)
            };

            return View(vm);
        }

        // GET: /Book/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vm = await BuildDetailsAsync(id.Value);
            return vm == null ? NotFound() : View(vm);
        }

        // GET: /Book/Create
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create()
        {
            return View(new BookFormViewModel
            {
                Categories = await GetCategoriesSelectListAsync()
            });
        }

        // POST: /Book/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            await ValidateIsbnAndCategoryAsync(vm, currentBookId: 0);

            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoriesSelectListAsync(vm.CategoryId);
                return View(vm);
            }

            var book = new Book
            {
                Title = vm.Title.Trim(),
                Author = vm.Author.Trim(),
                ISBN = NormalizeIsbn(vm.ISBN),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                CategoryId = vm.CategoryId!.Value,
                TotalCopies = vm.TotalCopies!.Value,
                AvailableCopies = vm.TotalCopies.Value // new books start fully available
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{book.Title}\" was added to the catalog.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Book/Edit/5
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            return View(new BookFormViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Description = book.Description,
                CategoryId = book.CategoryId,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                CopiesOnLoan = book.TotalCopies - book.AvailableCopies,
                Categories = await GetCategoriesSelectListAsync(book.CategoryId)
            });
        }

        // POST: /Book/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            var onLoan = book.TotalCopies - book.AvailableCopies;

            if (vm.TotalCopies.HasValue && vm.TotalCopies.Value < onLoan)
            {
                ModelState.AddModelError(nameof(vm.TotalCopies),
                    $"Total copies can't be lower than the {onLoan} copies currently on loan.");
            }

            await ValidateIsbnAndCategoryAsync(vm, currentBookId: id);

            if (!ModelState.IsValid)
            {
                vm.CopiesOnLoan = onLoan;
                vm.AvailableCopies = book.AvailableCopies;
                vm.Categories = await GetCategoriesSelectListAsync(vm.CategoryId);
                return View(vm);
            }

            book.Title = vm.Title.Trim();
            book.Author = vm.Author.Trim();
            book.ISBN = NormalizeIsbn(vm.ISBN);
            book.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            book.CategoryId = vm.CategoryId!.Value;
            book.TotalCopies = vm.TotalCopies!.Value;
            book.AvailableCopies = book.TotalCopies - onLoan; // keep loans intact

            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{book.Title}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Book/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var vm = await BuildDetailsAsync(id.Value);
            return vm == null ? NotFound() : View(vm);
        }

        // POST: /Book/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vm = await BuildDetailsAsync(id);
            if (vm == null) return NotFound();

            if (!vm.CanDelete)
            {
                TempData["Error"] = $"\"{vm.Book.Title}\" can't be deleted while it has loans or borrowing history.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var book = await _context.Books.FindAsync(id);
            _context.Books.Remove(book!);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{vm.Book.Title}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private async Task<BookDetailsViewModel?> BuildDetailsAsync(int id)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return null;

            var historyCount = await _context.Books
                .Where(b => b.Id == id)
                .Select(b => b.Borrowings.Count)
                .FirstOrDefaultAsync();

            return new BookDetailsViewModel { Book = book, HistoryCount = historyCount };
        }

        // Strips hyphens/spaces so "978-0-13-235088-4" and "9780132350884" are treated as the same ISBN
        private static string NormalizeIsbn(string? isbn) =>
            (isbn ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        private async Task ValidateIsbnAndCategoryAsync(BookFormViewModel vm, int currentBookId)
        {
            var isbn = NormalizeIsbn(vm.ISBN);

            if (!string.IsNullOrEmpty(isbn))
            {
                if (!Regex.IsMatch(isbn, @"^(\d{13}|\d{9}[\dX])$"))
                {
                    ModelState.AddModelError(nameof(vm.ISBN), "ISBN must be 10 or 13 digits.");
                }
                else if (await _context.Books.AnyAsync(b =>
                             b.ISBN.Replace("-", "") == isbn && b.Id != currentBookId))
                {
                    ModelState.AddModelError(nameof(vm.ISBN), "ISBN already exists.");
                }
            }

            if (vm.CategoryId.HasValue &&
                !await _context.Categories.AnyAsync(c => c.Id == vm.CategoryId.Value))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Please select a valid category.");
            }
        }

        private async Task<SelectList> GetCategoriesSelectListAsync(int? selectedId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return new SelectList(categories, "Id", "Name", selectedId);
        }
    }
}