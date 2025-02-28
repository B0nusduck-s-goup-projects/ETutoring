using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
	public class UpdateUserVM
	{
		public string Id { get; set; } 

		[Required]
		public string Name { get; set; }

		[Required]
		[StringLength(20)]
		public string Code { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[DataType(DataType.MultilineText)]
		public string? Address { get; set; }

		[Required]
		public string Gender { get; set; }

		public IFormFile? Image { get; set; } // Allow new image uploads

		public string? ExistingImage { get; set; } // Store old image path

		[Required(ErrorMessage = "Please select a role.")]
		public string Role { get; set; }
	}
}

