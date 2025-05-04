using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Controllers;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmailSender.Services;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class BlogControllerTests
    {
        private BlogController _controller;
        private Mock<UserManager<AppUser>> _mockUserManager;
        private AppDbContext _context;

        private Mock<IEmailService> _mockEmailService;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Đảm bảo mỗi test có DB riêng
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            _mockEmailService = new Mock<IEmailService>(); // Thêm dòng này!

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(
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

            _controller = new BlogController(_context, _mockUserManager.Object, _mockEmailService.Object);
        }

        [TearDown]
        public void Cleanup()
        {
            _context?.Dispose();
            if (_controller != null)
            {
                _controller.Dispose();
            }

        }

        [Test]
        public async Task Index_ReturnsViewWithBlogs()
        {
            // Arrange: Thêm dữ liệu vào InMemory Database
            _context.Blogs.AddRange(new List<Blog>
            {
                new Blog { Id = 1, Title = "Test Blog 1", Content = "Content 1", UserId = "User1" },
                new Blog { Id = 2, Title = "Test Blog 2", Content = "Content 2", UserId = "User2" }
            });
            await _context.SaveChangesAsync();

            // Act: Gọi phương thức Index()
            var result = await _controller.Index() as ViewResult;

            // Assert: Kiểm tra dữ liệu trả về
            Assert.NotNull(result);
            Assert.IsInstanceOf<List<Blog>>(result.Model);
            Assert.AreEqual(2, ((List<Blog>)result.Model).Count);
        }

        [Test]
        public async Task Create_Blog_ReturnsRedirectToIndex()
        {
            // Arrange: Mock user
            var user = new AppUser { Id = "User1", UserName = "TestUser" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var model = new BlogVM { Title = "New Blog", Content = "This is a test blog" };

            // Act: Gọi phương thức Create
            var result = await _controller.Create(model) as RedirectToActionResult;

            // Assert: Kiểm tra kết quả
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [Test]
        public async Task Edit_Blog_UpdatesSuccessfully()
        {
            // Arrange: Thêm một blog để sửa
            var blog = new Blog { Id = 1, Title = "Old Title", Content = "Old Content", UserId = "User1" };
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();

            var model = new BlogVM { Id = 1, Title = "Updated Title", Content = "Updated Content" };

            // Act: Gọi phương thức Edit
            var result = await _controller.Edit(model) as RedirectToActionResult;

            // Assert: Kiểm tra blog đã được cập nhật
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var updatedBlog = await _context.Blogs.FindAsync(1);
            Assert.AreEqual("Updated Title", updatedBlog.Title);
        }

        [Test]
        public async Task Delete_Blog_RemovesSuccessfully()
        {
            // Arrange: Thêm blog vào database
            var blog = new Blog { Id = 1, Title = "Blog to be deleted", Content = "Content", UserId = "User1" };
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();

            // Act: Gọi phương thức Delete
            var result = await _controller.Delete(1) as RedirectToActionResult;

            // Assert: Kiểm tra blog đã bị xóa
            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.Null(await _context.Blogs.FindAsync(1));
        }
    }
}