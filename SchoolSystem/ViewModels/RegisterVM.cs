using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class RegisterVM
	{
		[Required]
		public string? Name { get; set; }

		[Required]
		[DataType(DataType.EmailAddress)]
		public string? Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string? Password { get; set; }

		[Compare("Password", ErrorMessage="Passwords do not match.")]
		[Display(Name="Confirm Password")]
		[DataType(DataType.Password)]
		public string? ConfirmPassword { get; set; }

		[DataType(DataType.MultilineText)]
		public string? Address { get; set; }

		[Required(ErrorMessage = "Please select a role.")]
		public string? Role { get; set; }

		[Required]
		[StringLength(20)]
		public string Code { get; set; }

		[Required]
		public string Gender { get; set; } 

		public string? Image { get; set; }
	}
}
