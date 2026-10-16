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
        public string IconClass { get; set; } = "bi-folder2-open";
        public string ProgressColor { get; set; } = "#1e3a8a";

        // Properties for Edit & Details views
        public int TotalCopies { get; set; }
        public int InCirculationLoans { get; set; }
        public int InStockCount => Math.Max(0, TotalCopies - InCirculationLoans);
        public double OnShelvesPct => TotalCopies > 0 ? Math.Round((double)InStockCount / TotalCopies * 100, 1) : 0;
    }

    public class CategoryListViewModel
    {
        public List<CategoryViewModel> Categories { get; set; } = new();
        public int TotalCategories { get; set; }
        public int CategorizedBooks { get; set; }
        public string LargestCategoryName { get; set; } = "—";
        public int LargestCategoryCount { get; set; }
        public double LargestCategoryShare { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
    }

    public class CategoryBookExcerptViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public bool HasActiveLoan { get; set; }
    }

    public class DeleteCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BooksCount { get; set; }
        public int ActiveLoansCount { get; set; }
        public int UniqueAuthorsCount { get; set; }
        public string TaxonomyCode { get; set; } = string.Empty;
        public bool CanDelete { get; set; } = true;
        public string? BlockMessage { get; set; }
        public List<CategoryBookExcerptViewModel> AssociatedBooks { get; set; } = new();
    }
}
