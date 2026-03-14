using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class Email_Service
{
    public async Task EnviarTicketAsync(string emailDestino, string nombreUsuario, string evento, string tipoTicket, decimal precio, string qrBase64)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("BaraTickets 2.0", "enocr28@gmail.com"));
        mensaje.To.Add(new MailboxAddress(nombreUsuario, emailDestino));
        mensaje.Subject = $"Tu Ticket para {evento}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <div style='font-family: Arial; text-align: center; border: 1px solid #ccc; padding: 20px;'>
                <h2 style='color: #007bff;'>¡Gracias por tu compra, {nombreUsuario}!</h2>
                <p>Aquí tienes tu entrada para <b>{evento}</b></p>
                <hr />
                <p><b>Tipo:</b> {tipoTicket} | <b>Precio:</b> RD$ {precio:N0}</p>
                
                <div style='margin-top: 20px;'>
                    <p>Presenta este código QR en la entrada:</p>
                    <img src='data:image/png;base64,{qrBase64}' alt='Código QR' style='width: 200px; height: 200px;' />
                </div>
                
                <p style='font-size: 12px; color: #666; margin-top: 20px;'>BaraTickets - Tu plataforma de eventos.</p>
            </div>"
        };
        mensaje.Body = bodyBuilder.ToMessageBody();

        using (var cliente = new SmtpClient())
        {
            cliente.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await cliente.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
            await cliente.AuthenticateAsync("enocr28@gmail.com", "swbybflvqzwhisar");
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}