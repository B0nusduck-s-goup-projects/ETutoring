using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class AttachFiles
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required int MessageId { get; set; }
        public Message Message { get; set; } = null!;
        [Required]
        public required string FileContent { get; set; }
    }
}
