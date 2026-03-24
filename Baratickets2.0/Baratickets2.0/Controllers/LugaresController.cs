using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // <--- Este quita el error de 'ToListAsync'
using Baratickets2._0.Models;      // <--- Este quita el error de 'Lugar'
using Baratickets2._0.Data;        // <--- Para el ApplicationDbContext
using Microsoft.AspNetCore.Authorization;

namespace Baratickets2._0.Controllers
{
    [Authorize(Roles = "Admin")] // 🔐 Solo el Gran Jefe entra aquí
    public class LugaresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LugaresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA DE LUGARES PARA EL ADMIN
        public async Task<IActionResult> Index()
        {
            return View(await _context.Lugares.ToListAsync());
        }

        // CREAR LUGAR (GET)
        public IActionResult Create() => View();

        // CREAR LUGAR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lugar lugar)
        {
            // ✅ Limpiar errores de navegación que no vienen del form
            ModelState.Remove("Eventos");
            ModelState.Remove("SolicitudesAlquiler");

            if (ModelState.IsValid)
            {
                _context.Add(lugar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // DEBUG — ver qué falla
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine("ERROR: " + error.ErrorMessage);
            }

            return View(lugar);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReporteUso(int? mes, int? anio)
        {
            var mesActual = mes ?? DateTime.Now.Month;
            var anioActual = anio ?? DateTime.Now.Year;
            var inicioMes = new DateTime(anioActual, mesActual, 1);
            var inicioMesSiguiente = inicioMes.AddMonths(1);

            ViewBag.MesSeleccionado = mesActual;
            ViewBag.AnioSeleccionado = anioActual;

            var lugares = await _context.Lugares.ToListAsync();

            var eventosMes = await _context.Eventos
                .Include(e => e.Tickets)
                .Where(e => e.FechaInicio >= inicioMes
                         && e.FechaInicio < inicioMesSiguiente
                         && e.EstadoEvento == "Publicado")
                .ToListAsync();

            var solicitudesAprobadas = await _context.SolicitudesAlquiler
                .Where(s => s.Estado == "Aprobado")
                .ToListAsync();

            var solicitudesAprobadasMes = solicitudesAprobadas
                .Where(s => s.FechaInicio >= inicioMes && s.FechaInicio < inicioMesSiguiente)
                .ToList();

            // Eventos explícitamente vinculados a alquiler (cuando EventoId está presente)
            var eventosDeAlquilerIds = solicitudesAprobadas
                .Where(s => s.EventoId.HasValue)
                .Select(s => s.EventoId!.Value)
                .ToHashSet();

            // Respaldo: detectar eventos creados desde alquiler aunque EventoId se haya limpiado
            var eventosDetectadosComoAlquilerIds = eventosMes
                .Where(e => solicitudesAprobadas.Any(s =>
                    s.TipoEventoAlquiler == "Publico" &&
                    s.ClienteId == e.OrganizadorId &&
                    s.LugarId == e.LugarId &&
                    e.FechaInicio >= s.FechaInicio &&
                    e.FechaFin <= s.FechaFin))
                .Select(e => e.Id)
                .ToHashSet();

            var reporteFinal = lugares.Select(l =>
            {
                var eventosPropios = eventosMes
                    .Where(e => e.LugarId == l.Id
                             && !eventosDeAlquilerIds.Contains(e.Id)
                             && !eventosDetectadosComoAlquilerIds.Contains(e.Id))
                    .Select(e => new { e.Id, Nombre = e.Nombre })
                    .DistinctBy(e => e.Id)
                    .ToList();

                var alquileresAprobados = solicitudesAprobadasMes
                    .Where(s => s.LugarId == l.Id)
                    .Select(s => new { s.Id, Nombre = s.NombreEvento })
                    .DistinctBy(s => s.Id)
                    .ToList();

                var eventosPropiosIds = eventosPropios.Select(e => e.Id).ToHashSet();

                var gananciaTickets = eventosMes
                    .Where(e => eventosPropiosIds.Contains(e.Id))
                    .SelectMany(e => e.Tickets ?? new List<Ticket>())
                    .Where(t => t.Estado != "Devuelto"
                             && t.FechaCompra >= inicioMes
                             && t.FechaCompra < inicioMesSiguiente)
                    .Sum(t => t.PrecioPagado);

                var gananciaAlquileres = solicitudesAprobadasMes
                    .Where(s => s.LugarId == l.Id)
                    .Sum(s => s.MontoAlquiler);

                return new
                {
                    NombreRecinto = l.Nombre,
                    EventosPropios = eventosPropios,
                    AlquileresAprobados = alquileresAprobados,
                    CantidadEventos = eventosPropios.Count,
                    CantidadAlquileres = alquileresAprobados.Count,
                    GananciaTickets = gananciaTickets,
                    GananciaAlquileres = gananciaAlquileres,
                    GananciaTotal = gananciaTickets + gananciaAlquileres
                };
            }).ToList();

            ViewBag.GananciaEventos = reporteFinal.Sum(r => r.GananciaTickets);
            ViewBag.GananciaAlquileres = reporteFinal.Sum(r => r.GananciaAlquileres);
            ViewBag.GananciaTotal = reporteFinal.Sum(r => r.GananciaTotal);

            return View(reporteFinal);
        }
        [Authorize(Roles = "Admin")] // Solo el Admin puede borrar recintos
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lugar = await _context.Lugares
                .Include(l => l.Eventos) // Cargamos los eventos para verificar
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lugar == null) return NotFound();

            // 🛡️ VALIDACIÓN DE SEGURIDAD
            if (lugar.Eventos != null && lugar.Eventos.Any())
            {
                // Si tiene eventos, no lo borramos, mejor enviamos un error
                TempData["Error"] = "No se puede eliminar el recinto porque tiene eventos programados. Primero elimina los eventos.";
                return RedirectToAction(nameof(Index));
            }

            _context.Lugares.Remove(lugar);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Recinto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ACCIÓN RÁPIDA: CAMBIAR ESTADO (Activar/Desactivar)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var lugar = await _context.Lugares.FindAsync(id);
            if (lugar == null) return NotFound();
            return View(lugar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Lugar model)
        {
            if (id != model.Id) return NotFound();

            // ✅ Limpiar errores de navegación
            ModelState.Remove("Eventos");
            ModelState.Remove("SolicitudesAlquiler");

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Recinto actualizado correctamente.";
                return RedirectToAction("Index");
            }

            return View(model);
        }
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var lugar = await _context.Lugares.FindAsync(id);
            if (lugar != null)
            {
                lugar.EstaActivo = !lugar.EstaActivo; // Si es true pasa a false, y viceversa
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
