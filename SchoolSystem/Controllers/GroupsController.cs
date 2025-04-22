using EmailSender.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.DependencyResolver;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

//clarify that all Group used are of SchoolSystem.Models not System.Text.RegularExpressions
using Group = SchoolSystem.Models.Group;

namespace SchoolSystem.Controllers
{
    [Authorize(Roles = "Staff")]
    public class GroupsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;
        private readonly string emailTemplatePath;

        public GroupsController(AppDbContext context, UserManager<AppUser> userManager, EmailService emailService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            emailTemplatePath = webHostEnvironment.ContentRootPath + @"\EmailTemplates";
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
            List<Group> groups = await _context.Groups.Include(g=>g.User).OrderByDescending(g => g.IsValid).ToListAsync();
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

            //get all student without a group or without a valid group
            students = students.Where(s => s.Group.IsNullOrEmpty() || s.Group!.All(g => g.IsValid == false)).ToList();

            //trigger if any student used to have a group
            if (students.Any(s => !s.Group.IsNullOrEmpty()))
            {
                //order student based on the time since their last group, prioritise new student.
                //split the data into two sub list one with group and one without
                List <AppUser> subList1 = students.Where(s => s.Group.IsNullOrEmpty()).ToList();
                List <AppUser> subList2 = students.Where(s => !s.Group.IsNullOrEmpty()).ToList();
                //odering list with group
                subList2 = subList2.OrderBy(s => (DateTime)(s.Group?.Select(g => g.ExpiredTime).OrderBy(dt => (DateTime)dt!).FirstOrDefault())!).ToList();
                //merge 2 list with list without group being first
                subList1.AddRange(subList2);
                students = subList1;
            }
                                        
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
                
                //check for teacher vailditi
                if (teacher == null || !await UserHasRole(teacher!, "Tutor"))
                {
                    createVM.exception?.Add("Tutor with this id don't exist");
                    return View(createVM);
                }
                List<Group> validGroup = new List<Group>();
                foreach (AppUser student in students)
                {
                    //check students for validity before adding to wait list
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
                    //get the template texts from the template files
                    string studentEmailPath = emailTemplatePath + @"\TutorAllocation_Student.cshtml";
                    string tutorEmailPath = emailTemplatePath + @"\TutorAllocation_Tutor.cshtml";
                    string studentEmail= System.IO.File.ReadAllText(studentEmailPath);
                    string tutorEmail = System.IO.File.ReadAllText(tutorEmailPath);

                    //replace teacher name into the template placeholder
                    studentEmail = new Regex(@"\[Tutor Name\]").Replace(studentEmail, teacher.Name);
                    tutorEmail = new Regex(@"\[Tutor Name\]").Replace(tutorEmail, teacher.Name);
                    
                    //assigned student lists
                    List<AppUser> registeredStudent = new List<AppUser>();
                    //add student from wait list to database
                    foreach (Group group in validGroup)
                    {
                        foreach(AppUser user in group.User)
                        {
                            if(await UserHasRole(user, "Student"))
                            {
                                registeredStudent.Add(user);
                                //fill student name into placeholder
                                string tempEmail = new Regex(@"\[Student Name\]").Replace(studentEmail, user.Name);
                                //sent email
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                _emailService.SendEmailsAsync([user.Email!.ToString()], "Tutor assignment", tempEmail);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            }
                        }
                        await _context.Groups.AddAsync(group);
                        await _context.SaveChangesAsync();
                    }

                    //compile student list
                    List<string> studentNames = registeredStudent.Select(st => st.Name).ToList();
                    string studentListText = "";
                    foreach (string name in studentNames)
                    {
                        studentListText += "<li>" + name + "</li>";
                    }

                    //replace place holder with student list
                    tutorEmail = new Regex(@"\[Student List\]").Replace(tutorEmail, studentListText);

                    //sent email
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    _emailService.SendEmailsAsync([teacher.Email!.ToString()], "Tutees assignment", tutorEmail);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
            //get group + user 
            Group? group = await _context.Groups.Include(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
            //check if group exist
            if (group == null)
            {
                return NotFound();
            }
            try
            {
                //get user to sent mail to
                AppUser teacher = new AppUser();
                AppUser student = new AppUser();
                foreach (AppUser user in group.User)
                {
                    if (await UserHasRole(user, "Tutor"))
                    {
                        teacher = user;
                    }
                    else
                    {
                        student = user;
                    }
                }

                //if group is still available
                if (group!.ExpiredTime == null)
                {
                    //set group to expired, and sent feedback
                    group.ExpiredTime = DateTime.Now;
                    group.IsValid = false;
                    _context.Update(group);
                    await _context.SaveChangesAsync();
                    ViewBag.Message = "group close successfully, you have 24 hours to undo this action if it is not intended";

                    //get the template texts from the template files
                    string studentEmailPath = emailTemplatePath + @"\TutorUn-assigned_Student.cshtml";
                    string tutorEmailPath = emailTemplatePath + @"\TutorUn-assigned_Tutor.cshtml";
                    string studentEmail = System.IO.File.ReadAllText(studentEmailPath);
                    string tutorEmail = System.IO.File.ReadAllText(tutorEmailPath);

                    //replace teacher name into the template placeholder
                    studentEmail = new Regex(@"\[Tutor Name\]").Replace(studentEmail, teacher.Name);
                    studentEmail = new Regex(@"\[Student Name\]").Replace(studentEmail, student.Name);

                    tutorEmail = new Regex(@"\[Tutor Name\]").Replace(tutorEmail, teacher.Name);
                    tutorEmail = new Regex(@"\[Student Name\]").Replace(tutorEmail, student.Name);

                    //sent notification email
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    _emailService.SendEmailsAsync([teacher.Email!.ToString()], "Tutor un-assignment", studentEmail);
                    _emailService.SendEmailsAsync([teacher.Email!.ToString()], "Tutees un-assignment", tutorEmail);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                //if time since expire is <= 1 day
                else if ((((DateTime)group.ExpiredTime).Ticks - DateTime.Now.Ticks) <= 864000000000)
                {
                    //check if other group already exist for this student
                    bool hasConflict = _context.Groups.Any(g => g.User.Contains(student) && g.IsValid == true);
                    if (!hasConflict)
                    {
                        //undo the exiration mark and sent feedback
                        group.ExpiredTime = null;
                        group.IsValid = true;
                        _context.Update(group);
                        await _context.SaveChangesAsync();
                        ViewBag.Message = "group re-opened successfully";

                        //get the template texts from the template files
                        string studentEmailPath = emailTemplatePath + @"\TutorRe-assigned_Student.cshtml";
                        string tutorEmailPath = emailTemplatePath + @"\TutorRe-assigned_Tutor.cshtml";
                        string studentEmail = System.IO.File.ReadAllText(studentEmailPath);
                        string tutorEmail = System.IO.File.ReadAllText(tutorEmailPath);

                        //replace teacher name into the template placeholder
                        studentEmail = new Regex(@"\[Tutor Name\]").Replace(studentEmail, teacher.Name);
                        studentEmail = new Regex(@"\[Student Name\]").Replace(studentEmail, student.Name);

                        tutorEmail = new Regex(@"\[Tutor Name\]").Replace(tutorEmail, teacher.Name);
                        tutorEmail = new Regex(@"\[Student Name\]").Replace(tutorEmail, student.Name);

                        //sent email
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        _emailService.SendEmailsAsync([teacher.Email!.ToString()], "Tutor re-assignment", studentEmail);
                        _emailService.SendEmailsAsync([teacher.Email!.ToString()], "Tutees re-assignment", tutorEmail);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                    else
                    {
                        ViewBag.Message = "student already been assigned to another group";
                    }
                }
                else
                {
                    ViewBag.Message = "the 24 hours period has ended you can no longer change this";
                }
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
