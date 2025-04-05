using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using SchoolSystem.Data;
using SchoolSystem.Models;
using System.Threading.Tasks.Dataflow;

namespace SchoolSystem.Services.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _user;
        //variables wont lasted after each calls, causing null reference if calling anything other than add to groups
        public ChatHub(AppDbContext context, UserManager<AppUser> user)
        {
            _context = context;
            _user = user;
        }

        public  async Task AddToGroup(int currentGroupId)
        {
            AppUser? currentUser = await _user.GetUserAsync(Context.User);
            bool Exist = _context.Groups.Any(g => g.Id == currentGroupId && g.User.Contains(currentUser));
            if (Exist)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, currentGroupId.ToString());
            }
        }

        //todo:
        //-possibly adding a connection id feild (nullable string) to group
        //for repeated connection to a chat
        public async Task SendMessage(int currentGroupId, string textContent)
        {
            AppUser? currentUser = await _user.GetUserAsync(Context.User);
            Message message = new Message()
            {
                SenderId = currentUser.Id,
                GroupId = currentGroupId,
                TextContent = textContent,
                TimeStamp = DateTime.Now,
            };
            await _context.AddAsync(message);
            await _context.SaveChangesAsync();
            await Clients.Group(currentGroupId.ToString()).SendAsync("ReceiveMessage", currentUser.Name, currentUser.Id , textContent);
        }
    }
}
