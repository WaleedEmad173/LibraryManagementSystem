using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class MemberFormViewModel
    {
        public int? Id { get; set; } // used by Edit only

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone number is not valid")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        // Required on Create, optional on Edit (leave empty = keep current password)
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }
    }

    public class MemberRowViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public int ActiveBorrowings { get; set; }
    }

    public class MemberListViewModel
    {
        public List<MemberRowViewModel> Members { get; set; } = new();
        public string? Search { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class BorrowingRowViewModel
    {
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }

    public class MemberDetailsViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public List<BorrowingRowViewModel> Borrowings { get; set; } = new();
    }

    public class MemberDeleteViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public bool CanDelete { get; set; } = true;
        public string? BlockMessage { get; set; }
    }
}
