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
            ModelState.Remove("Eventos");

            if (ModelState.IsValid)
            {
                lugar.EstaActivo = true; // Por defecto nace activo
                _context.Add(lugar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lugar);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReporteUso()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            var reporte = await _context.Lugares
                .Select(l => new {
                    NombreRecinto = l.Nombre,
                    CantidadEventos = l.Eventos.Count(e => e.FechaInicio >= inicioMes && e.FechaInicio <= finMes),
                    Eventos = l.Eventos.Where(e => e.FechaInicio >= inicioMes && e.FechaInicio <= finMes).ToList()
                })
                .ToListAsync();

            return View(reporte);
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
