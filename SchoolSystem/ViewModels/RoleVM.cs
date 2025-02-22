using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
	public class RoleVM
	{
		[Required(ErrorMessage = "Role name is required")]
		public string? Name { get; set; }
	}
}

