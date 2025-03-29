using System;
using System.Collections.Generic;
using SchoolSystem.Models;

namespace SchoolSystem.ViewModels
{
    public class TutorDashboardVM
    {
        public string TutorName { get; set; } = string.Empty;
        public int UploadedDocuments { get; set; }
        public List<string> RecentComments { get; set; } = new List<string>();
        public List<Message> RecentMessages { get; set; } = new List<Message>();

        public List<Group> AssignedGroups { get; set; } = new List<Group> { };
    }
}
