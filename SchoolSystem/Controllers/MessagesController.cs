using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;

namespace SchoolSystem.Controllers
{
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessagesController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ChatWindow(int groupId)
        {
            AppUser? currentUser = await _userManager.GetUserAsync(this.User);
            Group? group = _context.Groups.Where(g => g.Id == groupId).FirstOrDefault();
            List<Message> messages = new List<Message>();
            messages = _context.Messages.Where(m => m.Group == group)
                                .Include(m => m.Sender)
                                .OrderBy(m => m.TimeStamp)
                                .ToList();
            ChatWindowVM viewModel = new ChatWindowVM()
            {
                CurrentUser = currentUser,
                CurrentGroup = group,
                Messages = messages,
            };
            return View(viewModel);
        }
        [Route("messages/")]
        public async Task<IActionResult> ChatList()
        {
            AppUser? currentUser = await _userManager.GetUserAsync(this.User);
            List<ChatListVM> result = new List<ChatListVM>();
            if (currentUser != null)
            {
                List<Group> groups = _context.Groups.Where( g => g.User.Contains(currentUser!)).Include(g => g.User).ToList();
                foreach (Group group in groups)
                {
                    AppUser otherUser = group.User.Where(u => u.Id != currentUser.Id).FirstOrDefault()!;
                    ChatListVM instance = new ChatListVM()
                    {
                        UserName = otherUser.Name,
                        groupId = group.Id,
                        image = otherUser.Image,
                        IsValid = group.IsValid,
                    };
                    result.Add(instance);
                }
            }
            return View(result);
        }
    }
}
