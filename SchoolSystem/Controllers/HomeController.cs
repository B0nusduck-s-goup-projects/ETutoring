using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.ViewModels;
using SchoolSystem.Models;
using SchoolSystem.Data;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace SchoolSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public HomeController(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Dashboard(string searchName = null, int? groupId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy ID người dùng hiện tại
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId); // Tìm người dùng trong cơ sở dữ liệu

        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Thiết lập ViewData để truyền userId sang view
        ViewData["CurrentUserId"] = userId;

        // Log các vai trò của người dùng
        var roles = await _userManager.GetRolesAsync(user);
        Debug.WriteLine($"User roles: {string.Join(", ", roles)}");

        // Kiểm tra vai trò của người dùng
        if (await _userManager.IsInRoleAsync(user, "Student"))
        {
            var studentDashboard = await GetStudentDashboard(userId);
            return View("StudentDashboard", studentDashboard);
        }
        else if (await _userManager.IsInRoleAsync(user, "Tutor"))
        {
            var tutorDashboard = await GetTutorDashboard(userId, searchName, groupId);
            return View("TutorDashboard", tutorDashboard);
        }

        return Forbid();
    }
    private async Task<StudentDashboardVM> GetStudentDashboard(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null; // Check if user does not exist

        var studentGroups = await _context.GroupUsers
            .Where(g => g.UserId == userId)
            .Join(_context.Groups, gu => gu.GroupId, g => g.Id, (gu, g) => g)
            .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User) // Include GroupUsers and then AppUser
            .ToListAsync();

        var studentGroupIds = studentGroups.Select(g => g.Id).ToList();

        // Fetch the roles for each user in the groups
        var groupUsersWithRoles = new List<UserWithRolesVM>();
        foreach (var group in studentGroups)
        {
            foreach (var groupUser in group.GroupUsers)
            {
                var roles = await _userManager.GetRolesAsync(groupUser.User);
                groupUsersWithRoles.Add(new UserWithRolesVM
                {
                    User = groupUser.User,
                    Roles = roles
                });
            }
        }

        var groupUsers = await _context.GroupUsers
            .Where(gu => studentGroupIds.Contains(gu.GroupId))
            .Include(gu => gu.User)
            .ToListAsync();

        var personalTutor = groupUsers
            .AsEnumerable()
            .Where(gu => _userManager.IsInRoleAsync(gu.User, "Tutor").Result)
            .Select(gu => gu.User.Name)
            .FirstOrDefault() ?? string.Empty;

        var tutorRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Tutor"))?.Id;

        var studentBlogs = await _context.Blogs
           .Where(b => b.UserId == userId)
           .OrderByDescending(b => b.TimeStamp)
           .Take(5)
           .ToListAsync();

        var blogComments = await _context.BlogComments
            .Where(c => studentBlogs.Select(b => b.Id).Contains(c.BlogId)) // Lọc theo blog của sinh viên
            .Include(c => c.Blog)// Bao gồm thông tin Blog nếu cần
            .Include(c => c.User)
            .Include(c => c.ParentComment) // Bao gồm thông tin ParentComment nếu cần
            .Take(5) // Giới hạn số lượng comment
            .ToListAsync();

        var tutorBlogs = await _context.Blogs
            .Join(_context.UserRoles, b => b.UserId, ur => ur.UserId, (b, ur) => new { b, ur })
            .Where(bur => bur.ur.RoleId == tutorRoleId)
            .Select(bur => bur.b)
            .Include(b => b.User) // Include the User to access the User.Name in the view
            .OrderByDescending(b => b.TimeStamp)
            .Take(5)
            .ToListAsync();

        var recentMessages = await _context.Messages
            .Where(m => studentGroupIds.Contains(m.GroupId))
            .Include(m => m.Sender)
            .Include(m => m.Group)
            .OrderByDescending(m => m.TimeStamp)
            .Take(5)
            .ToListAsync();

        var documents = await _context.Documents
        .Where(d => d.UserId == userId || d.User.Group.Any(g => studentGroupIds.Contains(g.Id)))
        .OrderByDescending(d => d.UploadDate)
        .Take(5)
        .ToListAsync();

        // Log the fetched data
        Debug.WriteLine($"Student Name: {user.Name}");
        Debug.WriteLine($"Blog Comments: {string.Join(", ", blogComments.Select(c => c.Content))}");
        Debug.WriteLine($"Student Blogs: {string.Join(", ", studentBlogs.Select(b => b.Title))}");
        Debug.WriteLine($"Tutor Blogs: {string.Join(", ", tutorBlogs.Select(b => b.Title))}");
        Debug.WriteLine($"Recent Messages: {string.Join(", ", recentMessages.Select(m => m.TextContent))}");
        Debug.WriteLine($"Groups: {string.Join(", ", studentGroups.Select(g => g.Id))}");
        Debug.WriteLine($"Personal Tutor: {personalTutor}");

        return new StudentDashboardVM
        {
            StudentName = user.Name,
            PersonalTutor = personalTutor,
            BlogComments = blogComments,
            StudentBlogs = studentBlogs,
            TutorBlogs = tutorBlogs,
            RecentMessages = recentMessages,
            Groups = studentGroups,
            GroupUsersWithRoles = groupUsersWithRoles,
            Documents = documents
        };
    }

    private async Task<TutorDashboardVM> GetTutorDashboard(string userId, string searchName, int? groupId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null; // Check if user does not exist

        // Fetch assigned groups
        var assignedGroups = await _context.Groups
            .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .Where(g => g.GroupUsers.Any(gu => gu.UserId == userId))
            .ToListAsync();

        var assignedGroupIds = assignedGroups.Select(g => g.Id).ToList();

        // Fetch group users with roles
        var groupUsersWithRoles = new List<UserWithRolesVM>();
        foreach (var group in assignedGroups)
        {
            foreach (var groupUser in group.GroupUsers)
            {
                var roles = await _userManager.GetRolesAsync(groupUser.User);
                groupUsersWithRoles.Add(new UserWithRolesVM
                {
                    User = groupUser.User,
                    Roles = roles.ToList()
                });
            }
        }

        // Filter students based on search criteria and role
        var filteredStudentsQuery = _context.Users.AsQueryable();

        // Ensure only users with the "Student" role are included
        var studentRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student"))?.Id;
        filteredStudentsQuery = filteredStudentsQuery
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId));

        if (!string.IsNullOrEmpty(searchName))
        {
            filteredStudentsQuery = filteredStudentsQuery.Where(u => u.Name.Contains(searchName));
        }
        if (groupId.HasValue)
        {
            filteredStudentsQuery = filteredStudentsQuery.Where(u => u.GroupUsers.Any(gu => gu.GroupId == groupId.Value));
        }

        var filteredStudents = await filteredStudentsQuery.ToListAsync();

        // Fetch recent messages
        var recentMessages = await _context.Messages
             .Where(m => assignedGroupIds.Contains(m.GroupId))
             .Include(m => m.Sender)
             .Include(m => m.Group)
             .OrderByDescending(m => m.TimeStamp)
             .Take(5)
             .ToListAsync();


        //// Fetch student blogs (blogs uploaded by students in the tutor's assigned groups)
        //var studentRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student"))?.Id;

        var studentBlogs = await _context.Blogs
            .Join(_context.UserRoles, b => b.UserId, ur => ur.UserId, (b, ur) => new { b, ur })
            .Where(bur => bur.ur.RoleId == studentRoleId &&
                          _context.GroupUsers.Any(gu => gu.UserId == bur.b.UserId && assignedGroupIds.Contains(gu.GroupId)))
            .Select(bur => bur.b)
            .Include(b => b.User) // Include the User to access the User.Name in the view
            .OrderByDescending(b => b.TimeStamp)
            .Take(5)
            .ToListAsync();

        // Fetch tutor blogs (blogs uploaded by the tutor)
        var tutorBlogs = await _context.Blogs
            .Where(b => b.UserId == userId) // Filter by the tutor's user ID
            .OrderByDescending(b => b.TimeStamp)
            .Take(5)
            .ToListAsync();


        //Fetch documents
        var documents = await _context.Documents
            .Where(d => d.User.Group.Any(g => assignedGroupIds.Contains(g.Id)))
            .OrderByDescending(d => d.UploadDate)
            .Take(5)
            .ToListAsync();
        //var documents = await _context.Documents
        //    .Where(d => d.UserId == userId)
        //    .OrderByDescending(d => d.UploadDate)
        //    .Take(5)
        //    .ToListAsync();


        // Log the fetched data
        Debug.WriteLine($"Tutor Name: {user.Name}");
        Debug.WriteLine($"Uploaded Documents: {documents.Count}");
        Debug.WriteLine($"Recent Messages: {string.Join(", ", recentMessages.Select(m => m.TextContent))}");
        Debug.WriteLine($"Assigned Groups: {string.Join(", ", assignedGroups.Select(g => g.Id))}");
        Debug.WriteLine($"Tutor Blogs: {string.Join(", ", tutorBlogs.Select(b => b.Title))}");
        Debug.WriteLine($"Student Blogs: {string.Join(", ", studentBlogs.Select(b => b.Title))}");

        return new TutorDashboardVM
        {
            TutorName = user.Name,
            RecentMessages = recentMessages,
            AssignedGroups = assignedGroups,
            GroupUsersWithRoles = groupUsersWithRoles,
            SearchName = searchName,
            FilteredStudents = filteredStudents,
            TutorBlogs = tutorBlogs,
            StudentBlogs = studentBlogs,
            Documents = documents
        };
    }

    //[HttpPost]
    //[Authorize(Roles = "Tutor")]
    //public async Task<IActionResult> RemoveStudentFromGroup(int groupId, string studentId)
    //{
    //    var groupUser = await _context.GroupUsers
    //        .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == studentId);

    //    if (groupUser == null)
    //    {
    //        return NotFound();
    //    }

    //    _context.GroupUsers.Remove(groupUser);
    //    await _context.SaveChangesAsync();

    //    return Ok();
    //}
	[Authorize(Roles = "Admin,Staff")]
	public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "Student,Tutor")]
    public IActionResult IndexUser()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
