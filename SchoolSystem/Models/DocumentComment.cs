using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
	public class DocumentComment
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int DocumentId { get; set; }
		public Document Document { get; set; }

		[Required]
		public string UserId { get; set; }
		public AppUser User { get; set; }

		// Foreign key for parent comment (nullable for top-level comments)
		public int? ParentCommentId { get; set; }
		public DocumentComment ParentComment { get; set; }

		public ICollection<DocumentComment> Replies { get; set; }

		[Required]
		public string Content { get; set; }

		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
	}

}

