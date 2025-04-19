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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the current user's ID
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId); // Find the user in the database

        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Log the user roles
        var roles = await _userManager.GetRolesAsync(user);
        Debug.WriteLine($"User roles: {string.Join(", ", roles)}");

        // Check if the user is a student or a tutor
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

        var blogComments = await _context.BlogComments
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.TimeStamp)
            .Select(c => c.Content)
            .Take(5)
            .ToListAsync();

        var studentBlogs = await _context.Blogs
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.TimeStamp)
            .ToListAsync();

        var tutorRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Tutor"))?.Id;

        var tutorBlogs = await _context.Blogs
            .Join(_context.UserRoles, b => b.UserId, ur => ur.UserId, (b, ur) => new { b, ur })
            .Where(bur => bur.ur.RoleId == tutorRoleId)
            .Select(bur => bur.b)
            .Include(b => b.User) // Include the User to access the User.Name in the view
            .OrderByDescending(b => b.TimeStamp)
            .ToListAsync();

        var recentMessages = await _context.Messages
            .Where(m => studentGroupIds.Contains(m.GroupId))
            .OrderByDescending(m => m.TimeStamp)
            .Take(5)
            .ToListAsync();

        var documents = await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();

        // Log the fetched data
        Debug.WriteLine($"Student Name: {user.Name}");
        Debug.WriteLine($"Blog Comments: {string.Join(", ", blogComments)}");
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

        var assignedGroups = await _context.Groups
            .Include(g => g.GroupUsers) // Include GroupUsers
                .ThenInclude(gu => gu.User) // Include AppUser
            .Where(g => g.GroupUsers.Any(gu => gu.UserId == userId))
            .ToListAsync();

        var assignedGroupIds = assignedGroups.Select(g => g.Id).ToList();

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

        groupUsersWithRoles = groupUsersWithRoles
            .OrderByDescending(gu => gu.Roles.Contains("Tutor"))
            .ThenBy(gu => gu.User.Name)
            .ToList();

        var recentMessages = await _context.Messages
            .Where(m => assignedGroupIds.Contains(m.GroupId))
            .OrderByDescending(m => m.TimeStamp)
            .Take(5)
            .ToListAsync();

        var recentComments = await _context.BlogComments
            .Where(c => _context.Blogs
                .Where(b => assignedGroupIds.Contains(b.User.Group.FirstOrDefault().Id))
                .Select(b => b.Id)
                .Contains(c.BlogId))
            .OrderByDescending(c => c.TimeStamp)
            .Select(c => c.Content)
            .Take(5)
            .ToListAsync();

        var filteredStudentsQuery = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchName))
        {
            filteredStudentsQuery = filteredStudentsQuery.Where(u => u.Name.Contains(searchName));
        }

        if (groupId.HasValue)
        {
            filteredStudentsQuery = filteredStudentsQuery.Where(u => u.GroupUsers.Any(gu => gu.GroupId == groupId.Value));
        }

        var filteredStudents = await filteredStudentsQuery.ToListAsync();

        // Log the fetched data
        Debug.WriteLine($"Tutor Name: {user.Name}");
        Debug.WriteLine($"Uploaded Documents: {recentMessages.Count()}");
        Debug.WriteLine($"Recent Comments: {string.Join(", ", recentComments)}");
        Debug.WriteLine($"Recent Messages: {string.Join(", ", recentMessages.Select(m => m.TextContent))}");
        Debug.WriteLine($"Assigned Groups: {string.Join(", ", assignedGroups.Select(g => g.Id))}");

        return new TutorDashboardVM
        {
            TutorName = user.Name,
            UploadedDocuments = recentMessages.Count(),
            RecentComments = recentComments,
            RecentMessages = recentMessages,
            AssignedGroups = assignedGroups,
            GroupUsersWithRoles = groupUsersWithRoles,
            SearchName = searchName,
            FilteredStudents = filteredStudents
        };
    }

    [HttpPost]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> RemoveStudentFromGroup(int groupId, string studentId)
    {
        var groupUser = await _context.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == studentId);

        if (groupUser == null)
        {
            return NotFound();
        }

        _context.GroupUsers.Remove(groupUser);
        await _context.SaveChangesAsync();

        return Ok();
    }

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
