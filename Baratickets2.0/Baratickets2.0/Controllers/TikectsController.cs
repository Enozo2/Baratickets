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
        public async Task<IActionResult> ComprarTicket(int eventoId, int categoriaId, string emailConfirmacion, string? tokenSeguridad)
        {
            var userId = _userManager.GetUserId(User);
            var evento = await _context.Eventos.FindAsync(eventoId);
            var categoria = await _context.CategoriasTickets.FindAsync(categoriaId);
            if (evento == null || categoria == null) return NotFound();

            if (categoria.Capacidad <= 0)
            {
                TempData["Error"] = "Lo sentimos, ya no quedan boletas.";
                return RedirectToAction("Index", "Home");
            }

            decimal montoDescuento = 0;
            bool esCupon = !string.IsNullOrEmpty(tokenSeguridad) && tokenSeguridad.StartsWith("REFUND-");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (esCupon)
                {
                    var cupon = await _context.Devoluciones
                        .FirstOrDefaultAsync(c => c.CodigoCupon == tokenSeguridad && c.MontoRestante > 0);

                    if (cupon == null)
                    {
                        TempData["Error"] = "Cupón ya utilizado o inválido.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("MisTickets");
                    }

                    // ✅ Validar expiración en backend también
                    if (cupon.FechaExpiracion.HasValue && cupon.FechaExpiracion < DateTime.Now)
                    {
                        TempData["Error"] = $"Este cupón expiró el {cupon.FechaExpiracion.Value:dd/MM/yyyy}.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("MisTickets");
                    }


                    // ✅ Descuenta solo lo que cubre el cupón
                    montoDescuento = Math.Min(cupon.MontoRestante, categoria.Precio);
                    decimal nuevoSaldo = cupon.MontoRestante - montoDescuento;

                    await _context.Devoluciones
                        .Where(c => c.Id == cupon.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.MontoRestante, nuevoSaldo)
                            .SetProperty(c => c.CuponUsado, nuevoSaldo <= 0)
                        );
                }

                decimal precioFinal = Math.Max(0, categoria.Precio - montoDescuento);

                var nuevoTicket = new Ticket
                {
                    Id = Guid.NewGuid(),
                    EventoId = eventoId,
                    UsuarioId = userId,
                    FechaCompra = DateTime.Now,
                    Tipo = categoria.Nombre,
                    PrecioPagado = precioFinal,
                    FueUsado = false,
                    Estado = "Valido",
                    UsoCupon = esCupon
                };

                categoria.Capacidad -= 1;
                _context.Tickets.Add(nuevoTicket);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("VerTicket", new { id = nuevoTicket.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Error al procesar la compra. Intenta nuevamente.";
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ValidarCupon(string codigo, int categoriaId)
        {
            var userId = _userManager.GetUserId(User);
            var cupon = await _context.Devoluciones
                .FirstOrDefaultAsync(c => c.CodigoCupon == codigo && c.UsuarioId == userId);

            if (cupon == null)
                return Json(new { valido = false, mensaje = "Cupón no encontrado." });

            // ✅ Validar que no esté marcado como usado
            if (cupon.CuponUsado)
                return Json(new { valido = false, mensaje = "Este cupón ya fue utilizado." });

            // ✅ Validar expiración
            if (cupon.FechaExpiracion.HasValue && cupon.FechaExpiracion < DateTime.Now)
                return Json(new { valido = false, mensaje = $"Este cupón expiró el {cupon.FechaExpiracion.Value:dd/MM/yyyy}." });

            // ✅ Validar saldo restante
            if (cupon.MontoRestante <= 0)
                return Json(new { valido = false, mensaje = "Este cupón no tiene saldo disponible." });

            var categoria = await _context.CategoriasTickets.FindAsync(categoriaId);
            if (categoria == null)
                return Json(new { valido = false, mensaje = "Categoría no encontrada." });


            // ✅ Calcular cuánto cubre el cupón y cuánto falta pagar
            decimal descuento = Math.Min(cupon.MontoRestante, categoria.Precio);
            decimal restaPagar = categoria.Precio - descuento;

            return Json(new
            {
                valido = true,
                monto = cupon.MontoRestante,
                descuento = descuento,
                restaPagar = restaPagar,
                mensaje = restaPagar > 0
                    ? $"Cupón aplica RD$ {descuento:N0}. Resta pagar RD$ {restaPagar:N0} con tarjeta."
                    : "Cupón cubre el total."
            });
        }
        [HttpPost]
        public async Task<IActionResult> SolicitarDevolucion(Guid ticketId, string tipoDevolucion)
        {
            // 1. Buscamos el ticket e incluimos la categoría para saber cuál actualizar
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // 2. Validamos el tiempo (24h)
            var tiempoTranscurrido = DateTime.Now - ticket.FechaCompra;
            if (tiempoTranscurrido.TotalHours > 24)
            {
                TempData["Error"] = "El plazo de 24h ha expirado.";
                return RedirectToAction("MisTickets");
            }

            // --- NUEVA LÓGICA DE ACTUALIZACIÓN DE STOCK ---
            // Buscamos la categoría que corresponde a este ticket en este evento
            var categoria = await _context.CategoriasTickets
                .FirstOrDefaultAsync(c => c.EventoId == ticket.EventoId && c.Nombre == ticket.Tipo);
            if (ticket.PrecioPagado == 0 || ticket.UsoCupon)
            {
                TempData["Error"] = "Los tickets adquiridos con cupón de devolución no pueden ser devueltos.";
                return RedirectToAction("MisTickets");
            }

            if (categoria != null)
            {
                // LE DEVOLVEMOS EL CUPO AL EVENTO
                categoria.Capacidad += 1;
                _context.Update(categoria);
            }
            // ----------------------------------------------

            // 3. Creamos la reclamación con el Motivo (para que no de error SQL)
            var devolucion = new Devolucion
            {
                TicketId = ticketId,
                UsuarioId = _userManager.GetUserId(User),
                TipoDevolucion = tipoDevolucion,
                Estado = "Completada",
                FechaSolicitud = DateTime.Now,
                Motivo = "Devolución y liberación de cupo (" + tipoDevolucion + ")"
            };

            // 4. Inhabilitamos el ticket
            ticket.Estado = "Devuelto";

            if (tipoDevolucion == "Cupon")
            {
                devolucion.CodigoCupon = "REFUND-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                devolucion.MontoOriginal = ticket.PrecioPagado;
                devolucion.MontoRestante = ticket.PrecioPagado;  // ✅ Saldo inicial = precio pagado
                devolucion.FechaExpiracion = DateTime.Now.AddDays(30); // ✅ Expira en 30 días
            }

            _context.Devoluciones.Add(devolucion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ticket devuelto. El cupo ha sido liberado para la venta.";
            return RedirectToAction("MisTickets");
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
            if (ticket.PrecioPagado == 0)
            {
                TempData["Error"] = "Los tickets adquiridos con cupón de devolución no pueden ser devueltos.";
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
                .Include(t => t.Devolucion) // <--- ESTA LÍNEA ES LA QUE HACE QUE SE VEA EL CUPÓN
                .Where(t => t.UsuarioId == userId)
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