using EmailSender.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using SchoolSystem.Data;
using SchoolSystem.Migrations;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SchoolSystem.Controllers
{
	[Authorize]
	public class BlogController : Controller
	{
		private readonly AppDbContext _context;
		private readonly UserManager<AppUser> _userManager;
		private readonly IEmailService _emailService;
		public BlogController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
		{
			_context = context;
			_userManager = userManager;
			_emailService = emailService;
		}

        public async Task<IActionResult> Index()
        {
            var blogs = await _context.Blogs
                .Include(b => b.User)
                .Include(b => b.Ratings)
                .ToListAsync();

            return View(blogs);
        }


        public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Create(BlogVM model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				//return Unauthorized();
			      return Forbid();

			// Check if the file is an image
			if(model.Image != null) { 
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
			var fileExtension = Path.GetExtension(model.Image.FileName).ToLower();
			if (!allowedExtensions.Contains(fileExtension))
			{
				ModelState.AddModelError("Image", "Only image files are allowed.");
				return View(model);
			}
			}
			string? imagePath = null;

			// Handle image upload
			if (model.Image != null)
			{
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/BlogUploads");
				Directory.CreateDirectory(uploadsFolder);

				var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await model.Image.CopyToAsync(fileStream);
				}

				imagePath = $"/BlogUploads/{uniqueFileName}"; 
			}

			var blog = new SchoolSystem.Models.Blog
			{
				Title = model.Title,
				Content = model.Content,
				UserId = user.Id,
				TimeStamp = DateTime.UtcNow,
				Image = imagePath
			};

			_context.Blogs.Add(blog);
			var result = await _context.SaveChangesAsync();
			if (result > 0) { 
			// Send email
			var subject = "Your blog has been successfully created.";
			var body = $@"
            <h1>Hello {user.Name}</h1>
            <p>Your blog has been successfully created.</p>
            <p><strong>Title:</strong> {blog.Title}</p>
        ";
			await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);
			}

			return RedirectToAction("Index");
		}

		public async Task<IActionResult> ListBlogs()
		{
			var blogs = await _context.Blogs.Include(b => b.User).Include(b => b.Ratings).ToListAsync();
			return View(blogs);
		}

		public async Task<IActionResult> Details(int id)
		{
			var blog = await _context.Blogs
				.Include(b => b.User)
				.Include(b => b.Ratings)
				.Include(b => b.Comments)
				.ThenInclude(c => c.User)
				.FirstOrDefaultAsync(b => b.Id == id);

			if (blog == null)
				return NotFound();

			return View(blog);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
			if (blog == null)
			{
				return NotFound();
			}

			var userId = _userManager.GetUserId(User);
			var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

		
			if (userId != blog.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			var model = new BlogVM
			{
				Id = blog.Id,
				Title = blog.Title,
				Content = blog.Content,
				ExistingImage = blog.Image 
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(BlogVM model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			// Check if the file is an image
			if (model.Image != null)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
				var fileExtension = Path.GetExtension(model.Image.FileName).ToLower();
				if (!allowedExtensions.Contains(fileExtension))
				{
					ModelState.AddModelError("Image", "Only image files are allowed.");
					return View(model);
				}
			}

			var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == model.Id);
			if (blog == null)
			{
				return NotFound();
			}

			var userId = _userManager.GetUserId(User);
			var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

			// Kiểm tra quyền chỉnh sửa
			if (userId != blog.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			string? imagePath = blog.Image;
			if (model.Image != null)
			{
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/BlogUploads");
				Directory.CreateDirectory(uploadsFolder);

				var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await model.Image.CopyToAsync(fileStream);
				}

				if (!string.IsNullOrEmpty(blog.Image))
				{
					var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", blog.Image.TrimStart('/'));
					if (System.IO.File.Exists(oldImagePath))
					{
						System.IO.File.Delete(oldImagePath);
					}
				}

				imagePath = $"/BlogUploads/{uniqueFileName}";
			}

			// Cập nhật thông tin blog
			blog.Title = model.Title;
			blog.Content = model.Content;
			blog.Image = imagePath;
			blog.TimeStamp = DateTime.UtcNow;

			_context.Blogs.Update(blog);
			var result = await _context.SaveChangesAsync();
			if (result > 0) { 
			var blogOwner = await _context.Users.FirstOrDefaultAsync(u => u.Id == blog.UserId);
				if (blogOwner != null)
				{

					// Send Email
					var subject = "Your blog has been successfully updated.";
					var body = $@"
               		<h1>Hello {blogOwner.Name}</h1>
               		<p>Your blog has been successfully updated.</p>
               		<p><strong>Title:</strong> {blog.Title}</p>
            ";
					await _emailService.SendEmailsAsync(new List<string> { blogOwner.Email }, subject, body);

				}
			}
			return RedirectToAction("Index");
		}


		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var blog = await _context.Blogs
				.Include(b => b.Comments)
				.Include(b => b.Ratings)
				.FirstOrDefaultAsync(b => b.Id == id);

			if (blog == null)
			{
				return NotFound();
			}

			var user = await _userManager.GetUserAsync(User);
			bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

			if (blog.UserId != user.Id && !isAdmin)
			{
				return Forbid();
			}

			_context.BlogComments.RemoveRange(blog.Comments);
			_context.BlogRatings.RemoveRange(blog.Ratings);
			_context.Blogs.Remove(blog);

			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var blogOwner = await _context.Users.FirstOrDefaultAsync(u => u.Id == blog.UserId);
				if (blogOwner != null)
				{

					// Send Email
					var subject = "Your blog has been successfully deleted.";
					var body = $@"
               		<h1>Hello {blogOwner.Name}</h1>
               		<p>Your blog has been successfully deleted.</p>
               		<p><strong>Title:</strong> {blog.Title}</p>
            ";
					await _emailService.SendEmailsAsync(new List<string> { blogOwner.Email }, subject, body);

				}
			}

			TempData["SuccessMessage"] = "Blog deleted successfully!";
			return RedirectToAction("Index");
		}

		[HttpPost]
		public async Task<IActionResult> RateBlog(int blogId, int rating)
		{
			if (rating < 1 || rating > 5)
			{
				TempData["ErrorMessage"] = "Invalid rating. Please select between 1 and 5.";
				return RedirectToAction("Details", new { id = blogId });
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized();

			var existingRating = await _context.BlogRatings
				.FirstOrDefaultAsync(r => r.BlogId == blogId && r.UserId == user.Id);

			if (existingRating != null)
			{
				existingRating.Rating = rating;
			}
			else
			{
				var newRating = new BlogRating
				{
					BlogId = blogId,
					UserId = user.Id,
					Rating = rating
				};
				_context.BlogRatings.Add(newRating);
			}

			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var blog = await _context.Blogs.FirstOrDefaultAsync(u => u.Id == blogId);
				if (blog != null)
				{

					// Send Email
					var subject = "Rating blog successfully.";
					var body = $@"
               		<h1>Hello {user.Name}</h1>
               		<p>Rating blog successfully.</p>
               		<p><strong>Blog Title:</strong> {blog.Title}</p>
					<p><strong>Rating: </strong> {rating}</p>
            ";
					await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);

				}
			}
			return RedirectToAction("Details", new { id = blogId });
		}

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> AddComment(CommentVM model)
		{
			if (!ModelState.IsValid)
			{
				TempData["ErrorMessage"] = "Comment cannot be empty.";
				return RedirectToAction("Details", new { id = model.BlogId });
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized();

			var comment = new BlogComment
			{
				Content = model.Content,
				BlogId = model.BlogId,
				UserId = user.Id,
				ParentCommentId = model.ParentCommentId,
				TimeStamp = DateTime.UtcNow
			};

			_context.BlogComments.Add(comment);
			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var blog = await _context.Blogs.FirstOrDefaultAsync(u => u.Id == model.BlogId);
				if(blog != null) { 
				// Send email
				var subject = "Comment blog successfully";
				var body = $@"
            <h1>Hello {user.Name}</h1>
            <p>Comment blog successfully.</p>
            <p><strong>Blog title:</strong> {blog.Title}</p>
			<p><strong>Comment:</strong> {model.Content}</p>
			<p><strong>Comment time:</strong> {comment.TimeStamp:dd/MM/yyyy HH:mm}</p>
        ";
				await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);
			}
			}
			return RedirectToAction("Details", new { id = model.BlogId });
		}

		[HttpPost]
		public async Task<IActionResult> DeleteComment(int CommentId)
		{
			var comment = await _context.BlogComments.Include(c => c.Blog).FirstOrDefaultAsync(c => c.Id == CommentId);

			if (comment == null)
			{
				return NotFound();
			}

			//var userId = _userManager.GetUserId(User);
			var user = await _userManager.GetUserAsync(User);
			var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

			if (user.Id != comment.UserId && user.Id != comment.Blog.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			await DeleteCommentRecursively(comment.Id);

			_context.BlogComments.Remove(comment);
			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var blog = await _context.Blogs.FirstOrDefaultAsync(u => u.Id == comment.BlogId);
				if (blog != null)
				{
					// Send email
					var subject = "Delete comment in blog successfully";
					var body = $@"
           			<h1>Hello {user.Name}</h1>
           			<p>Delete comment in blog successfully.</p>
           			<p><strong>Blog title:</strong> {blog.Title}</p>
					<p><strong>Comment:</strong> {comment.Content}</p>
					<p><strong>Comment time:</strong> {comment.TimeStamp:dd/MM/yyyy HH:mm}</p>
        ";
					await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);
				}
			}

			return RedirectToAction("Details", new { id = comment.BlogId });
		}

		private async Task DeleteCommentRecursively(int commentId)
		{
			var replies = await _context.BlogComments.Where(c => c.ParentCommentId == commentId).ToListAsync();

			foreach (var reply in replies)
			{
				await DeleteCommentRecursively(reply.Id);
				_context.BlogComments.Remove(reply);
			}

			await _context.SaveChangesAsync();
		}

	}
}

