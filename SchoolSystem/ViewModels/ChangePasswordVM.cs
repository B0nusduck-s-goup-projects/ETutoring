using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class ChangePasswordVM
	{
		[Required]
		[DataType(DataType.Password)]
		public string OldPassword { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

}
