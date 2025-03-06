using SchoolSystem.Models;

namespace SchoolSystem.ViewModels
{
    public class GroupIndexVM
    {
        public Group Group { get; set; } = null!;
        public AppUser Student { get; set; } = null!;
        public AppUser Teacher { get; set; } = null!;
    }
}
