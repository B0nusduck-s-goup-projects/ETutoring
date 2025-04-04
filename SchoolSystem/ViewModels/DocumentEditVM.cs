using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentEditVM
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string ExistingFilePath { get; set; }

        [Display(Name = "New File")]
        public IFormFile NewFile { get; set; }
    }
}