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
        private const int PageSize = 10;
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
            // Only users who have the Member role
            var memberRoleId = await _context.Roles
                .Where(r => r.Name == "Member")
                .Select(r => r.Id)
                .FirstAsync();

            var memberIds = _context.UserRoles
                .Where(ur => ur.RoleId == memberRoleId)
                .Select(ur => ur.UserId);

            var query = _context.Users.Where(u => memberIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) || u.Email.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Count active borrowings only for the users on this page
            var pageIds = users.Select(u => u.Id).ToList();
            var activeCounts = await _context.Borrowings
                .Where(b => pageIds.Contains(b.UserId) && b.ReturnDate == null)
                .GroupBy(b => b.UserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var model = new MemberListViewModel
            {
                Search = search,
                Page = page,
                TotalPages = totalPages,
                Members = users.Select(u => new MemberRowViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Phone = u.PhoneNumber,
                    JoinDate = u.JoinDate,
                    ActiveBorrowings = activeCounts.TryGetValue(u.Id, out var c) ? c : 0
                }).ToList()
            };

            return View(model);
        }

        // GET: /Members/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.BorrowDate)
                .Select(b => new BorrowingRowViewModel
                {
                    BookTitle = b.Book.Title,
                    BorrowDate = b.BorrowDate,
                    ReturnDate = b.ReturnDate
                })
                .ToListAsync();

            var model = new MemberDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                JoinDate = user.JoinDate,
                Borrowings = borrowings
            };

            return View(model);
        }

        // GET: /Members/Create
        public IActionResult Create()
        {
            return View(new MemberFormViewModel());
        }

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email already registered");
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
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var model = new MemberFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber
            };

            return View(model);
        }

        // POST: /Members/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MemberFormViewModel model)
        {
            if (model.Id != id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != id)
            {
                ModelState.AddModelError("Email", "Email already registered");
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
                    return View(model);
                }
            }

            TempData["Success"] = "Member updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Delete/{id}  -- Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var model = new MemberDeleteViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                JoinDate = user.JoinDate
            };

            if (await HasActiveBorrowings(id))
            {
                model.CanDelete = false;
                model.BlockMessage = "This member cannot be deleted because they have active borrowings.";
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
