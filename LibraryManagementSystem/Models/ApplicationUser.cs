using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
    }
}
