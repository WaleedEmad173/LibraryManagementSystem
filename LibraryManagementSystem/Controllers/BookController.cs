using LibraryManagementSystem.Data;
using LibraryManagementSystem.Helpers;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
                query = query.Where(b => b.TotalCopies > b.Borrowings.Count(loan => loan.ReturnDate == null));

            query = sortOrder switch
            {
                "title_desc" => query.OrderByDescending(b => b.Title),
                "author" => query.OrderBy(b => b.Author).ThenBy(b => b.Title),
                "author_desc" => query.OrderByDescending(b => b.Author).ThenBy(b => b.Title),
                _ => query.OrderBy(b => b.Title)
            };

            var books = await PaginatedList<Book>.CreateAsync(query, page, PageSize);
            var activeLoanCounts = await _context.Borrowings
                .Where(loan => loan.ReturnDate == null && books.Select(book => book.Id).Contains(loan.BookId))
                .GroupBy(loan => loan.BookId)
                .Select(group => new { BookId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.BookId, group => group.Count);
            foreach (var listedBook in books)
                listedBook.AvailableCopies = Math.Max(0, listedBook.TotalCopies - activeLoanCounts.GetValueOrDefault(listedBook.Id));

            // Stat cards always describe the whole catalog (not the filtered list)
            var totalCopies = await _context.Books.SumAsync(b => b.TotalCopies);
            var copiesOnLoan = await _context.Borrowings.CountAsync(loan => loan.ReturnDate == null);
            var availableCopies = Math.Max(0, totalCopies - copiesOnLoan);

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
                CopiesOnLoan = copiesOnLoan,
                ZeroAvailable = await _context.Books.CountAsync(b => b.TotalCopies <= b.Borrowings.Count(loan => loan.ReturnDate == null))
            };

            return View(vm);
        }

        // GET: /Book/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vm = await BuildDetailsAsync(id.Value, includeBorrowingHistory: true);
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

            if (vm.Image != null)
            {
                book.ImageUrl = Upload.UploadFile("Images", vm.Image);
            }

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
            var activeLoans = await _context.Borrowings.CountAsync(b => b.BookId == book.Id && b.ReturnDate == null);
            var availableCopies = Math.Max(0, book.TotalCopies - activeLoans);

            return View(new BookFormViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Description = book.Description,
                CategoryId = book.CategoryId,
                TotalCopies = book.TotalCopies,
                AvailableCopies = availableCopies,
                CopiesOnLoan = activeLoans,
                ImageUrl = book.ImageUrl,
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

            var onLoan = await _context.Borrowings.CountAsync(b => b.BookId == book.Id && b.ReturnDate == null);
            var availableCopies = Math.Max(0, book.TotalCopies - onLoan);

            if (vm.TotalCopies.HasValue && vm.TotalCopies.Value < onLoan)
            {
                ModelState.AddModelError(nameof(vm.TotalCopies),
                    $"Total copies can't be lower than the {onLoan} copies currently on loan.");
            }

            await ValidateIsbnAndCategoryAsync(vm, currentBookId: id);

            if (!ModelState.IsValid)
            {
                vm.CopiesOnLoan = onLoan;
                vm.AvailableCopies = availableCopies;
                vm.Categories = await GetCategoriesSelectListAsync(vm.CategoryId);
                return View(vm);
            }

            book.Title = vm.Title.Trim();
            book.Author = vm.Author.Trim();
            book.ISBN = NormalizeIsbn(vm.ISBN);
            book.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            book.CategoryId = vm.CategoryId!.Value;
            book.TotalCopies = vm.TotalCopies!.Value;
            book.AvailableCopies = Math.Max(0, book.TotalCopies - onLoan); // keep active loans in sync

            if (vm.Image != null)
            {
                if (!string.IsNullOrEmpty(book.ImageUrl))
                {
                    Upload.RemoveFile("Images", book.ImageUrl);
                }
                book.ImageUrl = Upload.UploadFile("Images", vm.Image);
            }

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

            if (book != null && !string.IsNullOrEmpty(book.ImageUrl))
            {
                Upload.RemoveFile("Images", book.ImageUrl);
            }

            _context.Books.Remove(book!);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{vm.Book.Title}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private async Task<BookDetailsViewModel?> BuildDetailsAsync(int id, bool includeBorrowingHistory = false)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return null;

            var vm = new BookDetailsViewModel { Book = book };
            if (includeBorrowingHistory)
            {
                var borrowings = await _context.Borrowings
                    .AsNoTracking()
                    .Include(b => b.User)
                    .Where(b => b.BookId == id)
                    .OrderBy(b => b.ReturnDate.HasValue)
                    .ThenByDescending(b => b.BorrowDate)
                    .ThenByDescending(b => b.Id)
                    .ToListAsync();

                vm.Borrowings = borrowings
                    .Select(b => new BorrowingHistoryItemViewModel
                    {
                        BorrowingId = b.Id,
                        BookId = b.BookId,
                        BookTitle = book.Title,
                        BookAuthor = book.Author,
                        BookISBN = book.ISBN,
                        BorrowerName = b.User?.FullName ?? "Unknown member",
                        MemberId = $"#MB-{b.UserId:D4}",
                        BorrowDate = b.BorrowDate,
                        ReturnDate = b.ReturnDate
                    })
                    .ToList();

                vm.HistoryCount = vm.Borrowings.Count;
                vm.CurrentLoanCount = vm.Borrowings.Count(b => !b.IsReturned);
            }
            else
            {
                vm.CurrentLoanCount = await _context.Borrowings.CountAsync(b => b.BookId == id && b.ReturnDate == null);
                vm.HistoryCount = await _context.Books
                    .Where(b => b.Id == id)
                    .Select(b => b.Borrowings.Count)
                    .FirstOrDefaultAsync();
            }

            return vm;
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
