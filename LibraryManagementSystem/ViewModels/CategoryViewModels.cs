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
    }

    public class DeleteCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BooksCount { get; set; }
        public bool CanDelete { get; set; } = true;
        public string? BlockMessage { get; set; }
    }
}