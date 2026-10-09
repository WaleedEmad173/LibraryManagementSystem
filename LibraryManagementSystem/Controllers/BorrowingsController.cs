using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class BorrowingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BorrowingsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Borrowings/ or /Borrowings/Index (My Borrowings & Circulation List)
        [HttpGet]
        public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Borrowings
                .Include(b => b.Book)
                .ThenInclude(bk => bk!.Category)
                .Include(b => b.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("Borrowed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.ReturnDate == null);
                }
                else if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.ReturnDate != null);
                }
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BorrowDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.BorrowDate <= toDate.Value.AddDays(1));
            }

            var borrowingsList = await query
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            var viewModel = new BorrowingIndexViewModel
            {
                StatusFilter = status,
                FromDate = fromDate,
                ToDate = toDate,
                TotalBorrowedCount = await _context.Borrowings.CountAsync(),
                CurrentlyBorrowedCount = await _context.Borrowings.CountAsync(b => b.ReturnDate == null),
                OverdueCount = await _context.Borrowings.CountAsync(b => b.ReturnDate == null && b.BorrowDate.AddDays(14) < DateTime.Now),
                Borrowings = borrowingsList.Select(b => new BorrowingHistoryItemViewModel
                {
                    BorrowingId = b.Id,
                    BookId = b.BookId,
                    BookTitle = b.Book?.Title ?? "Unknown Title",
                    BookAuthor = b.Book?.Author ?? "Unknown Author",
                    BookISBN = b.Book?.ISBN ?? "N/A",
                    CategoryName = b.Book?.Category?.Name ?? "General",
                    BorrowerName = b.User?.FullName ?? (b.User?.UserName ?? "Member"),
                    MemberId = $"#MB-{b.UserId:D4}",
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: /Borrowings/Confirm/{bookId?} (Figma design view)
        [HttpGet]
        public async Task<IActionResult> Confirm(int? id, string? simulate)
        {
            // Resolve Book
            Book? book = null;
            if (id.HasValue && id.Value > 0)
            {
                book = await _context.Books
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id.Value);
            }

            if (book == null)
            {
                book = await _context.Books
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync() ?? new Book
                    {
                        Id = 1,
                        Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                        Author = "Robert C. Martin (Uncle Bob)",
                        ISBN = "978-0132350884",
                        TotalCopies = 5,
                        AvailableCopies = 3,
                        Description = "A Handbook of Agile Software Craftsmanship"
                    };
            }

            // Resolve Borrower (Current logged-in user or fallback to demo member)
            ApplicationUser? borrower = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                borrower = await _userManager.GetUserAsync(User);
            }

            if (borrower == null)
            {
                borrower = await _userManager.FindByEmailAsync("sara@library.com")
                    ?? await _userManager.Users.FirstOrDefaultAsync()
                    ?? new ApplicationUser
                    {
                        Id = 4820,
                        FullName = "Sara Ahmed",
                        Email = "sara@library.com",
                        UserName = "sara@library.com"
                    };
            }

            var model = new ConfirmBorrowViewModel
            {
                BookId = book.Id,
                Title = book.Title,
                Author = string.IsNullOrWhiteSpace(book.Author) ? "Robert C. Martin (Uncle Bob)" : book.Author,
                ISBN = string.IsNullOrWhiteSpace(book.ISBN) ? "978-0132350884" : book.ISBN,
                CategoryName = book.Category?.Name ?? "ENGINEERING / TECH",
                TotalCopies = book.TotalCopies > 0 ? book.TotalCopies : 5,
                AvailableCopies = book.AvailableCopies,
                Edition = "1st Edition (Revised)",
                ShelfLocation = "2nd Floor • Section T4, Shelf 12",
                Barcode = "BC-9942-0193-CC",
                AccessStatus = book.AvailableCopies > 0 ? "Ready for immediate pickup" : "Currently Unavailable",
                StandardLoanPeriodDays = 14,
                UserId = borrower.Id,
                BorrowerName = string.IsNullOrWhiteSpace(borrower.FullName) ? "Sara Ahmed" : borrower.FullName,
                MemberId = $"#MB-{borrower.Id:D4}",
                BorrowerTier = "Standard Academic",
                BorrowerInitials = GetInitials(borrower.FullName),
                CheckoutDate = DateTime.Today,
                ScheduledDueDate = DateTime.Today.AddDays(14),
                PickupDesk = "Central Academic Library - Main Desk (Level 1)"
            };

            // Developer / Sandbox Simulation States (from query parameter)
            if (!string.IsNullOrEmpty(simulate))
            {
                model.IsSimulated = true;
                model.ActiveFeedbackState = simulate.ToLowerInvariant();
                switch (model.ActiveFeedbackState)
                {
                    case "success":
                        model.FeedbackBadge = "HTTP 200 OK";
                        model.FeedbackMessage = $"Your loan has been processed. Due date is {model.ScheduledDueDate:MMM dd, yyyy}. Please pick up at Section T4. A receipt email has been sent to your registered address.";
                        break;
                    case "nostock":
                        model.FeedbackBadge = "HTTP 409 Conflict";
                        model.FeedbackMessage = $"All {model.TotalCopies} copies are currently on loan. You may place a reservation hold from the catalog to be queued automatically once returned.";
                        break;
                    case "duplicate":
                        model.FeedbackBadge = "Policy Violation";
                        model.FeedbackMessage = $"Your account already has an active loan for '{model.Title}'. Members are strictly limited to 1 concurrent copy per title to ensure equitable catalog distribution.";
                        break;
                }
            }

            return View(model);
        }

        // POST: /Borrowings/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmBorrowViewModel input)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == input.BookId);

            if (book == null)
            {
                return NotFound("Book was not found in the catalog.");
            }

            // Resolve User
            int userId = input.UserId;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    userId = currentUser.Id;
                }
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync("sara@library.com");
                if (user != null) userId = user.Id;
            }

            // Guard Rail 1: Inventory stock availability
            if (book.AvailableCopies <= 0)
            {
                input.ActiveFeedbackState = "nostock";
                input.FeedbackBadge = "HTTP 409 Conflict";
                input.FeedbackMessage = $"All {book.TotalCopies} copies are currently on loan. You may place a reservation hold from the catalog to be queued automatically once returned.";
                input.AvailableCopies = 0;
                return View(input);
            }

            // Guard Rail 2: Prevent duplicate active borrowing of the same book by the same member
            var existingActiveLoan = await _context.Borrowings
                .AnyAsync(b => b.BookId == book.Id && b.UserId == userId && b.ReturnDate == null);

            if (existingActiveLoan)
            {
                input.ActiveFeedbackState = "duplicate";
                input.FeedbackBadge = "Policy Violation";
                input.FeedbackMessage = $"Your account already has an active loan for '{book.Title}'. Members are strictly limited to 1 concurrent copy per title to ensure equitable catalog distribution.";
                return View(input);
            }

            // Process Loan Transaction
            book.AvailableCopies -= 1;

            var borrowing = new Borrowing
            {
                BookId = book.Id,
                UserId = userId,
                BorrowDate = DateTime.Now,
                ReturnDate = null
            };

            _context.Borrowings.Add(borrowing);
            await _context.SaveChangesAsync();

            // Set Success state
            input.AvailableCopies = book.AvailableCopies;
            input.ActiveFeedbackState = "success";
            input.FeedbackBadge = "HTTP 200 OK";
            input.FeedbackMessage = $"Your loan has been processed. Due date is {DateTime.Today.AddDays(14):MMM dd, yyyy}. Please pick up at Section T4. A receipt email has been sent to your registered address.";

            return View(input);
        }

        // POST: /Borrowings/Return/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            if (borrowing.ReturnDate != null)
            {
                TempData["Error"] = "This borrowing record was already returned.";
                return RedirectToAction(nameof(Index));
            }

            borrowing.ReturnDate = DateTime.Now;
            if (borrowing.Book != null)
            {
                borrowing.Book.AvailableCopies += 1;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Book returned successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Helper for initials
        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "SA";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
        }
    }
}
