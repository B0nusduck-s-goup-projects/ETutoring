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

        // Số lượng tệp đính kèm đã tải lên (không còn sử dụng vì đả xoá attach file khỏi messages)
        //public int UploadedDocuments { get; set; }

        /// Comments from blogs
        public List<BlogComment> BlogComments { get; set; } = new List<BlogComment>();

        // Comments from documents
        //public List<string> DocumentComments { get; set; } = new List<string>();

        // Blog của sinh viên
        public List<Blog> StudentBlogs { get; set; } = new List<Blog>();
        // Blog của giáo viên
        public List<Blog> TutorBlogs { get; set; } = new List<Blog>();

        // Tin nhắn mới từ Tutor
        public List<Message> RecentMessages { get; set; } = new List<Message>();

        //Danh sách nhóm mà sinh viên thuộc về
        public List<Group> Groups { get; set; } = new List<Group> { };

        // List of documents
        public List<Document> Documents { get; set; } = new List<Document>();

        public List<UserWithRolesVM> GroupUsersWithRoles { get; set; } = new List<UserWithRolesVM> { };

        //Danh sách tệp đính kèm (không còn sử dụng vì đả xoá attach file khỏi messages)
        //public List<AttachFiles> RecentFiles { get; set; } = new List<AttachFiles> { };
    }
    //public class DashboardEntry
    //{
    //    public DateTime Timestamp { get; set; }
    //    public string UserCode { get; set; } = string.Empty;
    //    public string UserName { get; set; } = string.Empty;
    //    public string Content { get; set; } = string.Empty;
    //}
}
