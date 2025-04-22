using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
    public class MessagesControllerTests
    {
        private Mock<UserManager<AppUser>> _mockUserManager;
        private AppDbContext _context;
        private MessagesController _controller;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Ensure a fresh database per test run
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            // Properly initialize the UserManager mock
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

            // Ensure data is cleared before seeding
            _context.Users.RemoveRange(_context.Users);
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

            // Initialize controller with properly set UserManager
            _controller = new MessagesController(_context, _mockUserManager.Object);
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
        public async Task ChatWindow_ValidGroupId_ReturnsViewWithMessages()
        {
            var result = await _controller.ChatWindow(1) as ViewResult;
            Assert.NotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsInstanceOf<ChatWindowVM>(result.Model);
        }

        [Test]
        public async Task ChatWindow_InvalidGroupId_ReturnsViewWithEmptyMessages()
        {
            var result = await _controller.ChatWindow(999) as ViewResult;
            var viewModel = result?.Model as ChatWindowVM;

            Assert.NotNull(result);
            Assert.NotNull(viewModel);
            Assert.IsEmpty(viewModel.Messages);
        }

        [Test]
        public async Task ChatList_StudentRole_RedirectsToChatWindow()
        {
            _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "Student" });

            var result = await _controller.ChatList() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.AreEqual("ChatWindow", result.ActionName);
        }

        [Test]
        public async Task ChatList_NonStudentRole_ReturnsChatListView()
        {
            _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "Tutor" });

            var result = await _controller.ChatList() as ViewResult;
            Assert.NotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
        }
    }
}