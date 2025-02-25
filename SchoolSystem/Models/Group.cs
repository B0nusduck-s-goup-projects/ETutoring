using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string StudentId { get; set; }
        public AppUser Student { get; set; } = null!;
        [Required]
        public required string TutorId {  get; set; }
        public AppUser Tutor { get; set; } = null!;
        //public DateTime? TimeStamp { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}
