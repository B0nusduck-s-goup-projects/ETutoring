using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using EmailSender.Controllers;
using EmailSender.Services;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace SchoolSystem.Tests
{
    [TestFixture]
    public class EmailControllerTests
    {
        private Mock<IEmailService> _emailServiceMock;
        private EmailController _controller;

        [SetUp]
        public void Setup()
        {
            _emailServiceMock = new Mock<IEmailService>(); 
            _controller = new EmailController(_emailServiceMock.Object);
        }

        [Test]
        public async Task SendEmails_WithValidRequest_ReturnsOkResult()
        {
            var request = new EmailRequest
            {
                Recipients = new List<string> { "test1@example.com", "test2@example.com" },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            _emailServiceMock.Setup(e => e.SendEmailsAsync(request.Recipients, request.Subject, request.Body))
                .Returns(Task.CompletedTask);

            var result = await _controller.SendEmails(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("Emails sent successfully.", result.Value);
        }

        [Test]
        public async Task SendEmails_WithNullRequest_ReturnsBadRequest()
        {
            var result = await _controller.SendEmails(null) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Recipient list cannot be empty.", result.Value);
        }

        [Test]
        public async Task SendEmails_WithEmptyRecipients_ReturnsBadRequest()
        {
            var request = new EmailRequest
            {
                Recipients = new List<string>(),
                Subject = "Test Subject",
                Body = "Test Body"
            };

            var result = await _controller.SendEmails(request) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Recipient list cannot be empty.", result.Value);
        }

        [Test]
        public async Task SendTestEmails_ReturnsOkResult()
        {
            var recipients = new List<string> { "test1@example.com", "test2@example.com" };
            var subject = "Test Email Subject";
            var body = "<h1>Test Email</h1><p>Test email body.</p>";

            _emailServiceMock.Setup(e => e.SendEmailsAsync(recipients, subject, body))
                .Returns(Task.CompletedTask);

            var result = await _controller.SendTestEmails() as OkObjectResult;

            Assert.NotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("Test emails sent successfully.", result.Value);
        }
    }
}