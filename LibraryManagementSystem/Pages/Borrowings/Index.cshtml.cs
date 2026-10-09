using System;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public BorrowingIndexViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync(string? status, string? searchMember, string? searchBook, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Borrowings
                .Include(b => b.Book)
                .ThenInclude(bk => bk!.Category)
                .Include(b => b.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Borrowed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(b => b.ReturnDate == null);
                else if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(b => b.ReturnDate != null);
            }

            if (!string.IsNullOrWhiteSpace(searchMember))
            {
                var mLower = searchMember.Trim().ToLower();
                query = query.Where(b => b.User != null &&
                    ((b.User.FullName != null && b.User.FullName.ToLower().Contains(mLower)) ||
                     (b.User.Email != null && b.User.Email.ToLower().Contains(mLower)) ||
                     b.User.Id.ToString().Contains(mLower)));
            }

            if (!string.IsNullOrWhiteSpace(searchBook))
            {
                var bLower = searchBook.Trim().ToLower();
                query = query.Where(b => b.Book != null &&
                    (b.Book.Title.ToLower().Contains(bLower) ||
                     b.Book.Author.ToLower().Contains(bLower) ||
                     b.Book.ISBN.ToLower().Contains(bLower)));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BorrowDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.BorrowDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            var list = await query.OrderByDescending(b => b.BorrowDate).ToListAsync();

            var totalCount = await _context.Borrowings.CountAsync();
            var currentlyBorrowed = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);

            ViewModel = new BorrowingIndexViewModel
            {
                StatusFilter = status,
                SearchMember = searchMember,
                SearchBook = searchBook,
                FromDate = fromDate,
                ToDate = toDate,
                TotalBorrowedCount = totalCount,
                CurrentlyBorrowedCount = currentlyBorrowed,
                Borrowings = list.Select(b => new BorrowingHistoryItemViewModel
                {
                    BorrowingId = b.Id,
                    BookId = b.BookId,
                    BookTitle = b.Book?.Title ?? "Unknown Title",
                    BookAuthor = b.Book?.Author ?? "Unknown Author",
                    BookISBN = b.Book?.ISBN ?? "N/A",
                    CategoryName = b.Book?.Category?.Name ?? "General Catalog",
                    BorrowerName = b.User?.FullName ?? "Member",
                    BorrowerEmail = b.User?.Email ?? "member@library.org",
                    MemberId = $"#MB-{b.UserId:D4}",
                    BorrowerInitials = GetInitials(b.User?.FullName),
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate
                }).ToList()
            };
        }

        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "MB";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
