using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class CreateModel : PageModel
    {
        public IActionResult OnGet(int? id) => RedirectToPage("./Borrow", new { bookId = id });

        public IActionResult OnPost() => RedirectToPage("./Borrow");
    }
}
