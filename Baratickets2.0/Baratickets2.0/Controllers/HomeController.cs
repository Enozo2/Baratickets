using Baratickets2._0.Data; // Importante para encontrar ApplicationDbContext
using Baratickets2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Baratickets2._0.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // Inyectamos el contexto en el constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 1. Agregamos el parámetro 'buscar' para recibir el texto de la vista
        public async Task<IActionResult> Index(string buscar)
        {
            // 1. Iniciamos la consulta cargando las relaciones (Eager Loading)
            // Agregamos .Include(e => e.Lugar) para quitar el "Recinto por confirmar"
            // Agregamos .Include(e => e.CategoriasTickets) para quitar el "TBA" en los precios
            var consulta = _context.Eventos
                .Include(e => e.Lugar)
                .Include(e => e.CategoriasTickets)
                .AsQueryable();

            consulta = consulta.Where(e => e.FechaInicio >= DateTime.Today);
            // ? Solo mostrar eventos publicados al público
            consulta = consulta.Where(e => e.EstadoEvento == "Publicado");

            // 3. Lógica de búsqueda mejorada
            if (!string.IsNullOrEmpty(buscar))
            {
                string busquedaLower = buscar.ToLower();

                consulta = consulta.Where(s =>
                    s.Nombre.ToLower().Contains(busquedaLower) ||
                    s.Descripcion.ToLower().Contains(busquedaLower) ||
                    // ? Ahora también pueden buscar por el nombre del Pabellón/Lugar
                    (s.Lugar != null && s.Lugar.Nombre.ToLower().Contains(busquedaLower))
                );
            }

            // 4. Ordenamos por fecha de inicio y enviamos a la vista
            return View(await consulta.OrderBy(e => e.FechaInicio).ToListAsync());
        }
        [Authorize(Roles = "Admin,Validador")]
        public IActionResult ValidarEntrada()
        {
            return View(); // Al estar en Home, lo encontrará sin problemas
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}