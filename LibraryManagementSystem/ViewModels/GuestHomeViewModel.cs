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
        public double Rating { get; set; } = 4.8;
        public int ReviewCount { get; set; } = 124;
        public bool IsAvailable { get; set; } = true;
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public string? ImageUrl { get; set; }
        public string BgGradient { get; set; } = "linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%)";
        public string AccentColor { get; set; } = "#3b82f6";
    }

    public class GuestHomeViewModel
    {
        public int TotalTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int TotalMembers { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBorrowings { get; set; }
        public int TotalStudyRooms { get; set; } = 8;
        public List<Category> Categories { get; set; } = new();
        public List<FeaturedBookItem> FeaturedBooks { get; set; } = new();
    }
}
