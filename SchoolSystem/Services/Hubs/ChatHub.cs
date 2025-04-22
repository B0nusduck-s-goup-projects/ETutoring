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
            //get the current user and verify that they belong to the selected group
            AppUser? currentUser = await _user.GetUserAsync(Context.User);
            bool Exist = _context.Groups.Any(g => g.Id == currentGroupId && g.User.Contains(currentUser));
            if (Exist)
            {
                //add this session to a hub group with id set as group id on the server to handle live transmission
                await Groups.AddToGroupAsync(Context.ConnectionId, currentGroupId.ToString());
            }
        }

        //function callable from client
        public async Task SendMessage(int currentGroupId, string textContent)
        {
            //save data to server
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
            //call to client's function to print message
            await Clients.Group(currentGroupId.ToString()).SendAsync("ReceiveMessage", currentUser.Name, currentUser.Id , textContent);
        }
    }
}
