using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace Baratickets2._0.Services // <--- VERIFICA QUE ESTE SEA TU NAMESPACE
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Este es un "Mock": no envía nada, pero evita el error de Identity
            return Task.CompletedTask;
        }
    }
}