using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class BorrowModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BorrowModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int SelectedUserId { get; set; }

        [BindProperty]
        public int SelectedBookId { get; set; }

        public CirculationBorrowPageViewModel ViewModel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? bookId, int? userId, string? simulate)
        {
            await LoadCirculationDataAsync(bookId, userId);

            if (!string.IsNullOrEmpty(simulate))
            {
                ViewModel.AlertState = simulate.ToLowerInvariant();
                if (ViewModel.AlertState == "success")
                {
                    ViewModel.AlertTitle = "Book borrowed successfully";
                    ViewModel.AlertReference = "#LN-3827";
                    ViewModel.AlertDueDate = DateTime.Today.AddDays(14);
                    ViewModel.AlertMessage = $"Inventory status updated instantly. Transaction permitted with reference {ViewModel.AlertReference}. Assigned to member {ViewModel.SelectedMember?.FullName ?? "Sara Ahmed"}.";
                }
                else if (ViewModel.AlertState == "nostock")
                {
                    ViewModel.AlertTitle = "No available copies for this book";
                    ViewModel.AlertMessage = $"All {ViewModel.SelectedBook?.TotalCopies ?? 5} registered copies of this title are currently loaned out or placed on hold. Checkout cannot proceed until an active loan is cleared.";
                }
                else if (ViewModel.AlertState == "duplicate")
                {
                    ViewModel.AlertTitle = "Member already has an active loan for this book";
                    ViewModel.AlertMessage = $"The selected patron already holds an active checkout of '{ViewModel.SelectedBook?.Title ?? "Clean Code"}'. Duplicate loans of identical titles are restricted.";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostBorrowAsync()
        {
            await LoadCirculationDataAsync(SelectedBookId, SelectedUserId);

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == SelectedBookId);
            var member = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == SelectedUserId);

            if (book == null)
            {
                ViewModel.AlertState = "nostock";
                ViewModel.AlertTitle = "Book Not Found";
                ViewModel.AlertMessage = "The requested book was not found in the catalog.";
                return Page();
            }

            if (member == null)
            {
                ViewModel.AlertState = "duplicate";
                ViewModel.AlertTitle = "Patron Not Found";
                ViewModel.AlertMessage = "The selected patron could not be verified in the user registry.";
                return Page();
            }

            // Check availability
            if (book.AvailableCopies <= 0)
            {
                ViewModel.AlertState = "nostock";
                ViewModel.AlertTitle = "No available copies for this book";
                ViewModel.AlertMessage = $"All {book.TotalCopies} registered copies of '{book.Title}' are currently loaned out. Checkout cannot proceed until an active copy is returned.";
                return Page();
            }

            // Check duplicate active borrowing
            var hasActiveCopy = await _context.Borrowings.AnyAsync(b => b.BookId == book.Id && b.UserId == member.Id && b.ReturnDate == null);
            if (hasActiveCopy)
            {
                ViewModel.AlertState = "duplicate";
                ViewModel.AlertTitle = "Member already has this book checked out";
                ViewModel.AlertMessage = $"Member {member.FullName} already has an active loan for '{book.Title}'. Circulation policies restrict borrowing multiple copies of the same book.";
                return Page();
            }

            // Check member quota
            var activeLoans = await _context.Borrowings.CountAsync(b => b.UserId == member.Id && b.ReturnDate == null);
            if (activeLoans >= 3)
            {
                ViewModel.AlertState = "duplicate";
                ViewModel.AlertTitle = "Borrowing Quota Limit Reached";
                ViewModel.AlertMessage = $"Patron {member.FullName} currently has {activeLoans} active books checked out. Maximum quota is 3 books.";
                return Page();
            }

            // Execute loan
            var borrowing = new Borrowing
            {
                BookId = book.Id,
                UserId = member.Id,
                BorrowDate = DateTime.Today,
                ReturnDate = null
            };

            book.AvailableCopies = Math.Max(0, book.AvailableCopies - 1);
            _context.Borrowings.Add(borrowing);
            await _context.SaveChangesAsync();

            // Refresh data after loan
            await LoadCirculationDataAsync(book.Id, member.Id);

            ViewModel.AlertState = "success";
            ViewModel.AlertTitle = "Book borrowed successfully";
            ViewModel.AlertReference = $"#LN-{borrowing.Id:D4}";
            ViewModel.AlertDueDate = DateTime.Today.AddDays(14);
            ViewModel.AlertMessage = $"Inventory status updated instantly. Transaction permitted with reference {ViewModel.AlertReference}. Assigned to member {member.FullName}.";

            return Page();
        }

        private async Task LoadCirculationDataAsync(int? preferredBookId, int? preferredUserId)
        {
            // Load Books
            var dbBooks = await _context.Books.Include(b => b.Category).ToListAsync();
            if (dbBooks.Any())
            {
                ViewModel.Books = dbBooks.Select(b => new BookOptionViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    CategoryName = b.Category?.Name ?? "General Catalog",
                    TotalCopies = b.TotalCopies > 0 ? b.TotalCopies : 5,
                    AvailableCopies = b.AvailableCopies,
                    ShelfLocation = "2nd Floor • Section T4, Shelf 12"
                }).ToList();
            }
            else
            {
                // Fallback catalog if DB not populated yet
                ViewModel.Books = new List<BookOptionViewModel>
                {
                    new() { Id = 1, Title = "Clean Code: A Handbook of Agile Software Craftsmanship", Author = "Robert C. Martin (Uncle Bob)", ISBN = "978-0132350884", CategoryName = "Software Dev", TotalCopies = 5, AvailableCopies = 3 },
                    new() { Id = 2, Title = "System Design Interview – An Insider's Guide", Author = "Alex Xu", ISBN = "979-8664653403", CategoryName = "Architecture", TotalCopies = 4, AvailableCopies = 2 },
                    new() { Id = 3, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", ISBN = "978-1449373320", CategoryName = "Engineering", TotalCopies = 6, AvailableCopies = 0 }
                };
            }

            // Load Members
            var dbUsers = await _userManager.Users.Include(u => u.Borrowings).ToListAsync();
            if (dbUsers.Any())
            {
                ViewModel.Members = dbUsers.Select(u =>
                {
                    var activeLoans = u.Borrowings.Count(b => b.ReturnDate == null);
                    var overdue = u.Borrowings.Count(b => b.ReturnDate == null && DateTime.Today > b.BorrowDate.AddDays(14));
                    return new PatronOptionViewModel
                    {
                        Id = u.Id,
                        FullName = string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? $"Member {u.Id}") : u.FullName,
                        Email = u.Email ?? $"user{u.Id}@library.org",
                        MemberId = $"#MB-{u.Id:D4}",
                        Initials = GetInitials(u.FullName),
                        ActiveLoansCount = activeLoans,
                        MaxQuota = 3,
                        OverdueCount = overdue,
                        Standing = overdue > 0 ? "Under Review" : "Good Standing"
                    };
                }).ToList();
            }
            else
            {
                // Fallback members if DB not populated yet
                ViewModel.Members = new List<PatronOptionViewModel>
                {
                    new() { Id = 1, FullName = "Sara Ahmed", Email = "sara.ahmed@example.com", MemberId = "#MB-4820", Initials = "SA", ActiveLoansCount = 2, MaxQuota = 3, OverdueCount = 0, Standing = "Good Standing" },
                    new() { Id = 2, FullName = "David Kahanan", Email = "david.k@example.com", MemberId = "#MB-1904", Initials = "DK", ActiveLoansCount = 1, MaxQuota = 3, OverdueCount = 0, Standing = "Good Standing" },
                    new() { Id = 3, FullName = "Maya Lin", Email = "maya.lin@example.com", MemberId = "#MB-3211", Initials = "ML", ActiveLoansCount = 3, MaxQuota = 3, OverdueCount = 1, Standing = "Action Required" }
                };
            }

            // Counts
            var count = await _context.Borrowings.CountAsync();
            ViewModel.TransactedCount = count > 0 ? count : 42;

            // Resolve selection
            if (preferredBookId.HasValue && ViewModel.Books.Any(b => b.Id == preferredBookId.Value))
            {
                SelectedBookId = preferredBookId.Value;
                ViewModel.SelectedBook = ViewModel.Books.First(b => b.Id == preferredBookId.Value);
            }
            else
            {
                ViewModel.SelectedBook = ViewModel.Books.FirstOrDefault();
                SelectedBookId = ViewModel.SelectedBook?.Id ?? 0;
            }

            if (preferredUserId.HasValue && ViewModel.Members.Any(m => m.Id == preferredUserId.Value))
            {
                SelectedUserId = preferredUserId.Value;
                ViewModel.SelectedMember = ViewModel.Members.First(m => m.Id == preferredUserId.Value);
            }
            else
            {
                ViewModel.SelectedMember = ViewModel.Members.FirstOrDefault();
                SelectedUserId = ViewModel.SelectedMember?.Id ?? 0;
            }
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
