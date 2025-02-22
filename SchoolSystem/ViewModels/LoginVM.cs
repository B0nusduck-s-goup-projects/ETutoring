using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class LoginVM
	{
		[Required(ErrorMessage="User name is required.")]
		public string? Username { get; set; }

		[Required(ErrorMessage = "Password is required.")]
		[DataType(DataType.Password)]
		public string? Password { get; set; }

		[Display(Name="Remember me")]
		public bool RememberMe { get; set; }
	}
}
