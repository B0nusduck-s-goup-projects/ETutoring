using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentDetailsVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public bool CanEdit { get; set; }
    }
}