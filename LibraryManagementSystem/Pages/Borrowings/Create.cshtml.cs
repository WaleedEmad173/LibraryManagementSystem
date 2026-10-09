using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Pages.Borrowings
{
    public class CreateModel : PageModel
    {
        public IActionResult OnGet(int? id, string? simulate)
        {
            return RedirectToPage("./Borrow", new { bookId = id, simulate });
        }

        public IActionResult OnPost()
        {
            return RedirectToPage("./Borrow");
        }
    }
}
