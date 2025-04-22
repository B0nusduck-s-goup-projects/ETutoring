using System.Net;
using System.Net.Mail;

namespace EmailSender.Services
{
    public interface IEmailService
    {
        Task SendEmailsAsync(List<string> recipients, string subject, string body);
    }
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(string smtpHost, int smtpPort, string smtpUser, string smtpPass)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
        }

        public async Task SendEmailsAsync(List<string> recipients, string subject, string body)
        {
            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.EnableSsl = true;

                foreach (var recipient in recipients)
                {
                    using (var message = new MailMessage(_smtpUser, recipient))
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