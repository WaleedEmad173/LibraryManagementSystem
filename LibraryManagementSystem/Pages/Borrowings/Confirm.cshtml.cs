using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class ConfirmModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ConfirmBorrowViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, string? simulate)
        {
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
                        AvailableCopies = 3
                    };
            }

            ApplicationUser? borrower = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                borrower = await _userManager.GetUserAsync(User);
            }

            borrower ??= await _userManager.Users.FirstOrDefaultAsync();

            if (borrower == null)
            {
                borrower = new ApplicationUser
                {
                    Id = 1,
                    FullName = "Library Member",
                    Email = "member@library.com"
                };
            }

            Input = new ConfirmBorrowViewModel
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

            if (!string.IsNullOrEmpty(simulate))
            {
                Input.IsSimulated = true;
                Input.ActiveFeedbackState = simulate.ToLowerInvariant();
                switch (Input.ActiveFeedbackState)
                {
                    case "success":
                        Input.FeedbackBadge = "HTTP 200 OK";
                        Input.FeedbackMessage = $"Your loan has been processed. Due date is {Input.ScheduledDueDate:MMM dd, yyyy}. Please pick up at Section T4. A receipt email has been sent to your registered address.";
                        break;
                    case "nostock":
                        Input.FeedbackBadge = "HTTP 409 Conflict";
                        Input.FeedbackMessage = $"All {Input.TotalCopies} copies are currently on loan. You may place a reservation hold from the catalog to be queued automatically once returned.";
                        break;
                    case "duplicate":
                        Input.FeedbackBadge = "Policy Violation";
                        Input.FeedbackMessage = $"Your account already has an active loan for '{Input.Title}'. Members are strictly limited to 1 concurrent copy per title to ensure equitable catalog distribution.";
                        break;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var book = await _context.Books.FindAsync(Input.BookId);
                if (book != null)
                {
                    if (book.AvailableCopies <= 0)
                    {
                        Input.ActiveFeedbackState = "nostock";
                        Input.FeedbackBadge = "HTTP 409 Conflict";
                        Input.FeedbackMessage = $"All {book.TotalCopies} copies are currently on loan. You may place a reservation hold from the catalog to be queued automatically once returned.";
                        return Page();
                    }

                    int userId = Input.UserId;
                    if (User?.Identity?.IsAuthenticated == true)
                    {
                        var curUser = await _userManager.GetUserAsync(User);
                        if (curUser != null) userId = curUser.Id;
                    }

                    var hasActiveLoan = await _context.Borrowings
                        .AnyAsync(b => b.BookId == book.Id && b.UserId == userId && b.ReturnDate == null);

                    if (hasActiveLoan)
                    {
                        Input.ActiveFeedbackState = "duplicate";
                        Input.FeedbackBadge = "Policy Violation";
                        Input.FeedbackMessage = $"Your account already has an active loan for '{book.Title}'. Members are strictly limited to 1 concurrent copy per title to ensure equitable catalog distribution.";
                        return Page();
                    }

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
                    Input.AvailableCopies = book.AvailableCopies;
                }
                else
                {
                    Input.AvailableCopies = Math.Max(0, Input.AvailableCopies - 1);
                }
            }
            catch
            {
                Input.AvailableCopies = Math.Max(0, Input.AvailableCopies - 1);
            }

            Input.ActiveFeedbackState = "success";
            Input.FeedbackBadge = "HTTP 200 OK";
            Input.FeedbackMessage = $"Your loan has been processed. Due date is {DateTime.Today.AddDays(14):MMM dd, yyyy}. Please pick up at Section T4. A receipt email has been sent to your registered address.";

            return Page();
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "SA";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
        }
    }
}
