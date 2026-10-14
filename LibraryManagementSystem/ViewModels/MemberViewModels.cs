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

        public string MembershipTier { get; set; } = "Standard Member (5 loans max)";

        // Required on Create, optional on Edit (leave empty = keep current password)
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Must match exactly if updating.")]
        public string? ConfirmPassword { get; set; }

        // Context properties for Edit screen
        public string MemberCode { get; set; } = "#MB-4020";
        public string Initials { get; set; } = "SA";
        public string CardStatus { get; set; } = "Verified";
        public DateTime JoinDate { get; set; } = new DateTime(2023, 10, 12);
        public int CurrentlyBorrowed { get; set; } = 2;
        public int BorrowAllowance { get; set; } = 5;
        public int OverdueItems { get; set; } = 0;
        public string StandingStatus { get; set; } = "Good";
        public string StandingDesc { get; set; } = "Clean ledger & zero holds lost";
        public string OutstandingFines { get; set; } = "$0.00 USD";
        public string CardRenewalDue { get; set; } = "Oct 12, 2025";
        public string LifetimeCheckouts { get; set; } = "43 Volumes";

        public List<EditMemberLoanItem> ActiveLoans { get; set; } = new();
        public List<MemberAuditLogItem> AuditLogs { get; set; } = new();
    }

    public class EditMemberLoanItem
    {
        public string Title { get; set; } = string.Empty;
        public string Dewey { get; set; } = "005.759";
        public string Barcode { get; set; } = "#BOK-0402";
        public string DueText { get; set; } = "Due in 14 days (Nov 15, 2024)";
    }

    public class MemberAuditLogItem
    {
        public string Title { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
    }

    public class MemberRowViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public int ActiveBorrowings { get; set; }
        public string MemberCode { get; set; } = "#MEM-4091";
        public string Initials { get; set; } = "SA";
        public string Role { get; set; } = "Member";
    }

    public class MemberListViewModel
    {
        public List<MemberRowViewModel> Members { get; set; } = new();
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalMembers { get; set; } = 356;
        public int ActiveLoansTotal { get; set; } = 87;
        public double GoodStandingPercentage { get; set; } = 98.2;
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
        public string MemberCode { get; set; } = "#MB-4018";
        public string Initials { get; set; } = "SA";
        public int ActiveLoansCount { get; set; } = 2;
        public int MaxLoansQuota { get; set; } = 5;
        public int TotalLoansCount { get; set; } = 9;
        public double ReturnRate { get; set; } = 100.0;
        public string StatusStanding { get; set; } = "In Good Standing";
        public string OutstandingFines { get; set; } = "No Outstanding Fines";
        public List<BorrowingRowViewModel> Borrowings { get; set; } = new();
    }

    public class PatronActiveLoanViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
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
