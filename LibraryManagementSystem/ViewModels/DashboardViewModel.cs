using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class BorrowingItemViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime DueDate => BorrowDate.AddDays(CirculationPolicy.LoanPeriodDays);
        public bool IsReturned => ReturnDate.HasValue;
        public bool IsOverdue => !ReturnDate.HasValue && DateTime.Now > DueDate;
    }

    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PrimaryRole { get; set; } = "Member";
        public bool IsStaff => PrimaryRole == "Admin" || PrimaryRole == "Librarian";

        // Catalog statistics
        public int TotalCatalogTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int CopiesOnLoan { get; set; }
        public int ZeroAvailableTitles { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }

        public int ShelfReadinessPercent => TotalCopies > 0
            ? (int)Math.Round((double)AvailableCopies / TotalCopies * 100)
            : 0;

        // User personal stats (for members)
        public int MyActiveBorrowingsCount { get; set; }
        public int MyTotalHistoryCount { get; set; }
        public List<BorrowingItemViewModel> MyBorrowings { get; set; } = new();

        // System overview lists (for Admin / Librarian)
        public List<BorrowingItemViewModel> RecentBorrowings { get; set; } = new();
        public List<Book> RecentBooks { get; set; } = new();
    }
}
