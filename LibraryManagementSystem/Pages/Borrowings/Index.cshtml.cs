using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public BorrowingIndexViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync(string? status, string? searchMember, string? searchBook, DateTime? fromDate, DateTime? toDate, int pageNumber = 1)
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
                var from = fromDate.Value.Date;
                query = query.Where(b => b.ReturnDate == null || b.BorrowDate >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(b => b.ReturnDate == null || b.BorrowDate < toExclusive);
            }

            const int pageSize = 10;
            var matchingCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(matchingCount / (double)pageSize));
            pageNumber = Math.Clamp(pageNumber, 1, totalPages);
            var list = await query.OrderBy(b => b.ReturnDate.HasValue)
                .ThenByDescending(b => b.BorrowDate)
                .ThenByDescending(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Borrowings.CountAsync();
            var currentlyBorrowed = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);
            var firstRecordDate = await _context.Borrowings.Select(b => (DateTime?)b.BorrowDate).MinAsync();
            var latestRecordDate = await _context.Borrowings.Select(b => (DateTime?)b.BorrowDate).MaxAsync();

            ViewModel = new BorrowingIndexViewModel
            {
                StatusFilter = status,
                SearchMember = searchMember,
                SearchBook = searchBook,
                FromDate = fromDate ?? firstRecordDate?.Date ?? DateTime.Today,
                ToDate = toDate ?? latestRecordDate?.Date ?? DateTime.Today,
                TotalBorrowedCount = totalCount,
                CurrentlyBorrowedCount = currentlyBorrowed,
                MatchingRecordsCount = matchingCount,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                Borrowings = list.Select(b => new BorrowingHistoryItemViewModel
                {
                    BorrowingId = b.Id,
                    BookId = b.BookId,
                    BookTitle = b.Book?.Title ?? string.Empty,
                    BookAuthor = b.Book?.Author ?? string.Empty,
                    BookISBN = b.Book?.ISBN ?? string.Empty,
                    CategoryName = b.Book?.Category?.Name ?? string.Empty,
                    BorrowerName = b.User?.FullName ?? string.Empty,
                    BorrowerEmail = b.User?.Email ?? string.Empty,
                    MemberId = LibraryIdentifiers.Member(b.UserId),
                    BorrowerInitials = GetInitials(b.User?.FullName),
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate
                }).ToList()
            };
        }

        public async Task<IActionResult> OnGetExportAsync(string? status, string? searchMember, string? searchBook, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Borrowings
                .AsNoTracking()
                .Include(b => b.Book)
                .Include(b => b.User)
                .AsQueryable();

            if (string.Equals(status, "Borrowed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.ReturnDate == null);
            else if (string.Equals(status, "Returned", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.ReturnDate != null);

            if (!string.IsNullOrWhiteSpace(searchMember))
            {
                var term = searchMember.Trim().ToLower();
                query = query.Where(b => b.User != null &&
                    ((b.User.FullName != null && b.User.FullName.ToLower().Contains(term)) ||
                     (b.User.Email != null && b.User.Email.ToLower().Contains(term)) ||
                     b.User.Id.ToString().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(searchBook))
            {
                var term = searchBook.Trim().ToLower();
                query = query.Where(b => b.Book != null &&
                    (b.Book.Title.ToLower().Contains(term) || b.Book.Author.ToLower().Contains(term) || b.Book.ISBN.ToLower().Contains(term)));
            }

            if (fromDate.HasValue)
                query = query.Where(b => b.BorrowDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(b => b.BorrowDate < toDate.Value.Date.AddDays(1));

            var rows = await query.OrderByDescending(b => b.BorrowDate).ThenByDescending(b => b.Id)
                .Select(b => new
                {
                    Member = b.User == null ? string.Empty : b.User.FullName,
                    MemberId = b.UserId,
                    Book = b.Book == null ? string.Empty : b.Book.Title,
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate,
                    Status = b.ReturnDate.HasValue ? "Returned" : "Borrowed"
                })
                .ToListAsync();

            var csv = new StringBuilder("Member,Member ID,Book,Borrow Date,Return Date,Status\r\n");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(row.Member), EscapeCsv(LibraryIdentifiers.Member(row.MemberId)), EscapeCsv(row.Book),
                    EscapeCsv(row.BorrowDate.ToString("yyyy-MM-dd")),
                    EscapeCsv(row.ReturnDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                    EscapeCsv(row.Status)
                }));
            }

            var fileName = $"circulation_history_{DateTime.Today:yyyy-MM-dd}.csv";
            return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
        }

        private static string EscapeCsv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
