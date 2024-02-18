using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CarvedRock.WebApp;

public class EmailService : IEmailSender
{
    private readonly SmtpClient _client;
    public EmailService(IConfiguration config)
    {
        var port = config.GetValue<int>("CarvedRock:EmailPort");
        var host = config.GetValue<string>("CarvedRock:EmailHost")!;
        _client = new() { Port = port, Host = host };
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {                
        var mailMessage = new MailMessage
        {
            Body = htmlMessage,
            Subject = subject,
            IsBodyHtml = true,
            From = new MailAddress("e-commerce@carvedrock.com", "Carved Rock Shop"),
            To = { email }
        };
        return _client.SendMailAsync(mailMessage);
    }
}
