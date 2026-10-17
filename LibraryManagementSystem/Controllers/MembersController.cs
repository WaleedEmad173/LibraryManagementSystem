using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Librarian,Admin")]
    public class MembersController : Controller
    {
        private const int PageSize = 7;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Members?search=ali&page=2
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            ViewData["Title"] = "Members Directory";
            ViewData["Section"] = "Members";

            // Query ALL users/accounts in the database regardless of role
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var pageIds = users.Select(u => u.Id).ToList();
            var activeCounts = await _context.Borrowings
                .Where(b => pageIds.Contains(b.UserId) && b.ReturnDate == null)
                .GroupBy(b => b.UserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.Name);
            var userRoles = await _context.UserRoles.Where(ur => pageIds.Contains(ur.UserId)).ToListAsync();
            var userRoleMap = userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key, 
                    g => g.Select(ur => roles.TryGetValue(ur.RoleId, out var rName) ? rName : "Member").FirstOrDefault() ?? "Member"
                );

            int realActiveLoans = await _context.Borrowings.CountAsync(b => b.ReturnDate == null);
            int membersWithOverdueLoans = await _context.Borrowings
                .Where(b => b.ReturnDate == null && b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) < DateTime.Today)
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();
            double goodStandingPercentage = totalItems == 0
                ? 0
                : Math.Round(Math.Max(0, totalItems - membersWithOverdueLoans) * 100.0 / totalItems, 1);

            var memberList = users.Select(u =>
            {
                var names = u.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string initials = names.Length >= 2
                    ? $"{names[0][0]}{names[1][0]}".ToUpper()
                    : (u.FullName.Length >= 2 ? u.FullName[..2].ToUpper() : "ME");

                string roleName = userRoleMap.TryGetValue(u.Id, out var r) ? r : "Member";

                return new MemberRowViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Phone = u.PhoneNumber,
                    JoinDate = u.JoinDate,
                    ActiveBorrowings = activeCounts.TryGetValue(u.Id, out var c) ? c : 0,
                    MemberCode = LibraryIdentifiers.Member(u.Id),
                    Initials = initials,
                    Role = roleName
                };
            }).ToList();

            var model = new MemberListViewModel
            {
                Search = search,
                Page = page,
                TotalPages = Math.Max(totalPages, 1),
                TotalMembers = totalItems,
                ActiveLoansTotal = realActiveLoans,
                GoodStandingPercentage = goodStandingPercentage,
                Members = memberList
            };

            return View(model);
        }

        // GET: /Members/Details/{id}
        public async Task<IActionResult> Details(int id, string? filter = null)
        {
            ViewData["Title"] = "Patron Profile";
            ViewData["Section"] = "Members";

            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return NotFound();

            var query = _context.Borrowings
                .Include(b => b.Book)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.BorrowDate)
                .Select(b => new BorrowingRowViewModel
                {
                    Id = b.Id,
                    BookTitle = b.Book != null ? b.Book.Title : string.Empty,
                    Author = b.Book != null ? b.Book.Author : string.Empty,
                    ISBN = b.Book != null ? b.Book.ISBN : string.Empty,
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate,
                    DueDate = b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays)
                });

            var borrowings = await query.ToListAsync();

            int activeCount = borrowings.Count(b => b.ReturnDate == null);
            int overdueCount = borrowings.Count(b => b.ReturnDate == null && b.DueDate < DateTime.Today);
            int returnedCount = borrowings.Count(b => b.ReturnDate != null);
            int totalLoans = borrowings.Count;
            double returnRate = totalLoans == 0
                ? 0
                : Math.Round((totalLoans - overdueCount) * 100.0 / totalLoans, 1);

            var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = names.Length >= 2
                ? $"{names[0][0]}{names[1][0]}".ToUpper()
                : (user.FullName.Length >= 2 ? user.FullName[..2].ToUpper() : string.Empty);

            string memberCode = LibraryIdentifiers.Member(user.Id);

            ViewData["CurrentFilter"] = filter ?? "all";
            var filteredBorrowings = filter switch
            {
                "active" => borrowings.Where(b => b.ReturnDate == null).ToList(),
                "returned" => borrowings.Where(b => b.ReturnDate != null).ToList(),
                _ => borrowings
            };

            var model = new MemberDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                JoinDate = user.JoinDate,
                MemberCode = memberCode,
                Initials = initials,
                ActiveLoansCount = activeCount,
                MaxLoansQuota = CirculationPolicy.MaxActiveLoans,
                TotalLoansCount = totalLoans,
                ReturnedLoansCount = returnedCount,
                ReturnRate = returnRate,
                StatusStanding = overdueCount == 0 ? "No Overdue Loans" : $"{overdueCount} Overdue Loan(s)",
                Borrowings = filteredBorrowings
            };

            return View(model);
        }

        // GET: /Members/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add New Member";
            ViewData["Section"] = "Members";

            return View(new MemberFormViewModel());
        }

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberFormViewModel model)
        {
            ViewData["Title"] = "Add New Member";
            ViewData["Section"] = "Members";

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", $"Email already registered to member {LibraryIdentifiers.Member(existing.Id)}");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.Phone,
                JoinDate = DateTime.Now,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                TempData["Success"] = "Member created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // GET: /Members/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Member";
            ViewData["Section"] = "Members";

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = names.Length >= 2
                ? $"{names[0][0]}{names[1][0]}".ToUpper()
                : (user.FullName.Length >= 2 ? user.FullName[..2].ToUpper() : string.Empty);

            string memberCode = LibraryIdentifiers.Member(user.Id);

            // Fetch active loans for user
            var realLoans = await _context.Borrowings
                .Include(b => b.Book)
                .Where(b => b.UserId == id && b.ReturnDate == null)
                .ToListAsync();

            var activeLoanItems = new List<EditMemberLoanItem>();
            foreach (var loan in realLoans)
            {
                var dueDate = loan.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays);
                int daysLeft = (dueDate.Date - DateTime.Today).Days;
                activeLoanItems.Add(new EditMemberLoanItem
                {
                    Title = loan.Book?.Title ?? string.Empty,
                    Author = loan.Book?.Author ?? string.Empty,
                    ISBN = loan.Book?.ISBN ?? string.Empty,
                    DueText = daysLeft < 0
                        ? $"Overdue by {-daysLeft} days ({dueDate:MMM dd, yyyy})"
                        : $"Due in {daysLeft} days ({dueDate:MMM dd, yyyy})"
                });
            }

            int overdueItems = realLoans.Count(loan => loan.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) < DateTime.Today);
            int lifetimeLoans = await _context.Borrowings.CountAsync(loan => loan.UserId == id);

            var model = new MemberFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                MemberCode = memberCode,
                Initials = initials,
                CardStatus = user.EmailConfirmed ? "Verified" : "Unverified",
                JoinDate = user.JoinDate,
                CurrentlyBorrowed = realLoans.Count,
                BorrowAllowance = CirculationPolicy.MaxActiveLoans,
                OverdueItems = overdueItems,
                StandingStatus = overdueItems == 0 ? "Good" : "Overdue",
                StandingDesc = overdueItems == 0 ? "No overdue loans" : "Overdue loans require attention",
                LifetimeCheckouts = $"{lifetimeLoans} Volumes",
                ActiveLoans = activeLoanItems
            };

            return View(model);
        }

        // POST: /Members/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MemberFormViewModel model)
        {
            ViewData["Title"] = "Edit Member";
            ViewData["Section"] = "Members";

            if (model.Id != id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEditContextAsync(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != id)
            {
                ModelState.AddModelError("Email", $"Email already registered to member {LibraryIdentifiers.Member(emailOwner.Id)}");
                await PopulateEditContextAsync(model);
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                await PopulateEditContextAsync(model);
                return View(model);
            }

            // Password is optional: only reset it if a new one was entered
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    await PopulateEditContextAsync(model);
                    return View(model);
                }
            }

            TempData["Success"] = "Member updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateEditContextAsync(MemberFormViewModel model)
        {
            if (!model.Id.HasValue)
                return;

            var user = await _userManager.FindByIdAsync(model.Id.Value.ToString());
            if (user == null)
                return;

            var names = model.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            model.MemberCode = LibraryIdentifiers.Member(user.Id);
            model.Initials = names.Length >= 2
                ? $"{names[0][0]}{names[1][0]}".ToUpperInvariant()
                : (model.FullName.Length >= 2 ? model.FullName[..2].ToUpperInvariant() : string.Empty);
            model.CardStatus = user.EmailConfirmed ? "Verified" : "Unverified";
            model.JoinDate = user.JoinDate;

            var loans = await _context.Borrowings
                .Include(b => b.Book)
                .Where(b => b.UserId == user.Id && b.ReturnDate == null)
                .ToListAsync();
            model.CurrentlyBorrowed = loans.Count;
            model.OverdueItems = loans.Count(b => b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays) < DateTime.Today);
            model.StandingStatus = model.OverdueItems == 0 ? "Good" : "Overdue";
            model.StandingDesc = model.OverdueItems == 0 ? "No overdue loans" : "Overdue loans require attention";
            model.LifetimeCheckouts = $"{await _context.Borrowings.CountAsync(b => b.UserId == user.Id)} Volumes";
            model.BorrowAllowance = CirculationPolicy.MaxActiveLoans;
            model.ActiveLoans = loans.Select(loan =>
            {
                var dueDate = loan.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays);
                int daysLeft = (dueDate.Date - DateTime.Today).Days;
                return new EditMemberLoanItem
                {
                    Title = loan.Book?.Title ?? string.Empty,
                    Author = loan.Book?.Author ?? string.Empty,
                    ISBN = loan.Book?.ISBN ?? string.Empty,
                    DueText = daysLeft < 0
                        ? $"Overdue by {-daysLeft} days ({dueDate:MMM dd, yyyy})"
                        : $"Due in {daysLeft} days ({dueDate:MMM dd, yyyy})"
                };
            }).ToList();
        }

        // GET: /Members/Delete/{id}  -- Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : (parts.Length == 1 && parts[0].Length >= 2 ? parts[0][..2].ToUpper() : string.Empty);

            var completedLoans = await _context.Borrowings.CountAsync(b => b.UserId == id && b.ReturnDate != null);
            var activeBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Where(b => b.UserId == id && b.ReturnDate == null)
                .ToListAsync();

            var model = new MemberDeleteViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                MemberCode = LibraryIdentifiers.Member(user.Id),
                Initials = initials,
                JoinDate = user.JoinDate,
                CompletedLoansCount = completedLoans
            };

            if (activeBorrowings.Any())
            {
                model.CanDelete = false;
                model.BlockMessage = $"This member cannot be deleted because they have {activeBorrowings.Count} active borrowing(s).";
                model.ActiveLoans = activeBorrowings.Select(b => new PatronActiveLoanViewModel
                {
                    Id = b.Id,
                    BookTitle = b.Book?.Title ?? "Book",
                    DueDate = b.BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays)
                }).ToList();
            }

            return View(model);
        }

        // POST: /Members/Delete/{id}  -- Admin only
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            if (await HasActiveBorrowings(id))
            {
                TempData["Error"] = "This member cannot be deleted because they have active borrowings.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    TempData["Error"] = "The member could not be deleted.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            catch (DbUpdateException)
            {
                // FK constraint: member still has past borrowing history
                TempData["Error"] = "The member could not be deleted because they have borrowing history.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["Success"] = "Member deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> HasActiveBorrowings(int userId) =>
            _context.Borrowings.AnyAsync(b => b.UserId == userId && b.ReturnDate == null);
    }
}
