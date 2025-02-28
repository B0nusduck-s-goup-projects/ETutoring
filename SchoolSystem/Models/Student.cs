using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class Student : AppUser
    {
        public required string StudentId { get; set; } // Mã sinh viên
        public int? GradeLevel { get; set; } // Sinh viên năm bao nhiêu (vd : 1, 2, 3, 4, 5)
        public string? Major { get; set; } // Chuyên ngành
        public string? Bio { get; set; } // Giới thiệu bản thân (có thể để trống)

        public Group? GroupStudent { get; set; }
    }
}
