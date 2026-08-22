using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace AppAlumnos.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Aquí puedes agregar la lógica para enviar correos en el futuro (SendGrid, SMTP, etc).
            return Task.CompletedTask;
        }
    }
}