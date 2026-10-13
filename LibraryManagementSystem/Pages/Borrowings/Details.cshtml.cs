using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Borrowing? Borrowing { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .ThenInclude(bk => bk!.Category)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Borrowing == null) return NotFound();

            return Page();
        }
    }
}
