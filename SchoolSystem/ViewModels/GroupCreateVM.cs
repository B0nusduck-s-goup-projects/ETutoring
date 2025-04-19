using SchoolSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.ViewModels
{
    public class GroupCreateVM
    {
        public List<AppUser>? Teachers { get; set; } = null!;
        public List<AppUser>? Students { get; set; } = null!;
        public List<string>? exception { get; set; }

    }
}
