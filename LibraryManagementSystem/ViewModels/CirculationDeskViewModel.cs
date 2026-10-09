using System;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModels
{
    public class PatronOptionViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int ActiveLoansCount { get; set; }
        public int MaxQuota { get; set; } = 3;
        public int OverdueCount { get; set; }
        public string Standing { get; set; } = "Good Standing";
    }

    public class BookOptionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = "2nd Floor • Section T4, Shelf 12";
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int ActiveLoans => Math.Max(0, TotalCopies - AvailableCopies);
        public bool InStock => AvailableCopies > 0;
    }

    public class CirculationBorrowPageViewModel
    {
        public int SelectedUserId { get; set; }
        public int SelectedBookId { get; set; }
        public DateTime BorrowDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
        public int TransactedCount { get; set; }

        public List<PatronOptionViewModel> Members { get; set; } = new();
        public List<BookOptionViewModel> Books { get; set; } = new();

        public PatronOptionViewModel? SelectedMember { get; set; }
        public BookOptionViewModel? SelectedBook { get; set; }

        // Alert banner states
        public string? AlertState { get; set; } // "success", "nostock", "duplicate"
        public string? AlertTitle { get; set; }
        public string? AlertMessage { get; set; }
        public string? AlertReference { get; set; }
        public DateTime? AlertDueDate { get; set; }
    }

    public class ActiveLoanItemViewModel
    {
        public int BorrowingId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerEmail { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string BorrowerInitials { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate => BorrowDate.AddDays(14);
        public bool IsOverdue => DateTime.Today > DueDate;
        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate).Days);
        public int DaysRemaining => Math.Max(0, (DueDate - DateTime.Today).Days);
    }

    public class CirculationReturnPageViewModel
    {
        public string? SearchQuery { get; set; }
        public string Filter { get; set; } = "all";
        public int ReturnsTodayCount { get; set; }
        public int OverdueItemsCount { get; set; }
        public int TotalActiveLoansCount { get; set; }

        public List<ActiveLoanItemViewModel> ActiveLoans { get; set; } = new();

        // Return feedback banner
        public string? AlertState { get; set; } // "success", "error"
        public string? AlertTitle { get; set; }
        public string? AlertMessage { get; set; }
        public string? AlertReference { get; set; }
        public string? AlertBookTitle { get; set; }
        public int AlertAvailableCopies { get; set; }
    }
}
