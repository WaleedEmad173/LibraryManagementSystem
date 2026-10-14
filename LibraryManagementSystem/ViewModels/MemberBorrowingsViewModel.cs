namespace LibraryManagementSystem.ViewModels
{
    public class MemberBorrowingsViewModel
    {
        public List<BorrowingHistoryItemViewModel> Borrowings { get; set; } = new();
        public int MemberId { get; set; }
        public int TotalBorrowedCount { get; set; }
        public int ActiveBorrowingsCount { get; set; }
        public int OverdueCount { get; set; }
        public DateTime? NextDueDate { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int MatchingRecordCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
