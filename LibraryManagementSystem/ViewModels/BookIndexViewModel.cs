using LibraryManagementSystem.Helpers;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.ViewModels
{
    public class BookIndexViewModel
    {
        public PaginatedList<Book> Books { get; set; } = null!;

        // Current search / filter / sort state
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public bool AvailableOnly { get; set; }
        public string? SortOrder { get; set; }
        public SelectList Categories { get; set; } = null!;

        // Stat cards (whole catalog)
        public int TotalTitles { get; set; }
        public int CatalogCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int CopiesOnLoan { get; set; }
        public int ZeroAvailable { get; set; }

        public int ShelfReadinessPercent =>
            CatalogCopies == 0 ? 0 : (int)Math.Round(AvailableCopies * 100.0 / CatalogCopies);
    }
}