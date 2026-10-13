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
        public IActionResult About()
        {
            return View();
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
            var totalMembers = await _userManager.Users.CountAsync();

            // Featured Spotlight Books
            var featuredBooks = new List<FeaturedBookItem>
            {
                new FeaturedBookItem
                {
                    Title = "Designing Data-Intensive Applications",
                    Author = "Martin Kleppmann",
                    CategoryName = "TEXTBOOK",
                    PublisherYear = "2017 • O'REILLY",
                    Rating = 4.9,
                    ReviewCount = 124,
                    IsAvailable = true,
                    BgGradient = "linear-gradient(135deg, #1e3a8a 0%, #172554 100%)",
                    AccentColor = "#3b82f6"
                },
                new FeaturedBookItem
                {
                    Title = "Klara and the Sun",
                    Author = "Kazuo Ishiguro",
                    CategoryName = "FICTION",
                    PublisherYear = "2021 • FABER",
                    Rating = 4.7,
                    ReviewCount = 94,
                    IsAvailable = true,
                    BgGradient = "linear-gradient(135deg, #b45309 0%, #78350f 100%)",
                    AccentColor = "#f59e0b"
                },
                new FeaturedBookItem
                {
                    Title = "A Brief History of Time",
                    Author = "Stephen Hawking",
                    CategoryName = "POPULAR SCIENCE",
                    PublisherYear = "1988 • BANTAM",
                    Rating = 4.8,
                    ReviewCount = 212,
                    IsAvailable = true,
                    BgGradient = "linear-gradient(135deg, #1e293b 0%, #0f172a 100%)",
                    AccentColor = "#64748b"
                },
                new FeaturedBookItem
                {
                    Title = "Thinking, Fast and Slow",
                    Author = "Daniel Kahneman",
                    CategoryName = "PSYCHOLOGY",
                    PublisherYear = "2011 • FSG & PRH",
                    Rating = 4.6,
                    ReviewCount = 188,
                    IsAvailable = true,
                    BgGradient = "linear-gradient(135deg, #065f46 0%, #064e3b 100%)",
                    AccentColor = "#10b981"
                }
            };

            // If we have books in DB with matching titles, link their real ID
            var dbBooks = await _context.Books.AsNoTracking().Take(10).ToListAsync();
            foreach (var fb in featuredBooks)
            {
                var match = dbBooks.FirstOrDefault(b => b.Title.Contains(fb.Title, StringComparison.OrdinalIgnoreCase)
                    || fb.Title.Contains(b.Title, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    fb.Id = match.Id;
                    fb.IsAvailable = match.AvailableCopies > 0;
                }
                else if (dbBooks.Any())
                {
                    fb.Id = dbBooks.First().Id;
                }
            }

            return new GuestHomeViewModel
            {
                TotalTitles = totalTitles > 0 ? totalTitles : 45000,
                TotalMembers = totalMembers > 0 ? totalMembers : 12000,
                TotalStudyRooms = 8,
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
            var availableCopies = await _context.Books.SumAsync(b => (int?)b.AvailableCopies) ?? 0;
            var zeroAvailable = await _context.Books.CountAsync(b => b.AvailableCopies == 0);
            var totalMembers = await _userManager.Users.CountAsync();
            var activeLoans = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);

            var overdueCount = await _context.Borrowings
                .CountAsync(b => b.ReturnDate == null && b.BorrowDate.AddDays(14) < DateTime.Now);

            var vm = new DashboardViewModel
            {
                FullName = user?.FullName ?? (User.Identity?.Name ?? "User"),
                Email = user?.Email ?? "",
                PrimaryRole = primaryRole,
                TotalCatalogTitles = totalTitles,
                TotalCopies = totalCopies,
                AvailableCopies = availableCopies,
                CopiesOnLoan = totalCopies > availableCopies ? totalCopies - availableCopies : activeLoans,
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
                .OrderByDescending(b => b.Id)
                .Take(6)
                .ToListAsync();

            return vm;
        }

        #endregion
    }
}
