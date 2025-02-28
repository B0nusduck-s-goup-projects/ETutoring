using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Data;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class GroupUsersController : Controller
    {
        private readonly AppDbContext _context;

        public GroupUsersController(AppDbContext context)
        {
            _context = context;
        }

        public bool HasUser(string id)
        {
            return _context.GroupUsers.Any(u => u.UserId == id);
        }
        
        public bool HasGroup(int id)
        {
            return _context.GroupUsers.Any(u => u.GroupId == id);
        }


        public List<GroupUsers>? GetUsers(string id)
        {
            return HasUser(id) ? _context.GroupUsers.Where(u => u.UserId == id).ToList() : null;
        }
        
        public List<GroupUsers>? GetGroup(int id)
        {
            return HasGroup(id) ? _context.GroupUsers.Where(u => u.GroupId == id).ToList() : null;
        }
    }
}
