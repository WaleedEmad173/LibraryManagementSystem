using System.Diagnostics;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: / (or /Home/Index)
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var dashboardVm = await BuildDashboardViewModelAsync();
                return View("Dashboard", dashboardVm);
            }

            var guestVm = await BuildGuestHomeViewModelAsync();
            return View("Index", guestVm);
        }

        [Authorize]
        [HttpGet("/Dashboard")]
        [HttpGet("/Home/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboardVm = await BuildDashboardViewModelAsync();
            return View("Dashboard", dashboardVm);
        }

        [HttpGet("/About")]
        [HttpGet("/Home/About")]
        public async Task<IActionResult> About()
        {
            var vm = await BuildGuestHomeViewModelAsync();
            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("/Home/Error/{statusCode:int?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var code = statusCode ?? Response.StatusCode;
            if (code == 0 || code == 200) code = 500;

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = code
            });
        }

        [Route("/404")]
        public IActionResult PageNotFound()
        {
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = 404
            });
        }

        #region Private Helpers

        private async Task<GuestHomeViewModel> BuildGuestHomeViewModelAsync()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            var totalTitles = await _context.Books.CountAsync();
            var totalCopies = await _context.Books.SumAsync(b => (int?)b.TotalCopies) ?? 0;
            var activeLoans = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);
            var availableCopies = Math.Max(0, totalCopies - activeLoans);
            var totalMembers = await _userManager.Users.CountAsync();
            var totalBorrowings = await _context.Borrowings.CountAsync();
            var totalCategories = categories.Count;

            // Fetch real featured books from database (ordered by popular loans, then by newest)
            var realBooks = await _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Borrowings)
                .OrderByDescending(b => b.Borrowings.Count)
                .ThenByDescending(b => b.Id)
                .Take(8)
                .ToListAsync();

            var gradients = new[]
            {
                "linear-gradient(135deg, #1e3a8a 0%, #172554 100%)",
                "linear-gradient(135deg, #b45309 0%, #78350f 100%)",
                "linear-gradient(135deg, #065f46 0%, #064e3b 100%)",
                "linear-gradient(135deg, #4c1d95 0%, #2e1065 100%)",
                "linear-gradient(135deg, #831843 0%, #500724 100%)",
                "linear-gradient(135deg, #1e293b 0%, #0f172a 100%)",
                "linear-gradient(135deg, #0369a1 0%, #0c4a6e 100%)",
                "linear-gradient(135deg, #c2410c 0%, #7c2d12 100%)"
            };

            var featuredBooks = realBooks.Select((b, index) =>
            {
                var gradient = gradients[index % gradients.Length];
                return new FeaturedBookItem
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    CategoryName = b.Category?.Name?.ToUpperInvariant() ?? "GENERAL",
                    PublisherYear = !string.IsNullOrWhiteSpace(b.ISBN) ? $"ISBN: {b.ISBN}" : "CATALOG",
                    LoanCount = b.Borrowings.Count,
                    IsAvailable = b.TotalCopies > b.Borrowings.Count(loan => loan.ReturnDate == null),
                    AvailableCopies = Math.Max(0, b.TotalCopies - b.Borrowings.Count(loan => loan.ReturnDate == null)),
                    TotalCopies = b.TotalCopies,
                    ImageUrl = b.ImageUrl,
                    BgGradient = gradient
                };
            }).ToList();

            return new GuestHomeViewModel
            {
                TotalTitles = totalTitles,
                TotalCopies = totalCopies,
                AvailableCopies = availableCopies,
                TotalMembers = totalMembers,
                TotalCategories = totalCategories,
                TotalBorrowings = totalBorrowings,
                Categories = categories,
                FeaturedBooks = featuredBooks
            };
        }

        private async Task<DashboardViewModel> BuildDashboardViewModelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var primaryRole = roles.FirstOrDefault() ?? "Member";

            var totalTitles = await _context.Books.CountAsync();
            var totalCopies = await _context.Books.SumAsync(b => (int?)b.TotalCopies) ?? 0;
            var activeLoans = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);
            var availableCopies = Math.Max(0, totalCopies - activeLoans);
            var zeroAvailable = await _context.Books.CountAsync(b => b.TotalCopies <= b.Borrowings.Count(loan => loan.ReturnDate == null));
            var totalMembers = await _userManager.Users.CountAsync();

            var overdueCount = await _context.Borrowings
                .CountAsync(b => b.ReturnDate == null && b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) < DateTime.Now);

            var vm = new DashboardViewModel
            {
                FullName = user?.FullName ?? (User.Identity?.Name ?? "User"),
                Email = user?.Email ?? "",
                PrimaryRole = primaryRole,
                TotalCatalogTitles = totalTitles,
                TotalCopies = totalCopies,
                AvailableCopies = availableCopies,
                CopiesOnLoan = activeLoans,
                ZeroAvailableTitles = zeroAvailable,
                TotalMembers = totalMembers,
                ActiveLoansCount = activeLoans,
                OverdueLoansCount = overdueCount
            };

            if (user != null)
            {
                vm.MyBorrowings = await _context.Borrowings
                    .AsNoTracking()
                    .Include(b => b.Book)
                    .Where(b => b.UserId == user.Id)
                    .OrderByDescending(b => b.BorrowDate)
                    .Take(5)
                    .Select(b => new BorrowingItemViewModel
                    {
                        Id = b.Id,
                        BookId = b.BookId,
                        BookTitle = b.Book != null ? b.Book.Title : "Unknown Title",
                        BookAuthor = b.Book != null ? b.Book.Author : "",
                        UserName = user.FullName,
                        BorrowDate = b.BorrowDate,
                        ReturnDate = b.ReturnDate
                    })
                    .ToListAsync();

                vm.MyActiveBorrowingsCount = await _context.Borrowings
                    .CountAsync(b => b.UserId == user.Id && b.ReturnDate == null);

                vm.MyTotalHistoryCount = await _context.Borrowings
                    .CountAsync(b => b.UserId == user.Id);
            }

            // For admin / librarian: recent borrowings
            if (vm.IsStaff)
            {
                vm.RecentBorrowings = await _context.Borrowings
                    .AsNoTracking()
                    .Include(b => b.Book)
                    .Include(b => b.User)
                    .OrderByDescending(b => b.BorrowDate)
                    .Take(7)
                    .Select(b => new BorrowingItemViewModel
                    {
                        Id = b.Id,
                        BookId = b.BookId,
                        BookTitle = b.Book != null ? b.Book.Title : "Unknown",
                        BookAuthor = b.Book != null ? b.Book.Author : "",
                        UserName = b.User != null ? b.User.FullName : "Member",
                        BorrowDate = b.BorrowDate,
                        ReturnDate = b.ReturnDate
                    })
                    .ToListAsync();
            }

            // Recent or popular books
            vm.RecentBooks = await _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Borrowings)
                .OrderByDescending(b => b.Id)
                .Take(6)
                .ToListAsync();
            foreach (var recentBook in vm.RecentBooks)
                recentBook.AvailableCopies = Math.Max(0, recentBook.TotalCopies - recentBook.Borrowings.Count(loan => loan.ReturnDate == null));

            return vm;
        }

        #endregion
    }
}
