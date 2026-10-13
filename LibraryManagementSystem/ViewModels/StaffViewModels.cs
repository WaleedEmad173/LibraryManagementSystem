using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class StaffItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
        public string AvatarBg { get; set; } = "#fee2e2";
        public string AvatarColor { get; set; } = "#dc2626";
        public DateTime JoinDate { get; set; }
    }

    public class StaffListViewModel
    {
        public List<StaffItemViewModel> StaffMembers { get; set; } = new();
        public int TotalPersonnel { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalLibrarians { get; set; }
        public string? Search { get; set; }
        public string? SelectedRole { get; set; }
    }

    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "Librarian";
    }

    public class EditStaffViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "Librarian";

        [DataType(DataType.Password)]
        [Display(Name = "New Password (optional)")]
        public string? NewPassword { get; set; }
    }

    public class EditStaffRoleViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = "Librarian";
        public string SelectedRole { get; set; } = "Librarian";
        public string StaffCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
    }
}
