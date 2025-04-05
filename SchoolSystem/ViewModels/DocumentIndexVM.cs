using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolSystem.ViewModels
{
    public class DocumentIndexVM
    {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string FilePath { get; set; }
}
}
