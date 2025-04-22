using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Controllers;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SchoolSystem.Tests
{
    [TestFixture]
    public class RoleControllerTests
    {
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private Mock<UserManager<AppUser>> _userManagerMock;
        private RoleController _controller;

        [SetUp]
        public void Setup()
        {
           
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();

           
            var roleValidators = new List<IRoleValidator<IdentityRole>>();
            var lookupNormalizerMock = new Mock<ILookupNormalizer>();
            var identityErrorDescriberMock = new Mock<IdentityErrorDescriber>();
            var loggerMock = new Mock<ILogger<RoleManager<IdentityRole>>>();

         
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object,
                roleValidators,
                lookupNormalizerMock.Object,
                identityErrorDescriberMock.Object,
                loggerMock.Object
            );

     
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
            var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
            var lookupNormalizerUserMock = new Mock<ILookupNormalizer>();
            var identityErrorDescriberUserMock = new Mock<IdentityErrorDescriber>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var loggerUserMock = new Mock<ILogger<UserManager<AppUser>>>();

            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object,
                identityOptionsMock.Object,
                passwordHasherMock.Object,
                new IUserValidator<AppUser>[0],
                new IPasswordValidator<AppUser>[0],
                lookupNormalizerUserMock.Object,
                identityErrorDescriberUserMock.Object,
                serviceProviderMock.Object,
                loggerUserMock.Object
            );

            // ✅ Initialize Controller correctly
            _controller = new RoleController(_userManagerMock.Object, _roleManagerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public async Task CreateRole_WhenRoleDoesNotExist_CreatesRoleAndRedirects()
        {
           
            var roleVM = new RoleVM { Name = "Admin" };
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Admin")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                            .ReturnsAsync(IdentityResult.Success);

          
            var result = await _controller.CreateRole(roleVM) as RedirectToActionResult;

         
            Assert.NotNull(result);
            Assert.AreEqual("ListRoles", result.ActionName);
        }

        [Test]
        public async Task CreateRole_WhenRoleAlreadyExists_ReturnsViewWithError()
        {
            
            var roleVM = new RoleVM { Name = "Admin" };
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Admin")).ReturnsAsync(true);

            var result = await _controller.CreateRole(roleVM) as ViewResult;

            
            Assert.NotNull(result);
            Assert.AreEqual(roleVM, result.Model);
            Assert.True(result.ViewData.ModelState.ContainsKey(""));
        }

        [Test]
        public async Task DeleteRole_WhenRoleDoesNotExist_RedirectsWithErrorMessage()
        {
           
            _roleManagerMock.Setup(r => r.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole?)null);

            
            var result = await _controller.DeleteRole("invalidRoleId") as RedirectToActionResult;

      
            Assert.NotNull(result);
            Assert.AreEqual("ListRoles", result.ActionName);
            Assert.AreEqual("Role not found.", _controller.TempData["Error"]);
        }
    }
}