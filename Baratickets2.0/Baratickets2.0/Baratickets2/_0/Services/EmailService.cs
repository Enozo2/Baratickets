using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using MimeKit.Utils; // Necesario para MimeUtils

namespace Baratickets2._0.Services
{

    public class EmailService : IEmailService
    {
       
        public async Task EnviarTicketAsync(string emailDestino, string nombreUsuario, string evento, string tipoTicket, decimal precio, string qrBase64)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("BaraTickets 2.0", "enocr28@gmail.com"));
            mensaje.To.Add(new MailboxAddress(nombreUsuario, emailDestino));
            mensaje.Subject = $"🎫 Confirmación: {evento}";

            var bodyBuilder = new BodyBuilder();

            // 1. Limpiamos el base64 por si trae encabezados y convertimos a bytes
            if (qrBase64.Contains(","))
            {
                qrBase64 = qrBase64.Split(',')[1];
            }
            byte[] imageBytes = Convert.FromBase64String(qrBase64);

            // 2. Creamos un recurso vinculado (CID) para que Gmail no lo bloquee
            var image = bodyBuilder.LinkedResources.Add("qrcode.png", imageBytes);
            image.ContentId = MimeUtils.GenerateMessageId();

            // 3. Construcción del HTML usando el diseño profesional y el cid de la imagen
            bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial; text-align: center; border: 2px solid #007bff; padding: 25px; border-radius: 15px; max-width: 500px; margin: auto;'>
                <h1 style='color: #007bff;'>¡Gracias por tu compra!</h1>
                <p>Hola <b>{nombreUsuario}</b>, aquí tienes tu entrada para <b>{evento}</b>.</p>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0; border: 1px solid #ddd;'>
                    <p style='margin: 5px 0;'><b>Tipo de Ticket:</b> {tipoTicket}</p>
                    <p style='margin: 5px 0;'><b>Monto Pagado:</b> RD$ {precio:N0}</p>
                </div>
                
                <div style='margin: 20px 0;'>
                    <h2 style='color: #333;'>Tu Ticket para {evento}</h2>
                    <img src='cid:{image.ContentId}' width='250' height='250' style='display: block; margin: 10px auto; border: 1px solid #ccc;' />
                    <p>Presenta este código en la entrada.</p>
                </div>
                
                <p style='color: #888; font-size: 13px;'>Muestra este código QR desde tu celular al llegar al evento.</p>
                <p style='font-size: 11px; color: #aaa; margin-top: 20px;'>BaraTickets 2.0 - Universidad Proyecto</p>
            </div>";

            mensaje.Body = bodyBuilder.ToMessageBody();


            using (var cliente = new SmtpClient())
            {
                try
                {
                    cliente.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    // Conexión segura por puerto 465 (SslOnConnect)
                    await cliente.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);

                    // Autenticación con tu clave de aplicación de 16 caracteres
                    await cliente.AuthenticateAsync("enocr28@gmail.com", "swbybflvqzwhisar");

                    await cliente.SendAsync(mensaje);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("********** ERROR CRÍTICO DE ENVÍO **********");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    throw;
                }
                finally
                {
                    await cliente.DisconnectAsync(true);
                }
            }
        }
        // Este es el método que falta para cumplir con la interfaz IEmailService
        public async Task EnviarCorreoAsync(string emailDestino, string asunto, string contenidoHtml)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("BaraTickets 2.0", "enocr28@gmail.com"));
            mensaje.To.Add(new MailboxAddress("Usuario", emailDestino));
            mensaje.Subject = asunto;

            var bodyBuilder = new BodyBuilder { HtmlBody = contenidoHtml };
            mensaje.Body = bodyBuilder.ToMessageBody();

            using (var cliente = new SmtpClient())
            {
                cliente.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // Usa tu misma configuración que ya funciona
                await cliente.ConnectAsync("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await cliente.AuthenticateAsync("enocr28@gmail.com", "swbybflvqzwhisar");
                await cliente.SendAsync(mensaje);
                await cliente.DisconnectAsync(true);
            }
        }
        public async Task EnviarNotificacionDevolucionAsync(string emailDestino, string nombreUsuario, string evento)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("BaraTickets 2.0", "enocr28@gmail.com"));
            mensaje.To.Add(new MailboxAddress(nombreUsuario, emailDestino));
            mensaje.Subject = $"↩️ Confirmación de Devolución: {evento}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
    <div style='font-family: Arial; text-align: center; border: 2px solid #dc3545; padding: 25px; border-radius: 15px; max-width: 500px; margin: auto;'>
        <h1 style='color: #dc3545;'>Ticket Devuelto</h1>
        <p>Hola <b>{nombreUsuario}</b>,</p>
        <p>Te confirmamos que el ticket para el evento <b>{evento}</b> ha sido procesado como <b>devuelto</b> correctamente.</p>
        
        <div style='background: #fff5f5; padding: 15px; border-radius: 8px; margin: 15px 0; border: 1px solid #fabebf;'>
            <p style='margin: 5px 0; color: #a94442;'>El código QR asociado a esta entrada ha sido invalidado y ya no podrá ser usado para ingresar al recinto.</p>
        </div>
        
        <p style='color: #888; font-size: 13px;'>Si no realizaste esta acción, por favor contacta a soporte de inmediato.</p>
        <p style='font-size: 11px; color: #aaa; margin-top: 20px;'>BaraTickets 2.0 - Sistema de Gestión Universitaria</p>
    </div>";

            mensaje.Body = bodyBuilder.ToMessageBody();

            using (var cliente = new SmtpClient())
            {
                try
                {
                    cliente.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await cliente.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                    await cliente.AuthenticateAsync("enocr28@gmail.com", "swbybflvqzwhisar");
                    await cliente.SendAsync(mensaje);
                }
                finally
                {
                    await cliente.DisconnectAsync(true);
                }
            }
        }
    }
}