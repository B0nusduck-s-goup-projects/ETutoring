using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
	public class Blog
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Content { get; set; }

		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

		[Required]
		public string UserId { get; set; }
		public AppUser User { get; set; }

		public string? Image { get; set; }

		public ICollection<BlogComment> Comments { get; set; }
		public ICollection<BlogRating> Ratings { get; set; }
	}

}
