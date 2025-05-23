using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Security.Claims;
using EmailSender.Services;

namespace SchoolSystem.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
		private readonly EmailService _emailService;
		public DocumentController(
            AppDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            UserManager<AppUser> userManager, EmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
			_emailService = emailService;
		}

        public async Task<IActionResult> Index()
        {
            var documents = await _context.Documents
                .Include(d => d.User)
                .ToListAsync();

            var documentVMs = new List<DocumentIndexVM>();
            
            foreach (var doc in documents)
            {
                var userRoles = await _userManager.GetRolesAsync(doc.User);
                documentVMs.Add(new DocumentIndexVM
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Description = doc.Description,
                    UploadDate = doc.UploadDate,
                    FileType = doc.FileType,
                    FileSize = doc.FileSize,
                    UserName = doc.User.UserName,
                    UserRole = userRoles.FirstOrDefault() ?? "Unknown"
                });
            }

            return View(documentVMs);
        }

        // GET: Documents/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var document = await _context.Documents
			 .Include(d => d.User)
			.Include(d => d.Comments)
			.ThenInclude(c => c.User)
			.FirstOrDefaultAsync(m => m.Id == id);

			if (document == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserRole = (await _userManager.GetRolesAsync(currentUser)).FirstOrDefault();
        var documentUserRole = (await _userManager.GetRolesAsync(document.User)).FirstOrDefault();

        var viewModel = new DocumentDetailsVM
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FilePath = document.FilePath,
            UploadDate = document.UploadDate,
            FileType = document.FileType,
            FileSize = document.FileSize,
            UserName = document.User.UserName,
            UserId = document.UserId,
            CanEdit = document.UserId == currentUser.Id,
			Comments = document.Comments.ToList()
		};

        return View(viewModel);
    }

        // GET: Documents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentCreateVM model)
        {
            // Validate file size (10MB max)
            const int maxFileSize = 10 * 1024 * 1024; // 10MB in bytes
            if (model.File.Length > maxFileSize)
            {
                ModelState.AddModelError("File", "File size cannot exceed 10MB");
                return View(model);
            }
            // Validate file type
            var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx" }; // Đặt giới hạn các đuôi file
            var fileExtension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(fileExtension)) // Nếu như không đúng loại file thì báo lỗi và trả về view
            {
                ModelState.AddModelError("File", "Invalid file type. Allowed types: PDF, DOC, DOCX, TXT, XLS, XLSX");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError("", "User not authenticated");
                    return View(model);
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found");
                    return View(model);
                }
                
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }

                var document = new Document
                {
                    Title = model.Title,
                    Description = model.Description,
                    FilePath = "/uploads/documents/" + uniqueFileName,
                    UserId = userId,    // Now we know userId is not null
                    FileType = Path.GetExtension(model.File.FileName),
                    FileSize = model.File.Length,
                    User = user
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Documents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            // Only document owner can edit
            if (document.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var viewModel = new DocumentEditVM
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                ExistingFilePath = document.FilePath
            };

            return View(viewModel);
        }

        // POST: Documents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentEditVM model)
        {
            if (id != model.Id) return NotFound();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            if (document.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    document.Title = model.Title;
                    document.Description = model.Description;

                    if (model.NewFile != null)
                    {
                        // Validate file size
                        const int maxFileSize = 10 * 1024 * 1024;
                        if (model.NewFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError("NewFile", "File size cannot exceed 10MB");
                            return View(model);
                        }

                        // Validate file type
                        var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx" };
                        var fileExtension = Path.GetExtension(model.NewFile.FileName).ToLowerInvariant();
                        if (!allowedTypes.Contains(fileExtension))
                        {
                            ModelState.AddModelError("NewFile", "Invalid file type. Allowed types: PDF, DOC, DOCX, TXT, XLS, XLSX");
                            return View(model);
                        }

                        // Handle file upload
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.NewFile.FileName;
                        string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await model.NewFile.CopyToAsync(fileStream);
                        }

                        document.FilePath = "/uploads/documents/" + uniqueFileName;
                        document.FileType = Path.GetExtension(model.NewFile.FileName);
                        document.FileSize = model.NewFile.Length;
                    }

                    _context.Update(document);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(id))
                        return NotFound();
                    throw;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the changes.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var documentUserRoles = await _userManager.GetRolesAsync(document.User);

            // Allow if user owns the document or if user is a tutor and document owner is a student
            bool canDelete = document.UserId == currentUser.Id || 
                            (currentUserRoles.Contains("Tutor") && documentUserRoles.Contains("Student"));

            if (!canDelete)
                return Forbid();

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var documentUserRoles = await _userManager.GetRolesAsync(document.User);

            // Allow if user owns the document or if user is a tutor and document owner is a student
            bool canDelete = document.UserId == currentUser.Id || 
                            (currentUserRoles.Contains("Tutor") && documentUserRoles.Contains("Student"));

            if (!canDelete)
                return Forbid();

            try
            {
                // Delete the physical file
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Remove from database
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while deleting the document.");
                return RedirectToAction(nameof(Index));
            }
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }

		//CommentHead
		[HttpPost]
		public async Task<IActionResult> AddComment(DocumentCommentVM model)
		{
			if (!ModelState.IsValid)
			{
				TempData["ErrorMessage"] = "Comment cannot be empty.";
				return RedirectToAction("Details", new { id = model.DocumentId });
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized();

			var comment = new DocumentComment
			{
				Content = model.Content,
				DocumentId = model.DocumentId,
				UserId = user.Id,
				ParentCommentId = model.ParentCommentId,
				TimeStamp = DateTime.UtcNow
			};

			_context.DocumentComments.Add(comment);
			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var doc = await _context.Documents.FirstOrDefaultAsync(u => u.Id == model.DocumentId);
				if (doc != null)
				{
					// Send email
					var subject = "Comment document successfully";
					var body = $@"
                    <h1>Hello {user.Name}</h1>
                    <p>Comment document successfully.</p>
                    <p><strong>Document title:</strong> {doc.Title}</p>
			        <p><strong>Comment:</strong> {model.Content}</p>
			        <p><strong>Comment time:</strong> {comment.TimeStamp:dd/MM/yyyy HH:mm}</p>
        ";
					await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);
				}
			}

			return RedirectToAction("Details", new { id = model.DocumentId });
		}

		[HttpPost]
		public async Task<IActionResult> DeleteComment(int CommentId)
		{
			var comment = await _context.DocumentComments.Include(c => c.Document).FirstOrDefaultAsync(c => c.Id == CommentId);

			if (comment == null)
			{
				return NotFound();
			}

			var user = await _userManager.GetUserAsync(User);
			var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

			if (user.Id != comment.UserId && user.Id != comment.Document.UserId && !userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
			{
				return Forbid();
			}

			await DeleteCommentRecursively(comment.Id);

			_context.DocumentComments.Remove(comment);
			var result = await _context.SaveChangesAsync();
			if (result > 0)
			{
				var doc = await _context.Documents.FirstOrDefaultAsync(u => u.Id == comment.DocumentId);
				if (doc != null)
				{
					// Send email
					var subject = "Delete comment in document successfully";
					var body = $@"
           			<h1>Hello {user.Name}</h1>
           			<p>Delete comment in document successfully.</p>
           			<p><strong>Document title:</strong> {doc.Title}</p>
					<p><strong>Comment:</strong> {comment.Content}</p>
					<p><strong>Comment time:</strong> {comment.TimeStamp:dd/MM/yyyy HH:mm}</p>
        ";
					await _emailService.SendEmailsAsync(new List<string> { user.Email }, subject, body);
				}
			}

			return RedirectToAction("Details", new { id = comment.DocumentId });
		}

		private async Task DeleteCommentRecursively(int commentId)
		{
			var replies = await _context.DocumentComments.Where(c => c.ParentCommentId == commentId).ToListAsync();

			foreach (var reply in replies)
			{
				await DeleteCommentRecursively(reply.Id);
				_context.DocumentComments.Remove(reply);
			}

			await _context.SaveChangesAsync();
		}
		//CommentEnd
	}
}