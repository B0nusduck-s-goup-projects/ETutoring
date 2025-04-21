using System;
using System.Collections.Generic;
using SchoolSystem.Models;

namespace SchoolSystem.ViewModels
{
    public class TutorDashboardVM
    {
        public string TutorName { get; set; } = string.Empty;
        //public int UploadedDocuments { get; set; }
        public List<UserWithRolesVM> GroupUsersWithRoles { get; set; } = new List<UserWithRolesVM>();

        //public List<string> RecentComments { get; set; } = new List<string>();
        public List<Message> RecentMessages { get; set; } = new List<Message>();

        public List<Group> AssignedGroups { get; set; } = new List<Group>();
        public string SearchName { get; set; }
        public List<AppUser> FilteredStudents { get; set; } = new List<AppUser>();

        // New properties for blogs, comments, and documents
        public List<Blog> StudentBlogs { get; set; } = new List<Blog>();
        public List<Blog> TutorBlogs { get; set; } = new List<Blog>();
        public List<string> BlogComments { get; set; } = new List<string>();
        public List<Document> Documents { get; set; } = new List<Document>();
    }
}
