using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    // Used by both the Details and Delete pages
    public class BookDetailsViewModel
    {
        public Book Book { get; set; } = null!;
        public int HistoryCount { get; set; }

        public int Total => Book.TotalCopies;
        public int Available => Book.AvailableCopies;
        public int OnLoan => Book.TotalCopies - Book.AvailableCopies;

        public int LoanedPercent => Total == 0 ? 0 : (int)Math.Round(OnLoan * 100.0 / Total);
        public bool HasActiveLoans => OnLoan > 0;
        public bool CanDelete => !HasActiveLoans && HistoryCount == 0;
    }
}