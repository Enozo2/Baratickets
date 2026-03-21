using Baratickets2._0.Data;
using Baratickets2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
// teo la cabra illuminati

namespace Baratickets2._0.Controllers
{
    // Mantenemos el Authorize general, pero usaremos AllowAnonymous en las vistas públicas
    [Authorize(Roles = "Organizador,Admin,OrganizadorTemporal")]
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Eventos (Gestión para Organizadores/Admins)
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var esAdmin = User.IsInRole("Admin");
            var esOrganizador = User.IsInRole("Organizador");
            var esOrganizadorTemporal = User.IsInRole("OrganizadorTemporal");

            IQueryable<Evento> query = _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.Lugar)
                .Include(e => e.CategoriasTickets);

            if (esAdmin)
            {
                // El Admin es el único que ve absolutamente TODO (oficiales y alquileres)
            }
            else if (esOrganizador)
            {
                // ✅ FILTRO PARA EL ORGANIZADOR PROFESIONAL:
                // Solo ve sus propios eventos O eventos que no sean alquileres privados.
                // Asumiendo que usas 'EsAlquiler' o puedes filtrar por el rol del creador.
                query = query.Where(e => e.OrganizadorId == userId || e.EstadoEvento == "Publicado");
            }
            else if (esOrganizadorTemporal)
            {
                // Solo sus propios eventos (El cliente que alquiló)
                query = query.Where(e => e.OrganizadorId == userId);
            }
            else
            {
                // Para cualquier otro (Clientes normales que no alquilaron)
                query = query.Where(e => e.EstadoEvento == "Publicado");
            }

            var eventos = await query.ToListAsync();

            ViewBag.TieneEventoAlquiler = await _context.SolicitudesAlquiler
                .AnyAsync(s => s.ClienteId == userId &&
                          s.Estado == "Aprobado" &&
                          s.EventoId != null);

            return View(eventos);
        }
        // --- CORRECCIÓN: DETALLES PÚBLICOS ---
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos
          .Include(e => e.Organizador)
          .Include(e => e.Lugar)
          .Include(e => e.CategoriasTickets) // <--- ESTA LÍNEA ES OBLIGATORIA
          .Include(e => e.Tickets)
          .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            return View(evento);
        }
        // GET: Eventos/Create
        // ✅ ACCIÓN PARA PRELLENAR EL FORMULARIO DESDE UN ALQUILER
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CrearDesdeAlquiler(int id)
        {
            var solicitud = await _context.SolicitudesAlquiler
                .Include(s => s.Lugar)
                .FirstOrDefaultAsync(s => s.Id == id && s.Estado == "Aprobado");

            if (solicitud == null) return NotFound();

            var nuevoEvento = new Evento
            {
                Nombre = solicitud.NombreEvento, // Ajustado a tu modelo
                LugarId = solicitud.LugarId,
                Descripcion = solicitud.DescripcionEvento ?? "Evento privado.",
                Organizacion = User.Identity.Name
            };
            ViewBag.EsDesdeAlquiler = true;
            // 💡 PASAMOS LOS DATOS A VIEWGBAG PARA LOS INPUTS MANUALES
            ViewBag.FechaAprobada = solicitud.FechaInicio.ToString("yyyy-MM-dd");
            ViewBag.HoraInicioAprobada = solicitud.FechaInicio.ToString("HH:mm");
            ViewBag.HoraFinAprobada = solicitud.FechaFin.ToString("HH:mm");

            ViewBag.LugarId = new SelectList(_context.Lugares.Where(l => l.EstaActivo), "Id", "Nombre", solicitud.LugarId);
            ViewBag.EsAlquiler = true; // Flag para saber que venimos de un alquiler

            return View("Create", nuevoEvento);
        }
        public IActionResult Create()
        {
            var lugares = _context.Lugares.Where(l => l.EstaActivo).ToList();
            ViewBag.LugarId = new SelectList(lugares, "Id", "Nombre");

            // ✅ Prellenar datos del alquiler si vienen de una solicitud
            if (TempData["SolicitudFecha"] != null)
            {
                ViewBag.SolicitudFecha = TempData["SolicitudFecha"];
                ViewBag.SolicitudHoraInicio = TempData["SolicitudHoraInicio"];
                ViewBag.SolicitudHoraFin = TempData["SolicitudHoraFin"];
                ViewBag.SolicitudNombre = TempData["SolicitudNombreEvento"];
                ViewBag.SolicitudDescripcion = TempData["SolicitudDescripcion"];
                ViewBag.SolicitudLugarId = TempData["SolicitudLugarId"];
                TempData.Keep();
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Evento evento, string fechaSolo, string horaInicio, string horaFin)
        {
            // 1. Asignación de Identidad
            var userId = _userManager.GetUserId(User);
            evento.OrganizadorId = userId;

            // 2. 🧩 RECONSTRUCCIÓN MANUAL DE FECHAS
            if (!string.IsNullOrEmpty(fechaSolo) && !string.IsNullOrEmpty(horaInicio) && !string.IsNullOrEmpty(horaFin))
            {
                try
                {
                    evento.FechaInicio = DateTime.Parse($"{fechaSolo} {horaInicio}");
                    evento.FechaFin = DateTime.Parse($"{fechaSolo} {horaFin}");
                    evento.FechaEvento = evento.FechaInicio;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "El formato de fecha u hora es incorrecto.");
                }
            }

            // 3. 🧹 LIMPIEZA DE MODELSTATE
            ModelState.Remove("Organizador");
            ModelState.Remove("Tickets");
            ModelState.Remove("OrganizadorId");
            ModelState.Remove("FechaEvento");
            ModelState.Remove("Lugar");
            ModelState.Remove("Direccion");

            // 4. 🛡️ PROCESO DE GUARDADO
            if (ModelState.IsValid)
            {
                try
                {
                    // Validar choque de horario
                    bool lugarOcupado = await _context.Eventos.AnyAsync(e =>
                        e.LugarId == evento.LugarId &&
                        evento.FechaInicio < e.FechaFin &&
                        evento.FechaFin > e.FechaInicio
                    );

                    if (lugarOcupado)
                    {
                        ModelState.AddModelError("", "¡Atención! Este recinto ya tiene un evento en ese horario.");
                    }
                    else
                    {
                        var usuario = await _userManager.GetUserAsync(User);

                        // ✅ CORRECCIÓN: Lógica unificada para OrganizadorTemporal
                        bool esOrganizadorTemporal = User.IsInRole("OrganizadorTemporal") &&
                            await _context.SolicitudesAlquiler.AnyAsync(s => s.ClienteId == usuario.Id && s.Estado == "Aprobado");

                        if (esOrganizadorTemporal)
                        {
                            var solicitudAprobada = await _context.SolicitudesAlquiler
                                .FirstOrDefaultAsync(s => s.ClienteId == usuario.Id && s.Estado == "Aprobado");

                            // Verificar que no haya creado ya un evento
                            if (solicitudAprobada != null && solicitudAprobada.EventoId != null)
                            {
                                TempData["Error"] = "Ya tienes un evento creado para tu alquiler aprobado.";
                                ViewBag.LugarId = new SelectList(_context.Lugares.Where(l => l.EstaActivo), "Id", "Nombre", evento.LugarId);
                                return View(evento);
                            }

                            evento.EstadoEvento = "PendienteAprobacion";
                            _context.Add(evento);
                            await _context.SaveChangesAsync();

                            // Vincular el evento a la solicitud
                            if (solicitudAprobada != null)
                            {
                                solicitudAprobada.EventoId = evento.Id;
                                _context.Update(solicitudAprobada);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            evento.EstadoEvento = "Publicado";
                            _context.Add(evento);
                            await _context.SaveChangesAsync();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la BD: " + ex.Message);
                }
            }

            // 5. 🔄 RECARGA DE SEGURIDAD
            ViewBag.LugarId = new SelectList(_context.Lugares.Where(l => l.EstaActivo), "Id", "Nombre", evento.LugarId);
            return View(evento);
        }
        [HttpGet]
        [Authorize(Roles = "OrganizadorTemporal")]
        public async Task<IActionResult> MiEvento()
        {
            var userId = _userManager.GetUserId(User);

            var eventos = await _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.Lugar)
                .Include(e => e.CategoriasTickets)
                .Where(e => e.OrganizadorId == userId)
                .ToListAsync();

            ViewBag.TieneEventoAlquiler = await _context.SolicitudesAlquiler
                .AnyAsync(s => s.ClienteId == userId &&
                          s.Estado == "Aprobado" &&
                          s.EventoId != null);

            return View(eventos);
        }
        // ✅ Ver eventos pendientes de aprobación (Admin/Organizador)
        [HttpGet]

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EventosPendientes()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.Lugar)
                .Where(e => e.EstadoEvento == "PendienteAprobacion")
                .OrderByDescending(e => e.FechaInicio)
                .ToListAsync();

            return View(eventos);
        }

        // ✅ Aprobar evento
        [HttpPost]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> AprobarEvento(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            // 🛡️ VALIDACIÓN DE SEGURIDAD: Si ya está publicado, no hagas nada
            if (evento.EstadoEvento == "Publicado")
            {
                TempData["Error"] = "Este evento ya ha sido aprobado anteriormente.";
                return RedirectToAction("EventosPendientes");
            }

            evento.EstadoEvento = "Publicado";

            // Asegúrate de que el evento sea visible en la cartelera general
            // Si tienes un campo bool como 'EsVisible', cámbialo aquí también

            _context.Update(evento); // Es buena práctica marcarlo para Update
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Evento '{evento.Nombre}' aprobado y publicado correctamente.";
            return RedirectToAction("EventosPendientes");
        }

        // ✅ Rechazar evento
        [HttpPost]
        [Authorize(Roles = "Admin,Organizador")]
        public async Task<IActionResult> RechazarEvento(int id, string motivoRechazo)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            evento.EstadoEvento = "Rechazado";
            evento.MotivoRechazo = motivoRechazo;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Evento rechazado correctamente.";
            return RedirectToAction("EventosPendientes");
        }
        [Authorize(Roles = "Organizador,Admin,OrganizadorTemporal")]
        public async Task<IActionResult> Dashboard(int id)
        {
            // Cargamos el evento con sus categorías y tickets
            var evento = await _context.Eventos
                .Include(e => e.CategoriasTickets)
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            // 1. Separamos tickets activos de devueltos
            var ticketsActivos = evento.Tickets.Where(t => t.Estado != "Devuelto").ToList();
            var ticketsDevueltos = evento.Tickets.Where(t => t.Estado == "Devuelto").ToList();

            // 2. Cálculo de ingresos (Usando el precio real guardado en el ticket)
            // Esto es mucho mejor porque si el precio cambió después de la compra, el histórico se mantiene
            decimal totalVendido = ticketsActivos.Sum(t => t.PrecioPagado);
            decimal dineroPerdidoDevoluciones = ticketsDevueltos.Sum(t => t.PrecioPagado);
            // Obtener nombres de usuarios de las devoluciones
         

            // 3. Pasar datos básicos a la vista
            ViewBag.TotalVendido = totalVendido;
            ViewBag.TicketsVendidos = ticketsActivos.Count;
            ViewBag.TotalDevueltos = ticketsDevueltos.Count;
            ViewBag.DineroDevuelto = dineroPerdidoDevoluciones;
         

            // 4. Estadísticas de validación (Asistencia)
            ViewBag.AsistenciaReal = ticketsActivos.Count(t => t.FueUsado);
            ViewBag.Pendientes = ticketsActivos.Count(t => !t.FueUsado);

            ViewBag.PorcentajeAsistencia = ticketsActivos.Any()
                ? (double)ViewBag.AsistenciaReal / ticketsActivos.Count * 100
                : 0;

            // 5. NUEVO: Ventas por Categoría (Esto impresionará al profesor)
            // Agrupamos los tickets por su tipo (Nombre de categoría)
            var ventasPorCategoria = ticketsActivos
                .GroupBy(t => t.Tipo)
                .Select(g => new {
                    Nombre = g.Key,
                    Cantidad = g.Count(),
                    Subtotal = g.Sum(t => t.PrecioPagado)
                }).ToList();

            ViewBag.VentasPorCategoria = ventasPorCategoria;
            // 6. Historial de devoluciones con cupones
            var devoluciones = await _context.Devoluciones
               .Include(d => d.Ticket)
               .Where(d => d.Ticket.EventoId == id)
               .OrderByDescending(d => d.FechaSolicitud)
               .Select(d => new {
                   UsuarioId = d.UsuarioId,
                   FechaSolicitud = d.FechaSolicitud,
                   TipoDevolucion = d.TipoDevolucion,
                   MontoOriginal = d.MontoOriginal,
                   MontoRestante = d.MontoRestante,
                   CodigoCupon = d.CodigoCupon,
                   CuponUsado = d.CuponUsado,
                   // ✅ Convertir a string directamente — evita el problema de .Value en Razor
                   FechaExpiracionStr = d.FechaExpiracion.HasValue
                       ? d.FechaExpiracion.Value.ToString("dd/MM/yyyy")
                       : "—",
                   FechaExpiracionEsPasada = d.FechaExpiracion.HasValue
                       ? d.FechaExpiracion.Value < DateTime.Now
                       : false,
                   Estado = d.Estado
               })
               .ToListAsync();
            // Obtener nombres de usuarios
            var usuarioIds = devoluciones.Select(d => d.UsuarioId).Distinct().ToList();
            var usuarios = await _context.Users
                .Where(u => usuarioIds.Contains(u.Id))
                .ToListAsync();

            var usuariosDict = usuarios.ToDictionary(u => u.Id, u => u.NombreCompleto);

            ViewBag.Devoluciones = devoluciones;
            ViewBag.UsuariosDevoluciones = usuariosDict;

            return View(evento);
        }

        // GET: Eventos/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos
                .Include(e => e.CategoriasTickets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            // Cargar los lugares para el Dropdown
            ViewBag.LugarId = new SelectList(_context.Lugares.Where(l => l.EstaActivo), "Id", "Nombre", evento.LugarId);

            return View(evento);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. Agregamos los 3 strings al paréntesis para recibir los datos del formulario
        public async Task<IActionResult> Edit(int id, Evento evento, string fechaSolo, string horaInicio, string horaFin)
        {
            if (id != evento.Id) return NotFound();

            // Buscamos el original
            var eventoOriginal = await _context.Eventos
                .Include(e => e.CategoriasTickets)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventoOriginal == null) return NotFound();

            // 2. Limpieza de validaciones obsoletas (Evita el error del 0001)
            ModelState.Remove("Organizador");
            ModelState.Remove("Tickets");
            ModelState.Remove("FechaEvento"); // Quitamos la validación del campo viejo

            if (ModelState.IsValid)
            {
                try
                {
                    // 3. RECONSTRUCCIÓN: Unimos los pedazos editados
                    if (!string.IsNullOrEmpty(fechaSolo) && !string.IsNullOrEmpty(horaInicio))
                    {
                        eventoOriginal.FechaInicio = DateTime.Parse($"{fechaSolo} {horaInicio}");
                        eventoOriginal.FechaFin = DateTime.Parse($"{fechaSolo} {horaFin}");
                        // Si aún usas FechaEvento para algo, actualízalo también:
                        eventoOriginal.FechaEvento = eventoOriginal.FechaInicio;
                    }

                    // 4. ACTUALIZACIÓN de los demás campos
                    eventoOriginal.Nombre = evento.Nombre;
                    eventoOriginal.Organizacion = evento.Organizacion;
                    eventoOriginal.Descripcion = evento.Descripcion;
                    eventoOriginal.LugarId = evento.LugarId;
                    eventoOriginal.ImagenUrl = evento.ImagenUrl;

                    // 5. Sincronizamos las categorías
                    if (evento.CategoriasTickets != null)
                    {
                        _context.CategoriasTickets.RemoveRange(eventoOriginal.CategoriasTickets);
                        foreach (var cat in evento.CategoriasTickets)
                        {
                            cat.EventoId = id;
                            cat.Id = 0;
                            _context.CategoriasTickets.Add(cat);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                }
            }
            return View(evento);
        }
        // GET: Eventos/Delete/5
        [Authorize(Roles = "Admin,Organizador,OrganizadorTemporal")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.Lugar) // Agregamos esto para que en la confirmación se vea el lugar
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            // SEGURIDAD: Solo el dueño o el Admin pasan
            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && evento.OrganizadorId != userId)
            {
                return Forbid();
            }

            return View(evento);
        }


        // POST: Eventos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizador,Admin,OrganizadorTemporal")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.CategoriasTickets)
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && evento.OrganizadorId != userId)
                return Forbid();
            try
            {
                // 0. ✅ Desvincular solicitud de alquiler y quitar rol Organizador
                var solicitudVinculada = await _context.SolicitudesAlquiler
                    .Include(s => s.Cliente)
                    .FirstOrDefaultAsync(s => s.EventoId == id);

                if (solicitudVinculada != null)
                {
                    solicitudVinculada.EventoId = null;
                    _context.Update(solicitudVinculada);

                    if (solicitudVinculada.Cliente != null)
                    {
                        var esOrganizadorReal = await _context.Eventos
                            .AnyAsync(e => e.OrganizadorId == solicitudVinculada.ClienteId
                                        && e.Id != id);

                        if (solicitudVinculada.Cliente != null)
                        {
                            await _userManager.RemoveFromRoleAsync(solicitudVinculada.Cliente, "OrganizadorTemporal");
                            await _userManager.UpdateSecurityStampAsync(solicitudVinculada.Cliente);
                        }
                    }
                } // ← Cierra el if de solicitudVinculada

                // 1. Borrar devoluciones de los tickets del evento
                if (evento.Tickets != null && evento.Tickets.Any())
                {
                    var ticketIds = evento.Tickets.Select(t => t.Id).ToList();
                    await _context.Devoluciones
                        .Where(d => ticketIds.Contains(d.TicketId))
                        .ExecuteDeleteAsync();
                }

                // 2. Borrar tickets
                if (evento.Tickets != null && evento.Tickets.Any())
                    _context.Tickets.RemoveRange(evento.Tickets);

                // 3. Borrar categorías
                if (evento.CategoriasTickets != null && evento.CategoriasTickets.Any())
                    _context.CategoriasTickets.RemoveRange(evento.CategoriasTickets);

                // 4. Borrar evento
                _context.Eventos.Remove(evento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "No se pudo eliminar el evento: " + ex.Message);
                return View(evento);
            }
        }
    }
}