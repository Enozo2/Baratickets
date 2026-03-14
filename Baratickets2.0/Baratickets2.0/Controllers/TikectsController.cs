using Baratickets2._0.Data;
using Baratickets2._0.Models;
using Baratickets2._0.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Baratickets2._0.Controllers
{
    [Authorize] // <--- Esto obliga a que el usuario esté logueado para entrar a cualquier vista de este controlador
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QrService _qrService;
        private readonly IEmailService _emailService;

        public TicketsController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 QrService qrService,
                                 IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _qrService = qrService;
            _emailService = emailService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ComprarTicket(int eventoId, int categoriaId, string emailConfirmacion, string tokenSeguridad)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Buscamos las entidades (FindAsync las pone bajo seguimiento de EF)
            var evento = await _context.Eventos.FindAsync(eventoId);
            var categoria = await _context.CategoriasTickets.FindAsync(categoriaId);

            if (evento == null || categoria == null) return NotFound();

            // 2. Validación de seguridad: Si no hay stock en la categoría, cancelamos
            if (categoria.Capacidad <= 0)
            {
                TempData["Error"] = "Lo sentimos, ya no quedan boletas para esta sección.";
                return RedirectToAction("Index", "Home");
            }

            // 3. CREACIÓN DEL TICKET
            var nuevoTicket = new Ticket
            {
                Id = Guid.NewGuid(),
                EventoId = eventoId,
                UsuarioId = userId,
                FechaCompra = DateTime.Now,
                Tipo = categoria.Nombre,
                PrecioPagado = categoria.Precio,
                FueUsado = false
            };
            if (string.IsNullOrEmpty(tokenSeguridad))
            {
                return BadRequest("Transacción no autorizada por el ente financiero.");
            }

            // 4. DESCUENTO SINCRONIZADO (1 en 1)
            // Para que no baje de 2 en 2, restamos ÚNICAMENTE a la categoría.
            // El organizador verá que bajó 1 en su lista de boletas.
            categoria.Capacidad -= 1;

            // Si tu vista general también depende de la capacidad del evento, 
            // asegúrate de que esa capacidad sea la SUMA de las categorías en la base de datos,
            // pero si la manejas manual, puedes dejar esta línea comentada o borrarla:
            // evento.Capacidad -= 1; 

            // 5. AGREGAMOS EL TICKET
            _context.Tickets.Add(nuevoTicket);

            // Guardar cambios una sola vez asegura que la operación sea atómica
            await _context.SaveChangesAsync();

            // 6. ENVÍO DE CORREO Y QR
            try
            {
                string urlValidacion = $"https://localhost:7204/Tickets/ValidarEntrada?id={nuevoTicket.Id}";
                string qr = _qrService.GenerarQrBase64(urlValidacion);

                await _emailService.EnviarTicketAsync(
                    emailConfirmacion,
                    User.Identity.Name ?? "Cliente",
                    evento.Nombre,
                    categoria.Nombre,
                    categoria.Precio,
                    qr
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error enviando correo: " + ex.Message);
            }

            // Redirigimos a la vista del ticket
            return RedirectToAction("VerTicket", new { id = nuevoTicket.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DevolverTicket(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            var ticket = await _context.Tickets
                .Include(t => t.Evento)
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == userId);

            if (ticket == null) return NotFound();

            if (ticket.FueUsado || ticket.Estado == "Devuelto")
            {
                TempData["Error"] = "Este ticket no puede ser devuelto.";
                return RedirectToAction("MisTickets");
            }

            // BUSCAMOS LA CATEGORÍA DINÁMICA
            // Buscamos la categoría que coincida con el nombre guardado en el ticket para este evento
            var categoria = await _context.CategoriasTickets
                .FirstOrDefaultAsync(c => c.EventoId == ticket.EventoId && c.Nombre == ticket.Tipo);

            if (categoria != null)
            {
                // Devolvemos el cupo a la categoría correspondiente (sea VIP, Lado Izquierdo, etc.)
                categoria.Capacidad += 1;
                _context.Update(categoria);
            }

            ticket.Estado = "Devuelto";
            ticket.FechaUso = DateTime.Now; // Usamos esto como marca de tiempo de la devolución

            _context.Update(ticket);
            await _context.SaveChangesAsync();

            // NOTIFICACIÓN
            try
            {
                await _emailService.EnviarNotificacionDevolucionAsync(ticket.Usuario.Email, ticket.Usuario.NombreCompleto, ticket.Evento.Nombre);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error enviando correo: " + ex.Message);
            }

            TempData["CompraExitosa"] = "Ticket devuelto correctamente. El cupo ha sido liberado.";
            return RedirectToAction("MisTickets");
        }
        [Authorize(Roles = "Admin,Validador")]
        public IActionResult Escanear()
        {
            
            return View("EscanearTicket");
        }

        [HttpGet]
        [Authorize]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProcesarPago(int eventoId, int categoriaId)
        {
            var evento = await _context.Eventos.FindAsync(eventoId);
            var categoria = await _context.CategoriasTickets.FindAsync(categoriaId);

            if (evento == null || categoria == null) return NotFound();

            // Pasamos los datos dinámicos a la vista mediante ViewBag o ViewModel
            ViewBag.Tipo = categoria.Nombre;
            ViewBag.Precio = categoria.Precio;
            ViewBag.CategoriaId = categoriaId; // Importante para el POST final
            return View(await _context.Eventos.FindAsync(eventoId));
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ValidarEntrada(Guid id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Evento)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                TempData["Error"] = "El ticket no existe en la base de datos.";
                return RedirectToAction("Index", "Home"); 
            }

            return View(ticket); 
        }
        public async Task<IActionResult> VerTicket(Guid id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Evento)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            ViewBag.QrCode = _qrService.GenerarQrBase64(ticket.Id.ToString());
            return View(ticket);
        }

        public async Task<IActionResult> MisTickets()
        {
            var userId = _userManager.GetUserId(User);

            var tickets = await _context.Tickets
                .Include(t => t.Evento)
                .Where(t => t.UsuarioId == userId && t.Estado != "Devuelto")
                .OrderByDescending(t => t.FechaCompra)
                .ToListAsync();

            return View(tickets);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Validador")]
        public async Task<IActionResult> ProcesarEscaneo(Guid id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Evento)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return Json(new { success = false, message = "¡TICKET NO ENCONTRADO EN DB!" });

            if (ticket.FueUsado)
                return Json(new { success = false, message = "ESTE TICKET YA FUE USADO." });

            ticket.FueUsado = true;
            ticket.FechaUso = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "ACCESO CONCEDIDO",
                evento = ticket.Evento.Nombre,
                tipo = ticket.Tipo
            });
        }
    }
}