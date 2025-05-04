using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using SchoolSystem.Controllers;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmailSender.Services;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class DocumentControllerTests
    {
        private Mock<UserManager<AppUser>> _userManagerMock;
        private Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private AppDbContext _context;
        private DocumentController _controller;

        private Mock<IEmailService> _mockEmailService;

        [SetUp]
        public async Task Setup()
        {
            // Cấu hình In-Memory Database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Đảm bảo mỗi test có DB riêng
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            // Xóa dữ liệu cũ trước khi tạo mới
            _context.Users.RemoveRange(_context.Users);
            _context.Documents.RemoveRange(_context.Documents);
            await _context.SaveChangesAsync();

            // Mock môi trường web (nếu cần)
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            // Mock Email Service (dùng interface thay vì class trực tiếp)
            _mockEmailService = new Mock<IEmailService>();

            // Mock UserManager với tất cả tham số cần thiết
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<AppUser>>().Object,
                new IUserValidator<AppUser>[0],
                new IPasswordValidator<AppUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<AppUser>>>().Object
            );

            // Tạo người dùng test với tất cả thuộc tính bắt buộc
            var testUser = new AppUser
            {
                Id = "User1",
                Code = "USER001", // Thuộc tính bắt buộc
                Gender = "Male",  // Thuộc tính bắt buộc
                Name = "Test User"
            };

            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Thêm tài liệu mẫu để kiểm thử Delete và Edit
            _context.Documents.Add(new Document
            {
                Id = 1,
                Title = "Sample Document",
                Description = "For unit testing",
                FilePath = "/uploads/test.pdf",
                UserId = testUser.Id,
                User = testUser, // Đảm bảo User không null
                FileType = "pdf"
            });

            await _context.SaveChangesAsync();

            // Khởi tạo Controller với các mock phụ thuộc
            _controller = new DocumentController(_context, _mockWebHostEnvironment.Object, _userManagerMock.Object, _mockEmailService.Object);
        }

        [TearDown]
        public void Cleanup()
        {
            _context?.Dispose();
            if (_controller is IDisposable disposableController)
            {
                disposableController.Dispose();
            }
        }

        [Test]
        public async Task Index_ReturnsViewWithDocuments()
        {
            var result = await _controller.Index() as ViewResult;
            Assert.NotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsInstanceOf<List<DocumentIndexVM>>(result.Model);
        }

        [Test]
        public async Task Details_ValidId_ReturnsViewWithDocument()
        {
            var result = await _controller.Details(1) as ViewResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            var result = await _controller.Details(null);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Create_ReturnsView()
        {
            var result = _controller.Create() as ViewResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Edit_ValidId_ReturnsViewWithDocument()
        {
            var result = await _controller.Edit(1) as ViewResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Edit_InvalidId_ReturnsNotFound()
        {
            var result = await _controller.Edit(null);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Delete_ValidId_ReturnsView()
        {
            // Act
            var result = await _controller.Delete(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            var result = await _controller.Delete(null);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}