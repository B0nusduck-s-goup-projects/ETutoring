using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using SchoolSystem.Models;


namespace SchoolSystem.ViewModels
{
    public class StudentDashboardVM
    {
        public string StudentName { get; set; } = string.Empty;
        public string PersonalTutor { get; set; } = string.Empty;

        // Số lượng tệp đính kèm đã tải lên
        public int UploadedDocuments { get; set; }

        // Danh sách bình luận gần nhất từ Tutor
        public List<string> RecentComments { get; set; } = new List<string>();

        // Blog của sinh viên
        public List<Blog> StudentBlogs { get; set; } = new List<Blog>();

        // Tin nhắn mới từ Tutor
        public List<Message> RecentMessages { get; set; } = new List<Message>();

        //Danh sách nhóm mà sinh viên thuộc về
        public List<Group> Groups { get; set; } = new List<Group> { };

        public List<UserWithRolesVM> GroupUsersWithRoles { get; set; } = new List<UserWithRolesVM> { };

        //Danh sách tệp đính kèm
        public List<AttachFiles> RecentFiles { get; set; } = new List<AttachFiles> { };
    }
    //public class DashboardEntry
    //{
    //    public DateTime Timestamp { get; set; }
    //    public string UserCode { get; set; } = string.Empty;
    //    public string UserName { get; set; } = string.Empty;
    //    public string Content { get; set; } = string.Empty;
    //}
}
