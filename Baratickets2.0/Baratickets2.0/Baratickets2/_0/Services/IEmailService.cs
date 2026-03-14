namespace Baratickets2._0.Services
{
    public interface IEmailService
    {
        // 1.email, 2.nombre, 3.evento, 4.tipo, 5.precio, 6.qr
        Task EnviarTicketAsync(string emailDestino, string nombreUsuario, string evento, string tipoTicket, decimal precio, string qrBase64);

        Task EnviarNotificacionDevolucionAsync(string emailDestino, string nombreUsuario, string evento);
        Task EnviarCorreoAsync(string emailDestino, string asunto, string contenidoHtml);
    }
}