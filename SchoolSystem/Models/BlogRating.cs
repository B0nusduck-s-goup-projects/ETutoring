using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
	public class BlogRating
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; }
		public AppUser User { get; set; }

		[Required]
		public int BlogId { get; set; }
		public Blog Blog { get; set; }

		[Required]
		[Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
		public int Rating { get; set; }
	}

}
