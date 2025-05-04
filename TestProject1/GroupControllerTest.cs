using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Controllers;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmailSender.Services;
using Microsoft.AspNetCore.Hosting;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class GroupsControllerTests
    {
        
        private AppDbContext _context;
        private GroupsController _controller;
        private Mock<UserManager<AppUser>> _mockUserManager;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        [TearDown]
        public void Cleanup()
        {
            _context?.Dispose();

            if (_controller is IDisposable disposableController)
            {
                disposableController.Dispose();
            }
        }




       

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new AppDbContext(options);

            _mockEmailService = new Mock<IEmailService>(); 
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>(); 

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

            _controller = new GroupsController(_context, _mockUserManager.Object, _mockEmailService.Object, _mockWebHostEnvironment.Object);
        }

        [Test]
        public async Task Index_ReturnsViewWithGroups()
        {
            var result = await _controller.Index() as ViewResult;
            Assert.NotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Details_ValidId_ReturnsViewWithGroup()
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
        public async Task Create_ReturnsViewWithGroupCreateVM()
        {
            var result = await _controller.Create() as ViewResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Edit_ValidId_ReturnsViewWithGroup()
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
    }
}