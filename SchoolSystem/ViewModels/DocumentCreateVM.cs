using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentCreateVM
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "Document File")]
        public IFormFile File { get; set; }
    }
}