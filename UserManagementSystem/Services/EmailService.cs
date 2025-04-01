using System.Net;
using System.Net.Mail;

namespace UserManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string name, string token);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(string email, string name, string token)
        {
            // For demo purposes, just display the information
            // In a real application, you would send an actual email
            Console.WriteLine($"Verification email sent to {email}");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"Verification Token: {token}");

            // Simulating async operation
            await Task.CompletedTask;

            
            var mailServer = _configuration["Email:Server"];
            var mailPort = int.Parse(_configuration["Email:Port"]);
            var mailUsername = _configuration["Email:Username"];
            var mailPassword = _configuration["Email:Password"];
            var senderEmail = _configuration["Email:SenderEmail"];
            
            var client = new SmtpClient(mailServer)
            {
                Port = mailPort,
                Credentials = new NetworkCredential(mailUsername, mailPassword),
                EnableSsl = true,
            };
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail),
                Subject = "Verify your email",
                Body = $"Hi {name}, Your verification token is: {token}",
                IsBodyHtml = false
            };
            
            mailMessage.To.Add(email);
            
            await client.SendMailAsync(mailMessage);
           
        }
    }
}
