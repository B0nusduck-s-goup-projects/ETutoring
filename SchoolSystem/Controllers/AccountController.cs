using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace SchoolSystem.Controllers
{
    public class AccountController : Controller
    {
		private readonly SignInManager<AppUser> signInManager;
		private readonly UserManager<AppUser> userManager;

		public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
		{
			this.signInManager = signInManager;
			this.userManager = userManager;
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
	}
}
