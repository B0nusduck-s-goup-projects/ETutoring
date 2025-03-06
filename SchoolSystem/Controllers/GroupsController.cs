using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            List<Group> groups = await _context.Groups.ToListAsync();
            if (groups.IsNullOrEmpty())
            {
                return View();
            }
            List<GroupIndexVM> index = new List<GroupIndexVM>();
            foreach (Group group in groups)
            {
                GroupIndexVM entry = new GroupIndexVM();
                foreach (AppUser user in group.User)
                {
                    if (await UserHasRole(user,"Student")
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

            Group? group = await _context.Groups.FirstOrDefaultAsync(m => m.Id == id);
            if (group == null)
            {
                return NotFound();
            }
            
            GroupIndexVM groupDetail = new GroupIndexVM();
            groupDetail.Group = group;
            foreach(AppUser user in group.User)
            {
                if(await UserHasRole(user, "Student")
                    && groupDetail.Student == null)
                {
                    groupDetail.Student = user;
                }
                if (await UserHasRole(user, "Tutor")
                    && groupDetail.Teacher == null)
                {
                    groupDetail.Teacher = user;
                }
                else
                {
                    continue;
                }
            }
            return View(group);
        }

        // GET: Groups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Groups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeacherID, StudentID")] CreateGroupVM createVM)
        {
            createVM.exception = null;
            if (ModelState.IsValid)
            {
                AppUser? teacher = _context.Users.Where(u => u.Id == createVM.TeacherID).FirstOrDefault();
                AppUser? student = _context.Users.Where(u => u.Id == createVM.StudentID).FirstOrDefault();

                if (student == null || !await UserHasRole(student!, "Student"))
                {
                    createVM.exception?.Add("Student with this id don't exist");
                    return View(createVM);
                }
                if(_context.Groups.Where(s => s.User.Contains(student!) && s.IsValid).Any())
                {
                    createVM.exception?.Add("group already exist for this student");
                    return View(createVM);
                }
                if (teacher == null || !await UserHasRole(teacher!, "Tutor"))
                {
                    createVM.exception?.Add("Tutor with this id don't exist");
                    return View(createVM);
                }
                else
                { 
                    Group group = new Group();
                    group.CreatedTime = DateTime.Now;
                    group.User = new List<AppUser>(){ teacher!, student!};
                    await _context.Groups.AddAsync(group);
                    return RedirectToAction(nameof(Index));
                }
            }
            createVM.exception?.Add("Invalid model state");
            return View(createVM);
        }

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }
            return View(@group);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,IsValid,CreatedTime,ExpiredTime")] Group @group)
        {
            if (id != @group.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@group);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(@group.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @group = await _context.Groups.FindAsync(id);
            if (@group != null)
            {
                _context.Groups.Remove(@group);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.Id == id);
        }
    }
}
