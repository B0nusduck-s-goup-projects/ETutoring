using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Tutor : AppUser
    {
        public required string TutorId { get; set; } // Mã giáo viên
        public string? SubjectSpecialization { get; set; } // Chuyên ngành giảng dạy
        public string? Qualification { get; set; } // Bằng cấp
        public string? Bio { get; set; } // Giới thiệu bản thân (có thể để trống)

        public ICollection<Group>? GroupTutor { get; set; }
    }
}
