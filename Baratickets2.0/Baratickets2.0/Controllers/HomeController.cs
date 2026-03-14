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
            // Cambiamos DateTime.Now por .Date para que incluya todo el día de hoy
            // O simplemente comenta el filtro si quieres ver absolutamente todo lo que creas
            var consulta = from e in _context.Eventos
                           where e.FechaEvento >= DateTime.Today 
                           select e;

            if (!string.IsNullOrEmpty(buscar))
            {
                string busquedaLower = buscar.ToLower();

                consulta = consulta.Where(s => s.Nombre.ToLower().Contains(busquedaLower) ||
                                               s.Direccion.ToLower().Contains(busquedaLower) ||
                                               s.Descripcion.ToLower().Contains(busquedaLower));
            }

            return View(await consulta.OrderBy(e => e.FechaEvento).ToListAsync());
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