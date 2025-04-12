using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.IdentityModel.Tokens;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;

namespace SchoolSystem.Controllers
{
    public class GroupsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public GroupsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        private async Task<IList<string>> GetUserRole(AppUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        private async Task<bool> UserHasRole(AppUser user, string role)
        {
            return (await GetUserRole(user)).Contains(role);
        }

        private async Task<GroupIndexVM> AssignUserValue(Group group)
        {
            GroupIndexVM entry = new GroupIndexVM();
            foreach (AppUser user in group.User)
            {
                if (await UserHasRole(user, "Student")
                    && entry.Student == null)
                {
                    entry.Student = user;
                }
                if (await UserHasRole(user, "Tutor")
                    && entry.Teacher == null)
                {
                    entry.Teacher = user;
                }
                else
                {
                    continue;
                }
            }
            entry.Group = group;
            return entry;
        }

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            List<Group> groups = await _context.Groups.Include(g=>g.User).ToListAsync();
            if (groups.IsNullOrEmpty())
            {
                return View();
            }
            List<GroupIndexVM> index = new List<GroupIndexVM>();
            foreach (Group group in groups)
            {
                GroupIndexVM entry = await AssignUserValue(group);
                index.Add(entry);
            }
            return View(index);
        }

        // GET: Groups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Group? group = await _context.Groups.Include(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }
            
            GroupIndexVM groupDetail = await AssignUserValue(group);
            return View(groupDetail);
        }

        private async Task<GroupCreateVM> GetGroupCreateVM()
        {
            GroupCreateVM selection = new GroupCreateVM();
            //get student list
            IList<AppUser> users = await _userManager.GetUsersInRoleAsync("Student");
            //include group into the student list for filtering
            List<AppUser> students = _context.Users.Where(s => users.Contains(s)).Include(s => s.GroupUsers).ThenInclude(gu => gu.Group).ToList();
            //remove all student who already has a valid group and prioritise student without group
            //then student with long expired group, due to the lack of atribute indicating the student
            //has quit or graduated, this list will become longer overtime
            students = students.Where(s => s.Group.IsNullOrEmpty() || s.Group!.All(g => g.IsValid == false))
                                .OrderBy(s => s.Group.IsNullOrEmpty())
                                .ThenBy(s => (DateTime)(s.Group!.Select(g => g.ExpiredTime).OrderBy(dt => (DateTime)dt!).FirstOrDefault())!)
                                .ToList();
                                        
            //get a list of tutor
            List<AppUser> tutors = (await _userManager.GetUsersInRoleAsync("Tutor")).ToList();
            selection.Students = students;
            selection.Teachers = tutors;
            return selection;
        }
        
        // GET: Groups/Create
        public async Task<IActionResult> Create()
        {   
            return View(await GetGroupCreateVM());
        }

        // POST: Groups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(String teacherID, List<String>studentID)
        {
            GroupCreateVM createVM = await GetGroupCreateVM();
            createVM.exception = new List<string>();
            if (ModelState.IsValid)
            {
                AppUser? teacher = await _context.Users.FindAsync(teacherID);
                List<AppUser>? students = _context.Users.Where(u => studentID.Contains(u.Id)).ToList();
                
                if (teacher == null || !await UserHasRole(teacher!, "Tutor"))
                {
                    createVM.exception?.Add("Tutor with this id don't exist");
                    return View(createVM);
                }
                List<Group> validGroup = new List<Group>();
                foreach (AppUser student in students)
                {
                    if (!await UserHasRole(student!, "Student"))
                    {
                        createVM.exception?.Add(student.Name + " is not a valid student");
                    }
                    if (_context.Groups.Where(s => s.User.Contains(student!) && s.IsValid).Any())
                    {
                        createVM.exception?.Add(student.Name + " has already been in an active group");
                    }
                    else
                    {
                        Group group = new Group();
                        group.CreatedTime = DateTime.Now;
                        group.User = new List<AppUser>() { teacher!, student! };
                        group.IsValid = true;
                        validGroup.Add(group);
                        
                    }
                }
                if (!validGroup.IsNullOrEmpty())
                {
                    foreach (Group group in validGroup)
                    {
                        await _context.Groups.AddAsync(group);
                        await _context.SaveChangesAsync();
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            if (createVM.exception.IsNullOrEmpty())
            {
                createVM.exception?.Add("Invalid model state");
            }
            return View(createVM);
        }

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Group? group = await _context.Groups.Include(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }
            GroupIndexVM entry = await AssignUserValue(group);
            return View(entry);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            Group? group = await _context.Groups.Include(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }
            try
            {
                if (group!.ExpiredTime == null)
                {
                    group.ExpiredTime = DateTime.Now;
                    group.IsValid = false;
                }
                //if time since expire is <= 1 day
                else if ((((DateTime)group.ExpiredTime).Ticks - DateTime.Now.Ticks) <= 864000000000)
                {
                    group.ExpiredTime = null;
                    group.IsValid = true;
                }
                else
                {
                    return View(group);
                }
                _context.Update(group);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GroupExists(group!.Id))
                {
                    return NotFound();
                }
            }
            GroupIndexVM entry = await AssignUserValue(group);
            return View(entry);
        }

        // GET: Groups/Delete/5
        /*public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Group? group = await _context.Groups.Include(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }
            GroupIndexVM entry = await AssignUserValue(group);
            return View(entry);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }*/

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.Id == id);
        }
    }
}
