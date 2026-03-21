using Baratickets2._0.Data;
using Baratickets2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baratickets2._0.Controllers
{
    [Authorize]
    public class AlquileresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AlquileresController(ApplicationDbContext context,
                                    UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ GET: Formulario de solicitud
        [HttpGet]
        public async Task<IActionResult> Solicitar()
        {
            ViewBag.Lugares = await _context.Lugares.Where(l => l.EstaActivo).ToListAsync();
            return View();
        }

        // ✅ POST: Guardar solicitud y redirigir al pago
        [HttpPost]
        public async Task<IActionResult> Solicitar(SolicitudAlquiler model)
        {
            var userId = _userManager.GetUserId(User);

            ModelState.Remove("Cliente");
            ModelState.Remove("Lugar");
            ModelState.Remove("Evento");
            ModelState.Remove("Orden");

            // 1. Validar conflicto de horario
            var conflicto = await _context.SolicitudesAlquiler
                .AnyAsync(s => s.LugarId == model.LugarId
                    && s.Estado != "Rechazado"
                    && s.FechaInicio < model.FechaFin
                    && s.FechaFin > model.FechaInicio);

            if (conflicto)
            {
                TempData["Error"] = "El recinto ya está reservado en ese horario.";
                ViewBag.Lugares = await _context.Lugares.Where(l => l.EstaActivo).ToListAsync();
                return View(model);
            }

            // 2. Validar anticipación mínima de 3 días
            if (model.FechaInicio < DateTime.Now.AddDays(3))
            {
                TempData["Error"] = "La solicitud debe hacerse con al menos 3 días de anticipación.";
                ViewBag.Lugares = await _context.Lugares.Where(l => l.EstaActivo).ToListAsync();
                return View(model);
            }

            // 3. Validar duración máxima de 12 horas
            if ((model.FechaFin - model.FechaInicio).TotalHours > 12)
            {
                TempData["Error"] = "La duración máxima del alquiler es 12 horas.";
                ViewBag.Lugares = await _context.Lugares.Where(l => l.EstaActivo).ToListAsync();
                return View(model);
            }

            // 4. Calcular monto según precio del pabellón
            var lugar = await _context.Lugares.FindAsync(model.LugarId);
            double horas = (model.FechaFin - model.FechaInicio).TotalHours;
            model.MontoAlquiler = (decimal)horas * lugar.PrecioPorHora;
            model.ClienteId = userId;
            model.Estado = "Pendiente";
            model.FechaSolicitud = DateTime.Now;

            // 5. Guardar solicitud temporalmente en sesión para el pago
            TempData["SolicitudLugarId"] = model.LugarId;
            TempData["SolicitudFechaInicio"] = model.FechaInicio.ToString("o");
            TempData["SolicitudFechaFin"] = model.FechaFin.ToString("o");
            TempData["SolicitudMonto"] = model.MontoAlquiler.ToString();
            TempData["SolicitudNombreEvento"] = model.NombreEvento;
            TempData["SolicitudDescripcion"] = model.DescripcionEvento;
            TempData["SolicitudLugarNombre"] = lugar.Nombre;

            TempData["SolicitudNombreEvento"] = model.NombreEvento;
            TempData["SolicitudDescripcion"] = model.DescripcionEvento;
            TempData["SolicitudFecha"] = model.FechaInicio.ToString("yyyy-MM-dd");
            TempData["SolicitudHoraInicio"] = model.FechaInicio.ToString("HH:mm");
            TempData["SolicitudHoraFin"] = model.FechaFin.ToString("HH:mm");
            TempData["SolicitudLugarId"] = model.LugarId;

            return RedirectToAction("ProcesarPago");
        }

        // ✅ GET: Pantalla de pago
        [HttpGet]
        public IActionResult ProcesarPago()
        {
            if (TempData["SolicitudMonto"] == null)
                return RedirectToAction("Solicitar");

            ViewBag.Monto = decimal.Parse(TempData["SolicitudMonto"].ToString());
            ViewBag.LugarNombre = TempData["SolicitudLugarNombre"]?.ToString();
            ViewBag.NombreEvento = TempData["SolicitudNombreEvento"]?.ToString();

            // Mantener TempData para el POST
            TempData.Keep();

            return View();
        }

        // ✅ POST: Confirmar pago y crear solicitud
        [HttpPost]
        public async Task<IActionResult> ProcesarPago(string emailConfirmacion)
        {
            var userId = _userManager.GetUserId(User);

            if (TempData["SolicitudMonto"] == null)
                return RedirectToAction("Solicitar");

            var monto = decimal.Parse(TempData["SolicitudMonto"].ToString());
            var lugarId = int.Parse(TempData["SolicitudLugarId"].ToString());
            var fechaInicio = DateTime.Parse(TempData["SolicitudFechaInicio"].ToString());
            var fechaFin = DateTime.Parse(TempData["SolicitudFechaFin"].ToString());
            var nombreEvento = TempData["SolicitudNombreEvento"]?.ToString();
            var descripcion = TempData["SolicitudDescripcion"]?.ToString();

            // 1. Crear la orden de pago
            var orden = new Orden
            {
                ClienteId = userId,
                TotalPagado = monto,
                FechaCompra = DateTime.UtcNow,
                Concepto = "Alquiler",
                Reembolsado = false,
                TransaccionPasarelaId = "SIM-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };

            _context.Ordenes.Add(orden);
            await _context.SaveChangesAsync();

            // 2. Crear la solicitud vinculada a la orden
            var solicitud = new SolicitudAlquiler
            {
                ClienteId = userId,
                LugarId = lugarId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                MontoAlquiler = monto,
                Estado = "Pendiente",
                FechaSolicitud = DateTime.Now,
                NombreEvento = nombreEvento,
                DescripcionEvento = descripcion,
                OrdenId = orden.Id
            };

            _context.SolicitudesAlquiler.Add(solicitud);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"¡Pago procesado! Tu solicitud está pendiente de aprobación. Monto pagado: RD$ {monto:N0}";
            return RedirectToAction("MisSolicitudes");
        }
        // ✅ Ver mis solicitudes (Cliente)
        [HttpGet]
        public async Task<IActionResult> MisSolicitudes()
        {
            var userId = _userManager.GetUserId(User);
            var solicitudes = await _context.SolicitudesAlquiler
                .Include(s => s.Lugar)
                .Include(s => s.Evento)
                .Where(s => s.ClienteId == userId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        // ✅ Ver todas las solicitudes (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GestionSolicitudes()
        {
            var solicitudes = await _context.SolicitudesAlquiler
                .Include(s => s.Lugar)
                .Include(s => s.Cliente)
                .Where(s => s.Estado != "Terminado") // ✅ Ocultar terminados
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        // ✅ Aprobar solicitud (Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesAlquiler
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null) return NotFound();

            solicitud.Estado = "Aprobado";

            // ✅ Dar rol OrganizadorTemporal en vez de Organizador
            if (!await _userManager.IsInRoleAsync(solicitud.Cliente, "OrganizadorTemporal"))
                await _userManager.AddToRoleAsync(solicitud.Cliente, "OrganizadorTemporal");

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Solicitud aprobada. {solicitud.Cliente.NombreCompleto} ahora puede crear su evento.";
            return RedirectToAction("GestionSolicitudes");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Terminar(int id)
        {
            var solicitud = await _context.SolicitudesAlquiler
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null) return NotFound();

            // ✅ Despublicar el evento
            if (solicitud.EventoId != null)
            {
                var evento = await _context.Eventos.FindAsync(solicitud.EventoId);
                if (evento != null)
                {
                    evento.EstadoEvento = "Terminado";
                    _context.Update(evento);
                }
            }

            // ✅ Quitar rol OrganizadorTemporal
            if (solicitud.Cliente != null)
            {
                var tieneOtrosEventos = await _context.Eventos
                    .AnyAsync(e => e.OrganizadorId == solicitud.ClienteId
                                && e.Id != solicitud.EventoId);

                if (!tieneOtrosEventos)
                {
                    await _userManager.RemoveFromRoleAsync(solicitud.Cliente, "OrganizadorTemporal");
                    await _userManager.UpdateSecurityStampAsync(solicitud.Cliente);
                }
            }

            solicitud.Estado = "Terminado";
            solicitud.EventoId = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Alquiler terminado. El evento fue despublicado y el cliente perdió el acceso temporal.";
            return RedirectToAction("GestionSolicitudes");
        }
        public async Task<IActionResult> EventosOcupados(int? lugarId)
        {
            var eventos = await _context.Eventos
                .Where(e => lugarId == null || e.LugarId == lugarId)
                .Select(e => new {
                    title = e.Nombre,
                    start = e.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = e.FechaFin.ToString("yyyy-MM-ddTHH:mm:ss"),
                    color = "#dc3545",
                    textColor = "#fff"
                })
                .ToListAsync();

            var alquileres = await _context.SolicitudesAlquiler
                .Where(s => s.Estado != "Rechazado" && (lugarId == null || s.LugarId == lugarId))
                .Select(s => new {
                    title = s.NombreEvento,
                    start = s.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = s.FechaFin.ToString("yyyy-MM-ddTHH:mm:ss"),
                    color = "#fd7e14",
                    textColor = "#fff"
                })
                .ToListAsync();

            return Json(eventos.Concat(alquileres));
        }
        // ✅ Rechazar solicitud (Admin) — con reembolso
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Rechazar(int id, string motivoRechazo)
        {
            var solicitud = await _context.SolicitudesAlquiler
                .Include(s => s.Orden)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null) return NotFound();

            solicitud.Estado = "Rechazado";
            solicitud.MotivoRechazo = motivoRechazo;

            // ✅ Marcar la orden como reembolsada
            if (solicitud.Orden != null)
            {
                solicitud.Orden.Reembolsado = true;
                solicitud.Orden.FechaReembolso = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud rechazada. El pago será reembolsado a la tarjeta del cliente.";
            return RedirectToAction("GestionSolicitudes");
        }
    }
}