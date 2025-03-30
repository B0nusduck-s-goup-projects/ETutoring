using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
	public class AppUser : IdentityUser
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[StringLength(255)]
		public string? Address { get; set; }

		[Required]
		[StringLength(20)]
		public string Code { get; set; } 

		[Required]
		[StringLength(10)]
		public string Gender { get; set; } 

		public string? Image { get; set; } 

		public ICollection<Message>? Messages { get; set; }
        public ICollection<Group>? Group { get; set; }
        public ICollection<GroupUsers>? GroupUsers { get; set; }
		public ICollection<Document>? Documents { get; set; }
    }
}

