using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
    public class CreateGroupVM
    {
        [Required(ErrorMessage = "A tutor id is requred to create a group.")]
        public string TeacherID { get; set; } = null!;
        [Required(ErrorMessage = "One or more student id is required to create a group.")]
        public string StudentID { get; set; } = null!;
        public ICollection<string>? exception { get; set; }

    }
}
