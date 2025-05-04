using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Controllers;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EmailSender.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<SignInManager<AppUser>> _signInManagerMock;
        private Mock<UserManager<AppUser>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private AccountController _controller;

        private Mock<IEmailService> _mockEmailService;



        [SetUp]
        public void Setup()
        {
            _mockEmailService = new Mock<IEmailService>();
            var emailServiceMock = new Mock<EmailService>();
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

            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                _userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<AppUser>>().Object,
                null, null, null, null
            );

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object,
                new List<IRoleValidator<IdentityRole>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
            _mockEmailService.Setup(e => e.SendEmailsAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
            var emailService = new EmailService("smtp.example.com", 587, "username", "password"); 
_controller = new AccountController(_signInManagerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, emailService);
            _mockEmailService = new Mock<IEmailService>();
            _controller = new AccountController(_signInManagerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, _mockEmailService.Object);
        }


            [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public void Login_ReturnsView()
        {
            var result = _controller.Login() as ViewResult;

            Assert.NotNull(result);
        }

        [Test]
        public async Task Login_InvalidUser_ReturnsViewWithError()
        {
            var loginVM = new LoginVM { Username = "testuser", Password = "wrongpass", RememberMe = false };
            _userManagerMock.Setup(u => u.FindByNameAsync("testuser")).ReturnsAsync((AppUser?)null);

            var result = await _controller.Login(loginVM) as ViewResult;

            Assert.NotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey(""));
        }

        [Test]
        public async Task Logout_RedirectsToHomeIndex()
        {
            var result = await _controller.Logout() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
        }
    }
}