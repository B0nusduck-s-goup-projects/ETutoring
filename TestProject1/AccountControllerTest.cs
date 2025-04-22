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

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<SignInManager<AppUser>> _signInManagerMock;
        private Mock<UserManager<AppUser>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            _userManagerMock = new Mock<UserManager<AppUser>>(
                new Mock<IUserStore<AppUser>>().Object,
                null, null, null, null, null, null, null, null
            );

            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                _userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<AppUser>>().Object,
                null, null, null, null
            );

            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                new Mock<IRoleStore<IdentityRole>>().Object,
                null, null, null, null
            );

            _controller = new AccountController(_signInManagerMock.Object, _userManagerMock.Object, _roleManagerMock.Object);
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