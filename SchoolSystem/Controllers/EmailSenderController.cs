using EmailSender.Services;
using Microsoft.AspNetCore.Mvc;
using EmailSender.Services;

namespace EmailSender.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }


        [HttpPost("send-test")]
        public async Task<IActionResult> SendEmails([FromBody] EmailRequest request)
        {
            if (request == null || request.Recipients == null || !request.Recipients.Any())
            {
                return BadRequest("Recipient list cannot be empty.");
            }

            await _emailService.SendEmailsAsync(request.Recipients, request.Subject, request.Body);
            return Ok("Emails sent successfully.");
        }
        public async Task<IActionResult> SendTestEmails()
        {
            var recipients = new List<string>
            {
                "buicaonguyen6@gmail.com", "nbuicao22@gmail.com"
            };

            var subject = "Test Email Subject";
            var body = @"
                <h1>Test Email</h1>
                <p>This is a test email body sent to multiple users.</p>
                <p><strong>Enjoy!</strong></p>";

            await _emailService.SendEmailsAsync(recipients, subject, body);

            return Ok("Test emails sent successfully.");
        }
    }

    public class EmailRequest
    {
        public List<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
    public class EmailSettings
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
    }
}
