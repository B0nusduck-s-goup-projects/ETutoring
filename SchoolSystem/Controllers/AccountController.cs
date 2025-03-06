using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace SchoolSystem.Controllers
{
    public class AccountController : Controller
    {
		private readonly SignInManager<AppUser> signInManager;
		private readonly UserManager<AppUser> userManager;
		private readonly RoleManager<IdentityRole> roleManager;
		public AccountController(SignInManager<AppUser> signInManager,
			UserManager<AppUser> userManager,
			RoleManager<IdentityRole> roleManager
			)
		{			
			this.signInManager = signInManager;
			this.userManager = userManager;
			this.roleManager = roleManager;
		}
		public IActionResult AccessDenied()
		{
			return View();
		}

		public IActionResult Login()
        {
            return View();
        }

		[HttpPost]
		public async Task<IActionResult> Login(LoginVM model)
		{
			if (ModelState.IsValid)
			{
				var user = await userManager.FindByNameAsync(model.Username!);
				if (user != null)
				{
					var result = await signInManager.PasswordSignInAsync(user, model.Password!, model.RememberMe!, false);
					if (result.Succeeded)
					{
						// Get user's roles
						var roles = await userManager.GetRolesAsync(user);

						if (!roles.Any()) // Prevent login if no role assigned
						{
							await signInManager.SignOutAsync();
							ModelState.AddModelError("", "You do not have any assigned roles. Contact the admin.");
							return View(model);
						}

						// Create custom claims
						var claims = new List<Claim>
				{
					new Claim("FullName", user.Name)
				};

						foreach (var role in roles)
						{
							claims.Add(new Claim(ClaimTypes.Role, role));
						}

						// Sign in with claims
						await signInManager.SignOutAsync(); // Ensure clean authentication
						await signInManager.SignInWithClaimsAsync(user, model.RememberMe!, claims);

						return RedirectToAction("Index", "Home");
					}
				}
				ModelState.AddModelError("", "Invalid login attempt");
			}
			return View(model);
		}


		//OriginalStart
		//[HttpPost]
		//public async Task<IActionResult> Login(LoginVM model)
		//{
		//	if (ModelState.IsValid)
		//	{
		//		var result = await signInManager.PasswordSignInAsync(model.Username!, model.Password!, model.RememberMe!, false);
		//		if (result.Succeeded)
		//		{
		//			return RedirectToAction("Index", "Home");
		//		}
		//		ModelState.AddModelError("", "Invalid login attempt");
		//		return View(model);
		//	}
		//	return View(model);
		//}
		//OriginalEnd



		public async Task<IActionResult> Logout()
		{
			await signInManager.SignOutAsync();
			return RedirectToAction("Index","Home");
		}
		[Authorize(Roles = "Admin")]
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
				// Handle file upload
				string? imagePath = null;
				if (model.Image != null)
				{
					var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
					Directory.CreateDirectory(uploadsFolder); // Ensure the folder exists

					var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
					var filePath = Path.Combine(uploadsFolder, uniqueFileName);

					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						await model.Image.CopyToAsync(fileStream);
					}

					imagePath = $"/uploads/{uniqueFileName}"; // Save relative path to DB
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
					Image = imagePath
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


		public async Task<IActionResult> ListUsers(string? search, string? gender, string? role)
		{
			var usersQuery = userManager.Users.AsQueryable(); // Start with the base query

			// Filter by search (Code or Name)
			if (!string.IsNullOrEmpty(search))
			{
				usersQuery = usersQuery.Where(u => u.Code.Contains(search) || u.Name.Contains(search));
			}

			// Filter by Gender
			if (!string.IsNullOrEmpty(gender))
			{
				usersQuery = usersQuery.Where(u => u.Gender == gender);
			}

			var users = await usersQuery.ToListAsync();
			var userRoles = new List<UserWithRolesVM>();

			foreach (var user in users)
			{
				var roles = await userManager.GetRolesAsync(user); // Get roles for each user
																  
				if (string.IsNullOrEmpty(role) || roles.Contains(role))  // Filter by Role
				{
					userRoles.Add(new UserWithRolesVM
					{
						User = user,
						Roles = roles
					});
				}
			}
			// Populate dropdown filters
			ViewBag.Genders = new List<string> { "Male", "Female" };
			ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();

			return View(userRoles); // Pass the list of users with roles to the view
		}
		//Update and delete head
		[HttpGet]
		public async Task<IActionResult> UpdateUser(string id)
		{
			var user = await userManager.FindByIdAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			var model = new UpdateUserVM
			{
				Id = user.Id,
				Name = user.Name,
				Code = user.Code,
				Email = user.Email,
				Address = user.Address,
				Gender = user.Gender,
				ExistingImage = user.Image,
				Role = (await userManager.GetRolesAsync(user)).FirstOrDefault()
			};

			ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateUser(UpdateUserVM model)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
				return View(model);
			}

			var user = await userManager.FindByIdAsync(model.Id);
			if (user == null)
			{
				return NotFound();
			}

			// Update user properties
			user.Name = model.Name;
			user.Code = model.Code;
			user.Email = model.Email;
			user.UserName = model.Email;
			user.Address = model.Address;
			user.Gender = model.Gender;

			// Handle image update
			if (model.Image != null)
			{
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
				Directory.CreateDirectory(uploadsFolder);

				var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);
				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await model.Image.CopyToAsync(fileStream);
				}

				user.Image = $"/uploads/{uniqueFileName}";
			}

			var result = await userManager.UpdateAsync(user);
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Failed to update user");
				return View(model);
			}

			// Update user role if changed
			var userRoles = await userManager.GetRolesAsync(user);
			if (userRoles.Any())
			{
				await userManager.RemoveFromRolesAsync(user, userRoles);
			}
			if (!string.IsNullOrEmpty(model.Role) && await roleManager.RoleExistsAsync(model.Role))
			{
				await userManager.AddToRoleAsync(user, model.Role);
			}

			TempData["SuccessMessage"] = "User updated successfully!";
			return RedirectToAction("ListUsers");
		}

		[HttpPost]
		public async Task<IActionResult> DeleteUser(string id)
		{
			var user = await userManager.FindByIdAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			var result = await userManager.DeleteAsync(user);
			if (!result.Succeeded)
			{
				TempData["ErrorMessage"] = "Failed to delete user.";
				return RedirectToAction("ListUsers");
			}

			TempData["SuccessMessage"] = "User deleted successfully!";
			return RedirectToAction("ListUsers");
		}
		//End
	}
}
