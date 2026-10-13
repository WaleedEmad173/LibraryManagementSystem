using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    // Used by both the Create and Edit pages
    public class BookFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title can't be longer than 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(20, ErrorMessage = "ISBN is too long")]
        public string ISBN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(150, ErrorMessage = "Author can't be longer than 150 characters")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Total copies is required")]
        [Range(0, 10000, ErrorMessage = "Copies must be between 0 and 10,000 (cannot be negative)")]
        public int? TotalCopies { get; set; }

        [StringLength(2000, ErrorMessage = "Description can't be longer than 2000 characters")]
        public string? Description { get; set; }

        // Display-only (filled by the controller, never trusted from the form)
        public int CopiesOnLoan { get; set; }
        public int AvailableCopies { get; set; }

        // Must be nullable, otherwise MVC treats it as a required form field
        public SelectList? Categories { get; set; }

        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
    }
}