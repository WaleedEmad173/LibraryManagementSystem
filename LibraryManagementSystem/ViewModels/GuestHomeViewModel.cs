using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class FeaturedBookItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherYear { get; set; } = string.Empty;
        public int LoanCount { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public string? ImageUrl { get; set; }
        public string BgGradient { get; set; } = "linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%)";
    }

    public class GuestHomeViewModel
    {
        public int TotalTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int TotalMembers { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBorrowings { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<FeaturedBookItem> FeaturedBooks { get; set; } = new();
    }
}
