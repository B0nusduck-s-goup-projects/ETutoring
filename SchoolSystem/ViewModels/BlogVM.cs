using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class BlogVM
	{
		[Required]
		public string Title { get; set; }

		[Required]
		public string Content { get; set; }

		public IFormFile? Image { get; set; } 
	}
}

