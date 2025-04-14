using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class DocumentCommentVM
	{
		[Required]
		public string Content { get; set; }

		public int DocumentId { get; set; }

		public int? ParentCommentId { get; set; }
	}
}
