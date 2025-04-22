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

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class DocumentControllerTests
    {
        private Mock<UserManager<AppUser>> _userManagerMock;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private AppDbContext _context;
        private DocumentController _controller;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique database per test
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            _controller = new DocumentController(_context, _webHostEnvironmentMock.Object, _userManagerMock.Object);

            // Clear previous data before seeding
            _context.Users.RemoveRange(_context.Users);
            _context.Documents.RemoveRange(_context.Documents);
            await _context.SaveChangesAsync();

            // Seed test user
            var testUser = new AppUser
            {
                Id = "1",
                Code = "USER001",
                Gender = "Male",
                Name = "Test User"
            };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Seed a test document with all required fields set
            _context.Documents.Add(new Document
            {
                Id = 1, // Ensure a valid ID for deletion test
                Title = "Test Document",
                Description = "Sample document for testing",
                FilePath = "/uploads/sample.pdf",
                UserId = testUser.Id,
                User = testUser, // Explicitly set the User property
                FileType = "pdf"
            });

            await _context.SaveChangesAsync();
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