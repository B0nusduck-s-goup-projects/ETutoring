using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using OfficeOpenXml;
using System.IO;
using EmailSender.Services;
namespace SchoolSystem.Controllers
{
	
    public class AccountController : Controller
    {
		private readonly SignInManager<AppUser> signInManager;
		private readonly UserManager<AppUser> userManager;
		private readonly RoleManager<IdentityRole> roleManager;
		private readonly EmailService emailService;

		public AccountController(SignInManager<AppUser> signInManager,
			UserManager<AppUser> userManager,
			RoleManager<IdentityRole> roleManager, EmailService emailService
			)
		{			
			this.signInManager = signInManager;
			this.userManager = userManager;
			this.roleManager = roleManager;
			this.emailService = emailService;
		}
		public IActionResult AccessDenied()
		{
			return View();
		}

		[Authorize]
		public async Task<IActionResult> RedirectByRole()
		{
			var user = await userManager.GetUserAsync(User);
			var roles = await userManager.GetRolesAsync(user);

			if (roles.Contains("Student") || roles.Contains("Tutor"))
			{
				return RedirectToAction("IndexUser", "Home");
			}
			else if (roles.Contains("Admin") || roles.Contains("Staff"))
			{
				return RedirectToAction("Index", "Home");
			}

			await signInManager.SignOutAsync();
			return RedirectToAction("Login", "Account");
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

						if (roles.Contains("Student") || roles.Contains("Tutor"))
						{
							return RedirectToAction("IndexUser", "Home");
						}
						else if (roles.Contains("Admin") || roles.Contains("Staff"))
						{
							return RedirectToAction("Index", "Home");
						}

						return RedirectToAction("Index", "Home");
					}
				}
				ModelState.AddModelError("", "Invalid login attempt");
			}
			return View(model);
		}


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


		[Authorize(Roles = "Admin")]
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

				if(model.Image != null) { 
				// Check if the file is an image
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
				var fileExtension = Path.GetExtension(model.Image.FileName).ToLower();
				if (!allowedExtensions.Contains(fileExtension))
				{
					ModelState.AddModelError("Image", "Only image files are allowed.");
					ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
					return View(model);
				}
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

					// Send Eamil
					var subject = "Account created successfully";
					var body = $@"
                    <h1>Welcome {user.Name}</h1>
                    <p>Your account has been successfully created.</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Mật khẩu:</strong> {model.Password}</p>

                ";
					await emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);



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

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> ListUsers(string? search, string? gender, string? role)
		{
			var currentUser = await userManager.GetUserAsync(User);
			var usersQuery = userManager.Users.Where(u => u.Id != currentUser.Id);
			//var usersQuery = userManager.Users.AsQueryable(); 

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
		[Authorize(Roles = "Admin")]
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

		//[HttpPost]
		//public async Task<IActionResult> UpdateUser(UpdateUserVM model)
		//{
		//	if (!ModelState.IsValid)
		//	{
		//		ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
		//		return View(model);
		//	}

		//	var user = await userManager.FindByIdAsync(model.Id);
		//	if (user == null)
		//	{
		//		return NotFound();
		//	}
			
		//	// Kiểm tra trùng email trước khi cập nhật
		//	var existingUserByEmail = await userManager.FindByEmailAsync(model.Email);
		//	if (existingUserByEmail != null && existingUserByEmail.Id != model.Id)
		//	{
		//		ModelState.AddModelError("Email", "Email already exists");
		//		ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();  
		//		return View(model);
		//	}
			

		//	// Check if the file is an image
		//	var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
		//	var fileExtension = Path.GetExtension(model.Image.FileName).ToLower();
		//	if (!allowedExtensions.Contains(fileExtension))
		//	{
		//		ModelState.AddModelError("Image", "Only image files are allowed.");
		//		ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
		//		return View(model);
		//	}


		//	// Cập nhật thông tin người dùng
		//	user.Name = model.Name;
		//	user.Code = model.Code;
		//	user.Email = model.Email;
		//	user.UserName = model.Email;
		//	user.Address = model.Address;
		//	user.Gender = model.Gender;

		//	// Handle image update
		//	if (model.Image != null)
		//	{
		//		var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
		//		Directory.CreateDirectory(uploadsFolder);

		//		var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
		//		var filePath = Path.Combine(uploadsFolder, uniqueFileName);
		//		using (var fileStream = new FileStream(filePath, FileMode.Create))
		//		{
		//			await model.Image.CopyToAsync(fileStream);
		//		}

		//		user.Image = $"/uploads/{uniqueFileName}";
		//	}

		//	var result = await userManager.UpdateAsync(user);
		//	if (!result.Succeeded)
		//	{
		//		ModelState.AddModelError("", "Failed to update user");
		//		return View(model);
		//	}

		//	// Update user role if changed
		//	var userRoles = await userManager.GetRolesAsync(user);
		//	if (userRoles.Any())
		//	{
		//		await userManager.RemoveFromRolesAsync(user, userRoles);
		//	}
		//	if (!string.IsNullOrEmpty(model.Role) && await roleManager.RoleExistsAsync(model.Role))
		//	{
		//		await userManager.AddToRoleAsync(user, model.Role);
		//	}

		//	TempData["SuccessMessage"] = "User updated successfully!";
		//	return RedirectToAction("ListUsers");
		//}

		//Udate User has try/cactch
		[HttpPost]
		public async Task<IActionResult> UpdateUser(UpdateUserVM model)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
				return View(model);
			}
			// Check if the file is an image
			if(model.Image != null) { 
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
			var fileExtension = Path.GetExtension(model.Image.FileName).ToLower();
			if (!allowedExtensions.Contains(fileExtension))
			{
				ModelState.AddModelError("Image", "Only image files are allowed.");
				ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();
				return View(model);
			}
			}
			var user = await userManager.FindByIdAsync(model.Id);
			if (user == null)
			{
				return NotFound();
			}

			// Kiểm tra trùng email trước khi cập nhật
			var existingUserByEmail = await userManager.FindByEmailAsync(model.Email);
			if (existingUserByEmail != null && existingUserByEmail.Id != model.Id)
			{
				ModelState.AddModelError("Email", "Email already exists");
				ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();  // Đảm bảo không bị mất dữ liệu role
				return View(model);
			}

			// Cập nhật thông tin người dùng
			user.Name = model.Name;
			user.Code = model.Code;
			user.Email = model.Email;
			user.UserName = model.Email;
			user.Address = model.Address;
			user.Gender = model.Gender;

			// Xử lý hình ảnh nếu có
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

			try
			{
				var result = await userManager.UpdateAsync(user);
				if (!result.Succeeded)
				{
					ModelState.AddModelError("", "Failed to update user");
					ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();  // Đảm bảo không bị mất dữ liệu role
					return View(model);
				}

				// Cập nhật vai trò của người dùng nếu có thay đổi
				var userRoles = await userManager.GetRolesAsync(user);
				if (userRoles.Any())
				{
					await userManager.RemoveFromRolesAsync(user, userRoles);
				}
				if (!string.IsNullOrEmpty(model.Role) && await roleManager.RoleExistsAsync(model.Role))
				{
					await userManager.AddToRoleAsync(user, model.Role);
				}

				// Send Eamil
				var subject = "Account updateted successfully";
				var body = $@"
                    <h1>Hello {user.Name}</h1>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p>Your account has been successfully updateted.</p>
                ";
				await emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);

				TempData["SuccessMessage"] = "User updated successfully!";
				return RedirectToAction("ListUsers");
			}
			catch (DbUpdateException ex) // Bắt lỗi DbUpdateException nếu có lỗi với database
			{
				//Kiểm tra lỗi liên quan đến trùng email
				if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_AspNetUsers_Email"))
				{
					ModelState.AddModelError("Email", "Email already exists");
				}
				if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_AspNetUsers_Code"))
				{
					ModelState.AddModelError("Code", "Code already exists");
				}
				else
				{
					ModelState.AddModelError("", "Failed to update user");
				}

				ViewBag.Roles = roleManager.Roles.Select(r => r.Name).ToList();  // Đảm bảo không bị mất dữ liệu role
				return View(model);
			}
		}
		//End update user try/catch

		[Authorize(Roles = "Admin")]
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
			// Send Eamil
			var subject = "Account deleted successfully";
			var body = $@"
                    <h1>Hello {user.Name}</h1>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p>Your account has been deleted.</p>
                ";
			await emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);

			TempData["SuccessMessage"] = "User deleted successfully!";
			return RedirectToAction("ListUsers");
		}
		//End



		[Authorize(Roles = "Admin")]
		public IActionResult Excel()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> ImportUsersFromExcel(IFormFile file)
		{
			if (file != null && file.Length > 0)
			{
				using (var package = new ExcelPackage(file.OpenReadStream()))
				{
					var worksheet = package.Workbook.Worksheets[0];
					var rowCount = worksheet.Dimension.Rows;

					for (int row = 2; row <= rowCount; row++) 
					{
						var name = worksheet.Cells[row, 1].Text;
						var email = worksheet.Cells[row, 2].Text;
						var code = worksheet.Cells[row, 3].Text;
						var gender = worksheet.Cells[row, 4].Text;
						var address = worksheet.Cells[row, 5].Text;
						var imagePath = worksheet.Cells[row, 6].Text; 
						var role = worksheet.Cells[row, 7].Text; 

						// Kiểm tra xem cột Image có trống không
						if (string.IsNullOrWhiteSpace(imagePath))
						{
							imagePath = null; 
						}

						// Kiểm tra xem email và code có bị trùng không
						var existingUserByEmail = await userManager.FindByEmailAsync(email);
						var existingUserByCode = await userManager.Users.FirstOrDefaultAsync(u => u.Code == code);

						if (existingUserByEmail != null)
						{
							// Nếu email đã tồn tại
							TempData["ErrorMessage"] = $"Email {email} already take!";
							return RedirectToAction("ListUsers");
						}

						if (existingUserByCode != null)
						{
							// Nếu code đã tồn tại
							TempData["ErrorMessage"] = $"Code {code} already take!";
							return RedirectToAction("ListUsers");
						}


						// Kiểm tra xem người dùng đã tồn tại chưa
						var existingUser = await userManager.FindByEmailAsync(email);
						if (existingUser == null)
						{
							var user = new AppUser
							{
								Name = name,
								Email = email,
								UserName = email, // Email là tên đăng nhập
								Code = code,
								Gender = gender,
								Address = address,
								Image = imagePath // Lưu đường dẫn hình ảnh vào trường Image của người dùng
							};

							var result = await userManager.CreateAsync(user, "default123"); // Cấp mật khẩu mặc định
							if (result.Succeeded)
							{
								// Gán Role cho người dùng nếu tồn tại
								if (!string.IsNullOrEmpty(role) && await roleManager.RoleExistsAsync(role))
								{
									await userManager.AddToRoleAsync(user, role);
								}

								// Send Eamil
								var subject = "Account created successfully";
								var body = $@"
                   				<h1>Welcome {user.Name}</h1>
                   				<p>Your account has been successfully created.</p>
                   				<p><strong>Email:</strong> {user.Email}</p>
                   				<p><strong>Mật khẩu:</strong> default123</p>

                ";
								await emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);

							}
							else
							{
								// Xử lý lỗi nếu có
								foreach (var error in result.Errors)
								{
									// Xử lý lỗi (ví dụ: lưu vào log, thông báo cho người dùng, v.v.)
									//ModelState.AddModelError(string.Empty, error.Description);
									TempData["Error"] = "Have Error!";
									return RedirectToAction("Excel");
								}
							}
						}
						
					}
				}
				return RedirectToAction("ListUsers");
			}

			TempData["FailMessage"] = "No file!";
			return RedirectToAction("Excel");
		}

		[HttpGet]
		public async Task<IActionResult> ExportUsersToExcel()
		{
			//var users = await userManager.Users.ToListAsync();
			var adminRole = "Admin";
			var allUsers = await userManager.Users.ToListAsync();
			var users = new List<AppUser>();

			foreach (var user in allUsers)
			{
				if (!await userManager.IsInRoleAsync(user, adminRole))
				{
					users.Add(user);
				}
			}

			if (users == null)
			{
				return RedirectToAction("Excel");
			}
			var stream = new MemoryStream();

			using (var package = new ExcelPackage(stream))
			{
				var worksheet = package.Workbook.Worksheets.Add("Users");

				// Thêm tiêu đề cột
				//worksheet.Cells[1, 1].Value = "ID";
				worksheet.Cells[1, 1].Value = "Name";
				worksheet.Cells[1, 2].Value = "Email";
				worksheet.Cells[1, 3].Value = "Code";
				worksheet.Cells[1, 4].Value = "Gender";
				worksheet.Cells[1, 5].Value = "Address";
				worksheet.Cells[1, 6].Value = "Image"; // Thêm cột Image
				worksheet.Cells[1, 7].Value = "Role"; // Thêm cột Role

				// Thêm dữ liệu người dùng vào các hàng tiếp theo
				for (int i = 0; i < users.Count; i++)
				{
					//worksheet.Cells[i + 2, 1].Value = users[i].Id;
					worksheet.Cells[i + 2, 1].Value = users[i].Name;
					worksheet.Cells[i + 2, 2].Value = users[i].Email;
					worksheet.Cells[i + 2, 3].Value = users[i].Code;
					worksheet.Cells[i + 2, 4].Value = users[i].Gender;
					worksheet.Cells[i + 2, 5].Value = users[i].Address;
					worksheet.Cells[i + 2, 6].Value = users[i].Image; // Xuất đường dẫn hình ảnh
					var roles = await userManager.GetRolesAsync(users[i]);
					worksheet.Cells[i + 2, 7].Value = string.Join(", ", roles); // Xuất các chức vụ (Role)
				}

				package.Save();
			}

			stream.Position = 0;
			string fileName = "Users.xlsx";
			return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
		}

		//UploadImage
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UploadImage(IFormFile image)
		{

			if (image != null && image.Length > 0 )
			{
				// Check if the file is an image
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
				var fileExtension = Path.GetExtension(image.FileName).ToLower();
				if (!allowedExtensions.Contains(fileExtension))
				{
					return RedirectToAction("ListUsers");
				}

				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
				Directory.CreateDirectory(uploadsFolder);

				var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await image.CopyToAsync(fileStream);
				}

				return Json(new { filePath = $"/uploads/{uniqueFileName}" });
			}

			return BadRequest("No image uploaded because it not an image or something else");
		}

		//Profile

		//[Authorize(Roles = "Student,Staff,Tutor")]
		[Authorize]
		public async Task<IActionResult> ViewProfile()
		{
			var user = await userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound();
			}

			var model = new ProfileVM
			{
				Id = user.Id,
				Name = user.Name,
				Code = user.Code,
				Email = user.Email,
				Address = user.Address,
				Gender = user.Gender,
				Image = user.Image
			};

			return View(model);
		}

		//ChangePassword
		//[Authorize(Roles = "Student,Staff,Tutor")]
		[Authorize]
		public IActionResult ChangePassword()
		{
			return View();
		}

		[HttpPost]
		//[Authorize(Roles = "Student,Staff,Tutor")]
		public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound();
			}

			var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
				return View(model);
			}
			// Send Eamil
			var subject = "Change password successfully";
			var body = $@"
                    <h1>Hello {user.Name}</h1>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p>Change password successfully.</p>
                ";
			await emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);

			TempData["SuccessMessage"] = "Password changed successfully!";
			return RedirectToAction("ViewProfile");
		}


		[HttpPost]
		[Authorize]
		public async Task<IActionResult> UpdateProfileImage(string userId, IFormFile ProfileImage)
		{
			if (ProfileImage != null)
			{
				// Check if the file is an image
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
				var fileExtension = Path.GetExtension(ProfileImage.FileName).ToLower();
				if (!allowedExtensions.Contains(fileExtension))
				{
					ModelState.AddModelError("ProfileImage", "Only image files are allowed.");
					return RedirectToAction("ViewProfile");
				}

				// Handle file upload
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
				Directory.CreateDirectory(uploadsFolder); // Ensure the folder exists

				var uniqueFileName = $"{Guid.NewGuid()}_{ProfileImage.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await ProfileImage.CopyToAsync(fileStream);
				}

				var imagePath = $"/uploads/{uniqueFileName}"; // Save relative path to DB

				// Update the profile image path in the database or model
				var user = await userManager.FindByIdAsync(userId);
				if (user != null)
				{
					user.Image = imagePath;
					await userManager.UpdateAsync(user);

					TempData["SuccessMessage"] = "Profile image updated successfully!";
				}
			}
			else
			{
				ModelState.AddModelError("ProfileImage", "Please select an image file.");
			}

			return RedirectToAction("ViewProfile");
		}

	}
}
