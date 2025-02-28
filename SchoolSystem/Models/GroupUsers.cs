using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class GroupUsers
    {
        [Required]
        public required int GroupId { get; set; }
        //public Group Group { get; set; } = null!;
        [Required]
        public required string UserId { get; set; }
        //public AppUser User { get; set; } = null!;
    }
}
