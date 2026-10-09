using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class ConfirmBorrowViewModel
    {
        // Book Details
        public int BookId { get; set; }
        public string Title { get; set; } = "Clean Code: A Handbook of Agile Software Craftsmanship";
        public string Author { get; set; } = "Robert C. Martin (Uncle Bob)";
        public string ISBN { get; set; } = "978-0132350884";
        public string CategoryName { get; set; } = "ENGINEERING / TECH";
        public string Edition { get; set; } = "1st Edition (Revised)";
        public string ShelfLocation { get; set; } = "2nd Floor • Section T4, Shelf 12";
        public string Barcode { get; set; } = "BC-9942-0193-CC";
        public string AccessStatus { get; set; } = "Ready for immediate pickup";
        public int StandardLoanPeriodDays { get; set; } = 14;
        public int TotalCopies { get; set; } = 5;
        public int AvailableCopies { get; set; } = 3;

        // Borrower Details
        public int UserId { get; set; }
        public string BorrowerName { get; set; } = "Sara Ahmed";
        public string MemberId { get; set; } = "#MB-4820";
        public string BorrowerTier { get; set; } = "Standard Academic";
        public string BorrowerInitials { get; set; } = "SA";

        // Loan Timing
        public DateTime CheckoutDate { get; set; } = DateTime.Today;
        public DateTime ScheduledDueDate { get; set; } = DateTime.Today.AddDays(14);
        public string PickupDesk { get; set; } = "Central Academic Library - Main Desk (Level 1)";

        // Feedback / Result States
        public string? ActiveFeedbackState { get; set; } // "success", "nostock", "duplicate", or null
        public string? FeedbackMessage { get; set; }
        public string? FeedbackBadge { get; set; }
        public bool IsSimulated { get; set; }
    }

    public class BorrowingHistoryItemViewModel
    {
        public int BorrowingId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerEmail { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string BorrowerInitials { get; set; } = "MB";
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime DueDate => BorrowDate.AddDays(14);
        public bool IsReturned => ReturnDate.HasValue;
        public bool IsOverdue => !IsReturned && DateTime.Today > DueDate;
        public int DaysRemaining => IsReturned ? 0 : Math.Max(0, (DueDate - DateTime.Today).Days);
        public int DaysOverdue => IsOverdue ? (DateTime.Today - DueDate).Days : 0;
    }

    public class BorrowingIndexViewModel
    {
        public List<BorrowingHistoryItemViewModel> Borrowings { get; set; } = new();
        public int CurrentlyBorrowedCount { get; set; }
        public int TotalBorrowedCount { get; set; }
        public int CompletedReturnsCount => Math.Max(0, TotalBorrowedCount - CurrentlyBorrowedCount);
        public int OverdueCount { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchMember { get; set; }
        public string? SearchBook { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
