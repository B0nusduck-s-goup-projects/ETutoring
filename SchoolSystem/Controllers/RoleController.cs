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
    public class RoleController : Controller
    {
		private readonly RoleManager<IdentityRole> roleManager;
		private readonly UserManager<AppUser> userManager;

		public RoleController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			this.roleManager = roleManager;
			this.userManager = userManager;
		}
		public IActionResult Index()
        {
            return View();
        }

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
						TempData["Message"] = "Role created successfully!";
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

		//[HttpPost]
		//public async Task<IActionResult> DeleteRole(string roleId)
		//{
		//	if (string.IsNullOrEmpty(roleId))
		//	{
		//		return BadRequest("Role ID is required.");
		//	}

		//	var role = await roleManager.FindByIdAsync(roleId);
		//	if (role == null)
		//	{
		//		return NotFound("Role not found.");
		//	}

		//	var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);
		//	if (usersInRole.Any())
		//	{
		//		ModelState.AddModelError("", "Cannot delete role because users are assigned to it.");
		//		var roles = roleManager.Roles.ToList();
		//		return View("ListRoles", roles); // Return to ListRoles with the role list
		//	}

		//	var result = await roleManager.DeleteAsync(role);
		//	if (result.Succeeded)
		//	{
		//		return RedirectToAction("ListRoles"); // Redirect to ListRoles
		//	}

		//	foreach (var error in result.Errors)
		//	{
		//		ModelState.AddModelError("", error.Description);
		//	}

		//	var allRoles = roleManager.Roles.ToList();
		//	return View("ListRoles", allRoles); // Reload role list with errors
		//}

		[HttpPost]
		public async Task<IActionResult> DeleteRole(string roleId)
		{
			var role = await roleManager.FindByIdAsync(roleId);
			if (role == null)
			{
				TempData["Error"] = "Role not found.";
				return RedirectToAction("ListRoles");
			}

			var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);
			if (usersInRole.Any())
			{
				TempData["Error"] = "Cannot delete role because users are assigned to it.";
				return RedirectToAction("ListRoles");
			}

			var result = await roleManager.DeleteAsync(role);
			if (result.Succeeded)
			{
				TempData["Message"] = "Role deleted successfully!";
				return RedirectToAction("ListRoles");
			}

			TempData["Error"] = "Role deletion failed.";
			return RedirectToAction("ListRoles");
		}
	}
}
