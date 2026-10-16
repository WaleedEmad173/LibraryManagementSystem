using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        public int BooksCount { get; set; }

        public string Code { get; set; } = string.Empty;
        public string DepartmentNote { get; set; } = string.Empty;
        public double Percentage { get; set; }
        public bool IsHighDemand { get; set; }
        public string IconClass { get; set; } = "bi-folder2-open";
        public string ProgressColor { get; set; } = "#1e3a8a";

        // Properties for Edit & Details views
        public string TaxonomyCode { get; set; } = "#CAT-04";
        public string DdcSegment { get; set; } = "DDC Segment #004-006";
        public string CanonicalSlug { get; set; } = "cat-technology";
        public string TaxonomyScope { get; set; } = "Encompasses Computer Science, Emerging Hardware, Artificial Intelligence, Telecommunications, Software Engineering, and Digital Networking infrastructures.";
        public DateTime CreatedDate { get; set; } = new DateTime(2023, 1, 15);
        public string LastRevision { get; set; } = "Oct 29, 2023, 14:22";
        public int TotalCopies { get; set; }
        public int InCirculationLoans { get; set; }
        public int InStockCount => Math.Max(0, TotalCopies - InCirculationLoans);
        public double OnShelvesPct => TotalCopies > 0 ? Math.Round((double)InStockCount / TotalCopies * 100, 1) : 0;
        public string ShelfAisle { get; set; } = "West Wing • Aisles 14-18";
        public string ShelfFloorCollection { get; set; } = "Floor 3 • Computing & Cybernetics Collection";
        public string ShelfTileId { get; set; } = "3B";
        public List<string> SubDisciplines { get; set; } = new()
        {
            "Artificial Intelligence (112)",
            "Cloud Systems (74)",
            "Data Architecture (65)",
            "Cybersecurity (58)",
            "Embedded Robotics (71)"
        };
    }

    public class CategoryListViewModel
    {
        public List<CategoryViewModel> Categories { get; set; } = new();
        public int TotalCategories { get; set; }
        public int CategorizedBooks { get; set; }
        public string LargestCategoryName { get; set; } = "Technology";
        public int LargestCategoryCount { get; set; } = 380;
        public double LargestCategoryShare { get; set; } = 30.6;
        public string? Search { get; set; }
        public string? Sort { get; set; }
    }

    public class CategoryBookExcerptViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string ShelfPosition { get; set; } = string.Empty;
        public bool HasActiveLoan { get; set; }
    }

    public class DeleteCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BooksCount { get; set; }
        public int ActiveLoansCount { get; set; }
        public int SubSectionsCount { get; set; } = 14;
        public string TaxonomyCode { get; set; } = "#CAT-04";
        public string DeweyRange { get; set; } = "Dewey: 600-699 • Section: Tech Pavilion Wing C";
        public bool CanDelete { get; set; } = true;
        public string? BlockMessage { get; set; }
        public int CleanedCount { get; set; }
        public double CleanedPercentage { get; set; }
        public string CurrentStateView { get; set; } = "blocked"; // "blocked" or "safe"
        public List<CategoryBookExcerptViewModel> AssociatedBooks { get; set; } = new();
    }
}
