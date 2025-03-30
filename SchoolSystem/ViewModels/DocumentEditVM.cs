using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentEditVM
    {
        public int Id { get; set; } // Needed to identify which file to edit

        [Required(ErrorMessage = "Title is required")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        public required string ExistingFilePath { get; set; }

        public IFormFile? NewFile { get; set; }
    }
}
// Bị lỗi CS8618 như bên DocumentVM nên phải thêm "required" 