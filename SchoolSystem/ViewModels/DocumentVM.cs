using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentVM
    {
        [Required(ErrorMessage = "Title is required")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        public required IFormFile File { get; set; }
    }
}// CS8618: Non-nullable properties must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or
// declaring the property as nullable (?)
