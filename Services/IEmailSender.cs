// IEmailSender.cs
using System.Threading.Tasks;

namespace JamesThewProject.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
