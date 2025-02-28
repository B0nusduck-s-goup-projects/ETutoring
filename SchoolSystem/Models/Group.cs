using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string StudentId { get; set; }
        public Student Student { get; set; } = null!;
        [Required]
        public required string TutorId {  get; set; }
        public Tutor Tutor { get; set; } = null!;
        public bool IsValid { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? ExpiredTime { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}
