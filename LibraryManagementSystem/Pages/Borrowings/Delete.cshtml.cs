using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Borrowing? Borrowing { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Borrowing == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _context.Borrowings.Include(b => b.Book).FirstOrDefaultAsync(b => b.Id == id);
            if (borrowing != null)
            {
                var book = borrowing.Book;
                _context.Borrowings.Remove(borrowing);
                await _context.SaveChangesAsync();

                if (book != null)
                {
                    var activeLoans = await _context.Borrowings.CountAsync(b => b.BookId == book.Id && b.ReturnDate == null);
                    book.AvailableCopies = Math.Max(0, book.TotalCopies - activeLoans);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
