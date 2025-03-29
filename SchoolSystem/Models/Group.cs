using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        public bool IsValid { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? ExpiredTime { get; set; }

        public ICollection<Message>? Messages { get; set; }
        public ICollection<AppUser> User { get; set; } = null!;
        public ICollection<GroupUsers> GroupUsers { get; set; } = null!;
        //public string TutorId { get; set; } = string.Empty;
    }
}
