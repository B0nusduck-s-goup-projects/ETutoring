using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SchoolSystem.Controllers
{
	[Authorize]
	public class BlogController : Controller
	{
		private readonly AppDbContext _context;
		private readonly UserManager<AppUser> _userManager;

		public BlogController(AppDbContext context, UserManager<AppUser> userManager)
		{
			_context = context;
			_userManager = userManager;
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

			var blog = new Blog
			{
				Title = model.Title,
				Content = model.Content,
				UserId = user.Id,
				TimeStamp = DateTime.UtcNow,
				Image = imagePath
			};

			_context.Blogs.Add(blog);
			await _context.SaveChangesAsync();

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

			// Kiểm tra quyền chỉnh sửa
			if (userId != blog.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			var model = new BlogVM
			{
				Id = blog.Id,
				Title = blog.Title,
				Content = blog.Content,
				ExistingImage = blog.Image // Lưu ảnh cũ
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
			await _context.SaveChangesAsync();

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
			await _context.SaveChangesAsync();

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

			await _context.SaveChangesAsync();
			return RedirectToAction("Details", new { id = blogId });
		}

		[HttpPost]
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
			await _context.SaveChangesAsync();

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

			var userId = _userManager.GetUserId(User);
			var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

			if (userId != comment.UserId && userId != comment.Blog.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			await DeleteCommentRecursively(comment.Id);

			_context.BlogComments.Remove(comment);
			await _context.SaveChangesAsync();

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

