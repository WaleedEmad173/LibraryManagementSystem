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
        public IActionResult Index(string? status, DateTime? fromDate, DateTime? toDate)
        {
            return RedirectToPage("/Borrowings/Index", new { status, fromDate, toDate });
        }

        // GET: /Borrowings/Confirm/{bookId?} (Figma design view)
        [HttpGet]
        public IActionResult Confirm(int? id)
        {
            return RedirectToPage("/Borrowings/Borrow", new { bookId = id });
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
            if (user == null) return BadRequest("A valid member must be selected.");

            // Guard Rail 1: Inventory stock availability
            if (book.AvailableCopies <= 0)
            {
                input.ActiveFeedbackState = "nostock";
                input.FeedbackBadge = "HTTP 409 Conflict";
                input.FeedbackMessage = $"All {book.TotalCopies} copies are currently on loan. You may place a reservation hold from the catalog to be queued automatically once returned.";
                input.AvailableCopies = 0;
                TempData["Error"] = "No copies are available for this book.";
                return RedirectToPage("/Borrowings/Borrow", new { bookId = book.Id, userId });
            }

            // Guard Rail 2: Prevent duplicate active borrowing of the same book by the same member
            var existingActiveLoan = await _context.Borrowings
                .AnyAsync(b => b.BookId == book.Id && b.UserId == userId && b.ReturnDate == null);

            if (existingActiveLoan)
            {
                input.ActiveFeedbackState = "duplicate";
                input.FeedbackBadge = "Policy Violation";
                input.FeedbackMessage = $"Your account already has an active loan for '{book.Title}'. Members are strictly limited to 1 concurrent copy per title to ensure equitable catalog distribution.";
                TempData["Error"] = "This member already has an active loan for this book.";
                return RedirectToPage("/Borrowings/Borrow", new { bookId = book.Id, userId });
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
            TempData["Success"] = $"Loan #{borrowing.Id:D4} recorded. Due date: {borrowing.BorrowDate.AddDays(14):MMM dd, yyyy}.";
            return RedirectToPage("/Borrowings/Borrow", new { bookId = book.Id, userId });
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
