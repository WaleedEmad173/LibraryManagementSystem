using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public AdminController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Staff));
        }

        [HttpGet("/Admin/Staff")]
        public async Task<IActionResult> Staff(string? search, string? role)
        {
            // Find all users who are Admin or Librarian
            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            var librarianRoleId = await _context.Roles.Where(r => r.Name == "Librarian").Select(r => r.Id).FirstOrDefaultAsync();

            var staffUserRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRoleId || ur.RoleId == librarianRoleId)
                .ToListAsync();

            var staffIds = staffUserRoles.Select(ur => ur.UserId).Distinct().ToList();

            var allStaffUsers = await _context.Users
                .Where(u => staffIds.Contains(u.Id))
                .ToListAsync();

            var totalAdmins = staffUserRoles.Count(ur => ur.RoleId == adminRoleId);
            var totalLibrarians = staffUserRoles.Count(ur => ur.RoleId == librarianRoleId);
            var totalPersonnel = allStaffUsers.Count;

            var currentUserName = User.Identity?.Name ?? "";
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? 0;

            var colors = new[]
            {
                (bg: "#fee2e2", color: "#dc2626"), // Coral/Red
                (bg: "#eff6ff", color: "#2563eb"), // Soft Blue
                (bg: "#f1f5f9", color: "#475569"), // Slate
                (bg: "#f3e8ff", color: "#9333ea"), // Lavender
                (bg: "#e0e7ff", color: "#4f46e5")  // Indigo
            };

            var staffItems = new List<StaffItemViewModel>();
            int idx = 0;

            foreach (var u in allStaffUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                var primaryRole = userRoles.Contains("Admin") ? "Admin" : (userRoles.Contains("Librarian") ? "Librarian" : "Staff");

                // Filter by role if specified
                if (!string.IsNullOrWhiteSpace(role) && role != "All Roles" && !string.Equals(primaryRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Filter by search term
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLowerInvariant();
                    if (!u.FullName.ToLowerInvariant().Contains(s) && !u.Email?.ToLowerInvariant().Contains(s) == true)
                    {
                        continue;
                    }
                }

                var isCurrent = u.Id == currentUserId || string.Equals(u.Email, currentUserName, StringComparison.OrdinalIgnoreCase);

                var parts = u.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : (parts.Length == 1 && parts[0].Length >= 2 ? parts[0][..2].ToUpper() : "ST");

                var prefix = primaryRole == "Admin" ? "ADM" : "LIB";
                var staffCode = $"#{prefix}-{u.Id:D3}";

                var colorPair = colors[idx % colors.Length];
                idx++;

                staffItems.Add(new StaffItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = primaryRole,
                    StaffCode = staffCode,
                    Initials = initials,
                    IsCurrentSession = isCurrent,
                    AvatarBg = colorPair.bg,
                    AvatarColor = colorPair.color,
                    JoinDate = u.JoinDate
                });
            }

            // Put current session first if present, then Admin, then Librarian
            staffItems = staffItems
                .OrderByDescending(s => s.IsCurrentSession)
                .ThenBy(s => s.Role == "Admin" ? 0 : 1)
                .ThenBy(s => s.Id)
                .ToList();

            var vm = new StaffListViewModel
            {
                StaffMembers = staffItems,
                TotalPersonnel = totalPersonnel,
                TotalAdmins = totalAdmins,
                TotalLibrarians = totalLibrarians,
                Search = search,
                SelectedRole = role
            };

            ViewData["Title"] = "Staff Accounts";
            ViewData["Section"] = "Overview";
            return View(vm);
        }

        private async Task PopulateAddStaffViewBagAsync()
        {
            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            var librarianRoleId = await _context.Roles.Where(r => r.Name == "Librarian").Select(r => r.Id).FirstOrDefaultAsync();
            var staffUserRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRoleId || ur.RoleId == librarianRoleId)
                .ToListAsync();

            ViewBag.TotalPersonnel = staffUserRoles.Select(ur => ur.UserId).Distinct().Count();
            ViewBag.TotalAdmins = staffUserRoles.Count(ur => ur.RoleId == adminRoleId);
            ViewBag.TotalLibrarians = staffUserRoles.Count(ur => ur.RoleId == librarianRoleId);

            ViewData["Title"] = "Add New Staff Member";
            ViewData["Section"] = "Add New Staff";
        }

        [HttpPost("/Admin/CreateStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAddStaffViewBagAsync();
                return View("AddStaff", model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), $"A user with email '{model.Email}' already exists.");
                await PopulateAddStaffViewBagAsync();
                return View("AddStaff", model);
            }

            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                JoinDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    if (err.Code.Contains("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.Password), err.Description);
                    }
                    else if (err.Code.Contains("Email", StringComparison.OrdinalIgnoreCase) || err.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.Email), err.Description);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                }
                await PopulateAddStaffViewBagAsync();
                return View("AddStaff", model);
            }

            var targetRole = model.Role == "Admin" ? "Admin" : "Librarian";
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(targetRole));
            }

            await _userManager.AddToRoleAsync(newUser, targetRole);
            TempData["Success"] = $"Staff member '{model.FullName}' ({targetRole}) created successfully.";
            return RedirectToAction(nameof(Staff));
        }

        [HttpGet("/Admin/AddStaff")]
        public async Task<IActionResult> AddStaff()
        {
            await PopulateAddStaffViewBagAsync();
            return View(new CreateStaffViewModel());
        }

        [HttpGet("/Admin/EditRole/{id}")]
        public async Task<IActionResult> EditRole(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "Librarian";

            var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : (parts.Length == 1 && parts[0].Length >= 2 ? parts[0][..2].ToUpper() : "ST");

            var model = new EditStaffRoleViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                CurrentRole = currentRole,
                SelectedRole = currentRole,
                StaffCode = $"#{(currentRole == "Admin" ? "ADM" : "LIB")}-{user.Id:D3}",
                Initials = initials,
                JoinDate = user.JoinDate
            };

            ViewData["Title"] = "Edit Staff Role";
            ViewData["Section"] = "Edit Role";
            return View(model);
        }

        [HttpPost("/Admin/UpdateRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(EditStaffRoleViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var targetRole = model.SelectedRole == "Admin" ? "Admin" : (model.SelectedRole == "Member" ? "Member" : "Librarian");

            if (!currentRoles.Contains(targetRole))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!await _roleManager.RoleExistsAsync(targetRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(targetRole));
                }
                await _userManager.AddToRoleAsync(user, targetRole);
            }

            TempData["Success"] = $"Role for '{user.FullName}' updated to '{targetRole}' successfully.";
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost("/Admin/EditStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(EditStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields properly.";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {
                TempData["Error"] = "Staff member not found.";
                return RedirectToAction(nameof(Staff));
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Staff));
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            var targetRole = model.Role == "Admin" ? "Admin" : "Librarian";
            if (!currentRoles.Contains(targetRole))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, targetRole);
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            }

            TempData["Success"] = $"Staff member '{model.FullName}' updated successfully.";
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost("/Admin/DeleteStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                TempData["Error"] = "Staff account not found.";
                return RedirectToAction(nameof(Staff));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Staff account for '{user.FullName}' has been removed.";
            }
            else
            {
                TempData["Error"] = "Failed to remove staff account.";
            }

            return RedirectToAction(nameof(Staff));
        }

    }
}
