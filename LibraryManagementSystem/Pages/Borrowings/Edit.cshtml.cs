using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Borrowing Borrowing { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (borrowing == null) return NotFound();

            Borrowing = borrowing;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var existing = await _context.Borrowings.Include(b => b.Book).FirstOrDefaultAsync(b => b.Id == Borrowing.Id);
            if (existing == null) return NotFound();

            var previousBook = existing.Book;
            existing.BorrowDate = Borrowing.BorrowDate;
            existing.ReturnDate = Borrowing.ReturnDate;

            await _context.SaveChangesAsync();
            if (previousBook != null)
            {
                var activeLoans = await _context.Borrowings.CountAsync(b => b.BookId == previousBook.Id && b.ReturnDate == null);
                previousBook.AvailableCopies = Math.Max(0, previousBook.TotalCopies - activeLoans);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}
