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
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            ViewBag.MesSeleccionado = mesActual;
            ViewBag.AnioSeleccionado = anioActual;

            // ✅ Obtener IDs de eventos vinculados a alquileres
            var eventosDeAlquiler = await _context.SolicitudesAlquiler
                .Where(s => s.EventoId != null)
                .Select(s => s.EventoId)
                .ToListAsync();

            var reporte = await _context.Lugares
                .Select(l => new {
                    NombreRecinto = l.Nombre,
                    // ✅ Excluir eventos del organizador temporal
                    EventosPropios = l.Eventos
                        .Where(e => e.FechaInicio >= inicioMes && e.FechaInicio <= finMes
                            && !eventosDeAlquiler.Contains(e.Id))
                        .Select(e => new { Nombre = e.Nombre })
                        .ToList(),
                    AlquileresAprobados = l.SolicitudesAlquiler
                        .Where(s => s.Estado == "Aprobado" &&
                               s.FechaInicio >= inicioMes &&
                               s.FechaInicio <= finMes)
                        .Select(s => new { Nombre = s.NombreEvento })
                        .ToList(),
                    // ✅ Ganancias de tickets solo de eventos propios
                    GananciaTickets = l.Eventos
                        .Where(e => e.FechaInicio >= inicioMes && e.FechaInicio <= finMes
                            && !eventosDeAlquiler.Contains(e.Id))
                        .SelectMany(e => e.Tickets)
                        .Where(t => t.Estado != "Devuelto")
                        .Sum(t => (decimal?)t.PrecioPagado) ?? 0,
                    GananciaAlquileres = l.SolicitudesAlquiler
                        .Where(s => s.Estado == "Aprobado" &&
                               s.FechaInicio >= inicioMes &&
                               s.FechaInicio <= finMes)
                        .Sum(s => (decimal?)s.MontoAlquiler) ?? 0
                })
                .ToListAsync();

            var reporteFinal = reporte.Select(r => new {
                r.NombreRecinto,
                r.EventosPropios,
                r.AlquileresAprobados,
                CantidadEventos = r.EventosPropios.Count,
                CantidadAlquileres = r.AlquileresAprobados.Count,
                r.GananciaTickets,
                r.GananciaAlquileres,
                GananciaTotal = r.GananciaTickets + r.GananciaAlquileres
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
