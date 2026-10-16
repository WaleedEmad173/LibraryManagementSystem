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

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Must match exactly if updating.")]
        public string? ConfirmPassword { get; set; }

        // Context properties for Edit screen
        public string MemberCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string CardStatus { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public int CurrentlyBorrowed { get; set; }
        public int BorrowAllowance { get; set; }
        public int OverdueItems { get; set; }
        public string StandingStatus { get; set; } = string.Empty;
        public string StandingDesc { get; set; } = string.Empty;
        public string LifetimeCheckouts { get; set; } = string.Empty;

        public List<EditMemberLoanItem> ActiveLoans { get; set; } = new();
    }

    public class EditMemberLoanItem
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string DueText { get; set; } = string.Empty;
    }

    public class MemberRowViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public int ActiveBorrowings { get; set; }
        public string MemberCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class MemberListViewModel
    {
        public List<MemberRowViewModel> Members { get; set; } = new();
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalMembers { get; set; }
        public int ActiveLoansTotal { get; set; }
        public double GoodStandingPercentage { get; set; }
    }

    public class BorrowingRowViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsReturned => ReturnDate != null;
    }

    public class MemberDetailsViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public string MemberCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int ActiveLoansCount { get; set; }
        public int MaxLoansQuota { get; set; }
        public int TotalLoansCount { get; set; }
        public int ReturnedLoansCount { get; set; }
        public double ReturnRate { get; set; }
        public string StatusStanding { get; set; } = string.Empty;
        public List<BorrowingRowViewModel> Borrowings { get; set; } = new();
    }

    public class PatronActiveLoanViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue => DateTime.UtcNow > DueDate;
    }

    public class MemberDeleteViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string MemberCode { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public int CompletedLoansCount { get; set; }
        public bool CanDelete { get; set; } = true;
        public string? BlockMessage { get; set; }
        public List<PatronActiveLoanViewModel> ActiveLoans { get; set; } = new();
    }
}
