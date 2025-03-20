using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class CommentVM
	{
		[Required]
		public string Content { get; set; }

		public int BlogId { get; set; } 

		public int? ParentCommentId { get; set; } 
	}
}
