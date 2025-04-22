using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolSystem.Controllers;
using SchoolSystem.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Data;
using SchoolSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class HomeControllerTests
    {
        private Mock<ILogger<HomeController>> _loggerMock;
        private HomeController _controller;
        private Mock<AppDbContext> _contextMock;
        private AppDbContext _context;
        private Mock<UserManager<AppUser>> _mockUserManager;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique database per test run
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            // Initialize mock dependencies properly
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
            _controller = new HomeController(_mockUserManager.Object, _context);
        }

        [TearDown]
       
        public void Cleanup()
        {
            _context?.Dispose();
           
                _context?.Dispose();

                if (_controller is IDisposable disposableController)
                {
                    disposableController.Dispose();
                }
            
        }


        [Test]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index() as ViewResult;

            Assert.NotNull(result);
        }

        [Test]
        public void Privacy_ReturnsViewResult()
        {
            var result = _controller.Privacy() as ViewResult;

            Assert.NotNull(result);
        }

        [Test]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var expectedRequestId = Activity.Current?.Id ?? "TestTraceId";

            var result = _controller.Error() as ViewResult;
            var model = result?.Model as ErrorViewModel;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.False(string.IsNullOrEmpty(model.RequestId));
        }
    }
}