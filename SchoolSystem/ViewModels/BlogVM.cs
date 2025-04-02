using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class BlogVM
	{
		public int Id { get; set; }
		[Required]
		public string Title { get; set; }

		[Required]
		public string Content { get; set; }

		public IFormFile? Image { get; set; }
		public string? ExistingImage { get; set; }
	}
}

