using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    [Authorize(Roles = "Admin,Librarian")]
    public class ReturnModel : PageModel
    {
        private readonly AppDbContext _context;

        public ReturnModel(AppDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public CirculationReturnPageViewModel ViewModel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadActiveLoansAsync(PageNumber);
            return Page();
        }

        public async Task<IActionResult> OnPostReturnAsync(int id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrowing == null)
            {
                ViewModel.AlertState = "error";
                ViewModel.AlertTitle = "Record Not Found";
                ViewModel.AlertMessage = "The selected circulation record could not be found.";
                await LoadActiveLoansAsync(PageNumber);
                return Page();
            }

            if (borrowing.ReturnDate.HasValue)
            {
                ViewModel.AlertState = "error";
                ViewModel.AlertTitle = "This borrowing was already returned";
                ViewModel.AlertReference = $"#LN-{borrowing.Id:D4}";
                ViewModel.AlertMessage = $"This record was returned on {borrowing.ReturnDate.Value:MMM dd, yyyy}.";
                await LoadActiveLoansAsync(PageNumber);
                return Page();
            }

            borrowing.ReturnDate = DateTime.Now;
            if (borrowing.Book != null)
            {
                var remainingActiveLoans = await _context.Borrowings.CountAsync(b => b.BookId == borrowing.BookId && b.Id != borrowing.Id && b.ReturnDate == null);
                borrowing.Book.AvailableCopies = Math.Max(0, borrowing.Book.TotalCopies - remainingActiveLoans);
            }

            await _context.SaveChangesAsync();
            await LoadActiveLoansAsync(PageNumber);

            ViewModel.AlertState = "success";
            ViewModel.AlertTitle = "Book returned successfully";
            ViewModel.AlertReference = $"#LN-{borrowing.Id:D4}";
            ViewModel.AlertBookTitle = borrowing.Book?.Title;
            ViewModel.AlertAvailableCopies = borrowing.Book?.AvailableCopies ?? 0;
            ViewModel.AlertMessage = $"Loan {ViewModel.AlertReference} closed. Available inventory for '{ViewModel.AlertBookTitle}' updated to {ViewModel.AlertAvailableCopies} units.";
            return Page();
        }

        private async Task LoadActiveLoansAsync(int pageNumber)
        {
            ViewModel.SearchQuery = SearchQuery;

            var query = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b => b.ReturnDate == null);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var term = SearchQuery.Trim().ToLower();
                query = query.Where(b =>
                    (b.Book != null && (b.Book.Title.ToLower().Contains(term) || b.Book.Author.ToLower().Contains(term) || b.Book.ISBN.ToLower().Contains(term))) ||
                    (b.User != null && ((b.User.FullName != null && b.User.FullName.ToLower().Contains(term)) || (b.User.Email != null && b.User.Email.ToLower().Contains(term)) || b.User.Id.ToString().Contains(term))));
            }

            const int pageSize = 7;
            var matchingCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(matchingCount / (double)pageSize));
            pageNumber = Math.Clamp(pageNumber, 1, totalPages);
            var loans = await query.OrderByDescending(b => b.BorrowDate)
                .ThenByDescending(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewModel.ActiveLoans = loans.Select(b => new CirculationActiveLoanItemViewModel
            {
                BorrowingId = b.Id,
                BookId = b.BookId,
                BookTitle = b.Book?.Title ?? string.Empty,
                BookAuthor = b.Book?.Author ?? string.Empty,
                BookISBN = b.Book?.ISBN ?? string.Empty,
                UserId = b.UserId,
                BorrowerName = b.User?.FullName ?? string.Empty,
                BorrowerEmail = b.User?.Email ?? string.Empty,
                MemberId = LibraryIdentifiers.Member(b.UserId),
                BorrowerInitials = GetInitials(b.User?.FullName),
                BorrowDate = b.BorrowDate
            }).ToList();

            ViewModel.ReturnsTodayCount = await _context.Borrowings.CountAsync(b => b.ReturnDate.HasValue && b.ReturnDate.Value.Date == DateTime.Today);
            ViewModel.OverdueItemsCount = await _context.Borrowings.CountAsync(b => b.ReturnDate == null && b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) < DateTime.Today);
            ViewModel.DueThisWeekCount = await _context.Borrowings.CountAsync(b => b.ReturnDate == null && b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) >= DateTime.Today && b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) <= DateTime.Today.AddDays(7));
            ViewModel.TotalActiveLoansCount = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);
            ViewModel.MatchingActiveLoansCount = matchingCount;
            ViewModel.PageNumber = pageNumber;
            ViewModel.TotalPages = totalPages;
        }

        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
