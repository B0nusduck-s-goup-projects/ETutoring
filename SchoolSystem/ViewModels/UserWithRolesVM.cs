using SchoolSystem.Models;

namespace SchoolSystem.ViewModels
{
	public class UserWithRolesVM
	{
		public AppUser User { get; set; }
		public IEnumerable<string> Roles { get; set; }
	}
}
