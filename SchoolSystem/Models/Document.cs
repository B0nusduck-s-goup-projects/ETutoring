using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public required string FilePath { get; set; }

        [Required]
        public required string UserId { get; set; }
        public required AppUser User { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public required string FileType { get; set; }
        public long FileSize { get; set; }
		public ICollection<DocumentComment> Comments { get; set; }
	}
}
// Thêm các dòng "required" để tránh bị bắt lỗi của non-nullable property assignment khi exit constructor