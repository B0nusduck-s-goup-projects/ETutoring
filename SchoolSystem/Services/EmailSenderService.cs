using System.Net;
using System.Net.Mail;
using EmailSender.Controllers;
using Microsoft.Extensions.Options;

namespace EmailSender.Services
{
    public interface IEmailService
    {
        Task SendEmailsAsync(List<string> recipients, string subject, string body);
    }
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailsAsync(List<string> recipients, string subject, string body)
        {
            using (var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort))
            {
                client.Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                client.EnableSsl = true;

                foreach (var recipient in recipients)
                {
                    using (var message = new MailMessage(_emailSettings.SmtpUser, recipient))
                    {
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        try
                        {
                            await client.SendMailAsync(message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending email to {recipient}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
                