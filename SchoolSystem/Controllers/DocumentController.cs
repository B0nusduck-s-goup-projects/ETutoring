using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.ViewModels;
using SchoolSystem.Models;
using System.Security.Claims;
using SchoolSystem.Data;

namespace SchoolSystem.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Documents
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var documents = await _context.Documents
            .Include(d => d.User)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadDate)
            .Select(d => new DocumentIndexVM
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                UploadDate = d.UploadDate,
                FileType = d.FileType,
                FileSize = d.FileSize,
                UserName = d.User.UserName
            })
            .ToListAsync();
        return View(documents);
    }

        // GET: Documents/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var document = await _context.Documents
            .Include(d => d.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (document == null) return NotFound();

        // Check if the current user owns the document
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (document.UserId != currentUserId)
            return Forbid();

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
            UserId = document.UserId
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

            if (document.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var viewModel = new DocumentEditVM
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                ExistingFilePath = document.FilePath,
                NewFile = null
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

            if (document.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            if (document.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
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
    }
}