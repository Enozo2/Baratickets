using Baratickets2._0.Data;
using Baratickets2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

namespace Baratickets2._0.Controllers
{
    // Mantenemos el Authorize general, pero usaremos AllowAnonymous en las vistas públicas
    [Authorize(Roles = "Organizador,Admin")]
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

            // 1. Definimos la consulta base
            var consulta = _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.CategoriasTickets); // <--- ESTO ES LO QUE FALTABA

            // 2. Si es Admin, ve todo. Si no, solo lo suyo.
            if (User.IsInRole("Admin"))
            {
                return View(await consulta.ToListAsync());
            }
            else
            {
                return View(await consulta.Where(e => e.OrganizadorId == userId).ToListAsync());
            }
        }
        // --- CORRECCIÓN: DETALLES PÚBLICOS ---
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos
          .Include(e => e.Organizador)
          .Include(e => e.CategoriasTickets) // <--- ESTA LÍNEA ES OBLIGATORIA
          .Include(e => e.Tickets)
          .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            return View(evento);
        }
        // GET: Eventos/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Evento evento)
        {
            // 1. Asignamos el ID del organizador
            var userId = _userManager.GetUserId(User);
            evento.OrganizadorId = userId;

            // 2. Limpieza de validaciones
            ModelState.Remove("Organizador");
            ModelState.Remove("Tickets");
            ModelState.Remove("OrganizadorId");

            if (ModelState.IsValid)
            {
                // --- 🟢 LA SOLUCIÓN ESTÁ AQUÍ ---
                // Al agregar el 'evento', EF detecta automáticamente las 'CategoriasTickets'
                // que vienen dentro del objeto y les asigna el EventoId solo.

                _context.Add(evento);

                // Un solo SaveChanges para todo el paquete (Evento + Categorías)
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(evento);
        }
        [Authorize(Roles = "Organizador,Admin")]
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

    // IMPORTANTE: Aquí es donde cargamos las boletas para que se vean en la lista
    var evento = await _context.Eventos
        .Include(e => e.CategoriasTickets) 
        .FirstOrDefaultAsync(m => m.Id == id);

    if (evento == null) return NotFound();
    
    return View(evento);
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Evento evento)
        {
            if (id != evento.Id) return NotFound();

            // 1. Buscamos el evento original en la DB
            var eventoOriginal = await _context.Eventos
                .Include(e => e.CategoriasTickets)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventoOriginal == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. ACTUALIZACIÓN: Agregamos la línea de Institución aquí
                    eventoOriginal.Nombre = evento.Nombre;
                    eventoOriginal.Organizacion = evento.Organizacion; // <--- ESTO ES LO NUEVO
                    eventoOriginal.Descripcion = evento.Descripcion;
                    eventoOriginal.Direccion = evento.Direccion;
                    eventoOriginal.FechaEvento = evento.FechaEvento;
                    eventoOriginal.ImagenUrl = evento.ImagenUrl;

                    // 3. Sincronizamos las categorías (como hicimos antes)
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
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventoExists(evento.Id)) return NotFound();
                    else throw;
                }
            }
            return View(evento);
        }
        // GET: Eventos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos
                .Include(e => e.Organizador)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evento == null) return NotFound();

            return View(evento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento != null)
            {
                _context.Eventos.Remove(evento);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EventoExists(int id)
        {
            return _context.Eventos.Any(e => e.Id == id);
        }
    }
}