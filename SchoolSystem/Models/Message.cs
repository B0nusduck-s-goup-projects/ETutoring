using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string SenderId {  get; set; }
        public AppUser Sender { get; set; } = null!;
        [Required]
        public required int GroupId { get; set; }
        public Group Group { get; set; } = null!;
        [Required]
        public required string TextContent { get; set; }
        [Required]
        public required int FileCount { get; set; }
        [Required]
        public required DateTime TimeStamp { get; set; }

        public ICollection<AttachFiles>? AttachFiles { get; set; }
    }
}
