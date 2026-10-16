using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Member,Admin,Librarian")]
    public class MemberController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Borrow(string? status, DateTime? fromDate, DateTime? toDate, int pageNumber = 1)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Librarian"))
            {
                return Redirect("/Borrow");
            }

            var member = await _userManager.GetUserAsync(User);
            if (member == null)
                return Challenge();

            var memberLoans = _context.Borrowings
                .AsNoTracking()
                .Where(b => b.UserId == member.Id);

            var totalBorrowedCount = await memberLoans.CountAsync();
            var activeLoans = memberLoans.Where(b => b.ReturnDate == null);
            var activeCount = await activeLoans.CountAsync();
            var overdueCount = await activeLoans.CountAsync(b => b.BorrowDate.AddDays(14) < DateTime.Today);
            var nextDueDate = activeCount == 0
                ? null
                : await activeLoans.Select(b => (DateTime?)b.BorrowDate.AddDays(14)).MinAsync();
            var firstBorrowDate = await memberLoans.Select(b => (DateTime?)b.BorrowDate).MinAsync();
            var lastBorrowDate = await memberLoans.Select(b => (DateTime?)b.BorrowDate).MaxAsync();

            var resolvedFromDate = fromDate ?? firstBorrowDate?.Date;
            var resolvedToDate = toDate ?? lastBorrowDate?.Date;
            var query = memberLoans.AsQueryable();

            if (string.Equals(status, "Borrowed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.ReturnDate == null);
            else if (string.Equals(status, "Returned", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.ReturnDate != null);

            if (resolvedFromDate.HasValue)
            {
                var from = resolvedFromDate.Value.Date;
                query = query.Where(b => b.ReturnDate == null || b.BorrowDate >= from);
            }
            if (resolvedToDate.HasValue)
            {
                var toExclusive = resolvedToDate.Value.Date.AddDays(1);
                query = query.Where(b => b.ReturnDate == null || b.BorrowDate < toExclusive);
            }

            const int pageSize = 8;
            var matchingCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(matchingCount / (double)pageSize));
            pageNumber = Math.Clamp(pageNumber, 1, totalPages);

            var borrowings = await query
                .OrderBy(b => b.ReturnDate.HasValue)
                .ThenByDescending(b => b.BorrowDate)
                .ThenByDescending(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BorrowingHistoryItemViewModel
                {
                    BorrowingId = b.Id,
                    BookId = b.BookId,
                    BookTitle = b.Book != null ? b.Book.Title : string.Empty,
                    BookAuthor = b.Book != null ? b.Book.Author : string.Empty,
                    BookISBN = b.Book != null ? b.Book.ISBN : string.Empty,
                    CategoryName = b.Book != null && b.Book.Category != null ? b.Book.Category.Name : string.Empty,
                    BorrowerName = member.FullName,
                    MemberId = $"#MB-{member.Id:D4}",
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate
                })
                .ToListAsync();

            var model = new MemberBorrowingsViewModel
            {
                Borrowings = borrowings,
                MemberId = member.Id,
                TotalBorrowedCount = totalBorrowedCount,
                ActiveBorrowingsCount = activeCount,
                OverdueCount = overdueCount,
                NextDueDate = nextDueDate,
                StatusFilter = status,
                FromDate = resolvedFromDate,
                ToDate = resolvedToDate,
                MatchingRecordCount = matchingCount,
                PageNumber = pageNumber,
                TotalPages = totalPages
            };

            ViewData["Title"] = "My Borrowings";
            ViewData["Section"] = "Confirm Borrowing";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var member = await _userManager.GetUserAsync(User);
            if (member == null)
                return Challenge();

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == member.Id);

            if (borrowing == null)
            {
                TempData["Error"] = "Borrowing record not found.";
                return RedirectToAction(nameof(Borrow));
            }

            if (borrowing.ReturnDate != null)
            {
                TempData["Warning"] = "This book has already been returned.";
                return RedirectToAction(nameof(Borrow));
            }

            borrowing.ReturnDate = DateTime.Now;
            if (borrowing.Book != null)
            {
                var remainingActiveLoans = await _context.Borrowings.CountAsync(b => b.BookId == borrowing.BookId && b.Id != borrowing.Id && b.ReturnDate == null);
                borrowing.Book.AvailableCopies = Math.Max(0, borrowing.Book.TotalCopies - remainingActiveLoans);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Successfully returned '{borrowing.Book?.Title ?? "book"}'.";

            return RedirectToAction(nameof(Borrow));
        }
    }
}
