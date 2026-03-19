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
