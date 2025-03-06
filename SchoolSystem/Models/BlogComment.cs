using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
	public class BlogComment
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int BlogId { get; set; }
		public Blog Blog { get; set; }

		[Required]
		public string UserId { get; set; }
		public AppUser User { get; set; }

		// Foreign key for parent comment (nullable for top-level comments)
		public int? ParentCommentId { get; set; }
		public BlogComment ParentComment { get; set; }

		public ICollection<BlogComment> Replies { get; set; }

		[Required]
		public string Content { get; set; }

		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
	}

}
