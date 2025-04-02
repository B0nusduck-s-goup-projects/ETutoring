using SchoolSystem.Models;

namespace SchoolSystem.ViewModels
{
    public class ChatWindowVM
    {
        public AppUser CurrentUser { get; set; } = null!;
        public Group CurrentGroup { get; set; } = null!;
        public IEnumerable<Message>? Messages { get; set; }
    }
}
