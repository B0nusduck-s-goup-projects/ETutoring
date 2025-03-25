using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using SchoolSystem.Data;
using SchoolSystem.Models;

namespace SchoolSystem.Services.Hubs
{
    public class ChatHub : Hub
    {
        private readonly UserManager<AppUser> _user;
        private readonly AppDbContext _context;
        public ChatHub(UserManager<AppUser> user, AppDbContext context)
        {
            _user = user;
            _context = context;
        }

        //todo:
        //-update group user model to make it able to retrive the associated
        //user and group for connection establishment
        //-possibly adding a connection id feild (nullable string) to group
        //for repeated connection to a chat
        public async Task SentMessage(GroupUsers connection, Message message)
        {
            string? role = (await _user.GetRolesAsync(connection.User)).FirstOrDefault();
            if (role == null || role != "Student" || role != "Tutor")
                return;
            //List<Message> data = _context.Messages.Where(m => m.GroupId == connection.GroupId).OrderBy(m => m.TimeStamp).ToList();
            await Groups.AddToGroupAsync(Context.ConnectionId, connection.GroupId.ToString());
            await Clients.Group(connection.GroupId.ToString()).SendAsync("recive message", connection.User, message);
        }
    }
}
