using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class ConfirmModel : PageModel
    {
        public IActionResult OnGet(int? id) => RedirectToPage("./Borrow", new { bookId = id });

        public IActionResult OnPost(int bookId) => RedirectToPage("./Borrow", new { bookId });
    }
}
