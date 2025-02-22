using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolSystem.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly RoleManager<IdentityRole> roleManager;
		private readonly UserManager<AppUser> userManager;
		private readonly SignInManager<AppUser> signInManager;

		public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<AppUser> signInManager)
		{
			this.userManager = userManager;
			this.roleManager = roleManager;
			this.signInManager = signInManager;
		}
		//RoleHead
		public IActionResult CreateRole()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> CreateRole(RoleVM model)
		{
			if (ModelState.IsValid)
			{
				var roleExists = await roleManager.RoleExistsAsync(model.Name!);
				if (!roleExists)
				{
					var result = await roleManager.CreateAsync(new IdentityRole(model.Name!));
					if (result.Succeeded)
					{
						ViewBag.Message = "Role created successfully!";
						return RedirectToAction("ListRoles");
					}
					else
					{
						foreach (var error in result.Errors)
						{
							ModelState.AddModelError("", error.Description);
						}
					}
				}
				else
				{
					ModelState.AddModelError("", "Role already exists.");
				}
			}
			return View(model);
		}

		public IActionResult ListRoles()
		{
			var roles = roleManager.Roles.ToList();
			return View(roles);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteRole(string roleId)
		{
			if (string.IsNullOrEmpty(roleId))
			{
				return BadRequest("Role ID is required.");
			}

			var role = await roleManager.FindByIdAsync(roleId);
			if (role == null)
			{
				return NotFound("Role not found.");
			}

			var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);
			if (usersInRole.Any())
			{
				ModelState.AddModelError("", "Cannot delete role because users are assigned to it.");
				var roles = roleManager.Roles.ToList();
				return View("ListRoles", roles); // Return to ListRoles with the role list
			}

			var result = await roleManager.DeleteAsync(role);
			if (result.Succeeded)
			{
				return RedirectToAction("ListRoles"); // Redirect to ListRoles
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error.Description);
			}

			var allRoles = roleManager.Roles.ToList();
			return View("ListRoles", allRoles); // Reload role list with errors
		}
		//RoleEnd


		//AccountHead
		public IActionResult Register()
		{
			ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
			return View();
		}

		

		[HttpPost]
		public async Task<IActionResult> Register(RegisterVM model)
		{
			if (ModelState.IsValid)
			{
				// Check if Code is already taken
				var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.Code == model.Code);
				if (existingUser != null)
				{
					ModelState.AddModelError("Code", "This Code is already registered.");
					ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
					return View(model);
				}

				// Create new user
				var user = new AppUser
				{
					Name = model.Name,
					Code = model.Code, 
					UserName = model.Email, 
					Email = model.Email,
					Address = model.Address,
					Gender = model.Gender, 
					Image = model.Image
				};

				var result = await userManager.CreateAsync(user, model.Password!);

				if (result.Succeeded)
				{
					// If the role exists, assign it to the user
					if (!string.IsNullOrEmpty(model.Role) && await roleManager.RoleExistsAsync(model.Role))
					{
						await userManager.AddToRoleAsync(user, model.Role);
					}

					TempData["SuccessMessage"] = "User registered successfully!";
					return RedirectToAction("ListUsers"); // Redirect to user list
				}

				// Add errors to ModelState if registration fails
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}

			// Reload roles if there's an error
			ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
			return View(model);
		}


		public async Task<IActionResult> ListUsers()
		{
			var users = await userManager.Users.ToListAsync(); // Use async query
			var userRoles = new List<UserWithRolesVM>();

			foreach (var user in users)
			{
				var roles = await userManager.GetRolesAsync(user); // Get roles for each user
				userRoles.Add(new UserWithRolesVM
				{
					User = user,
					Roles = roles
				});
			}

			return View(userRoles); // Pass the list of users with roles to the view
		}


		//AccountEnd
	}
}

