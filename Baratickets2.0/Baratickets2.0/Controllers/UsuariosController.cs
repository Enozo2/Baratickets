using Baratickets2._0.Data; // Asegúrate de tener este usando para el DbContext
using Baratickets2._0.Models;
using Baratickets2._0.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize] // Ahora cualquier usuario logueado puede entrar al controlador
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context; 
    private readonly IEmailService _emailService;

    public UsuariosController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IEmailService emailService) // Lo recibimos aquí
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _emailService = emailService;
    }

    // ESTO ES LO QUE EL USUARIO VERÁ
    [HttpGet]
    public async Task<IActionResult> MiCuenta()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        var model = new MiCuentaViewModel
        {
            Usuario = user,

            // MODIFICACIÓN AQUÍ: Filtramos para que no cuente los tickets devueltos
            TotalTicketsComprados = await _context.Tickets
                .CountAsync(t => t.UsuarioId == user.Id && t.Estado != "Devuelto"),

            UltimoTicket = await _context.Tickets
                .Include(t => t.Evento)
                .OrderByDescending(t => t.FechaCompra)
                .FirstOrDefaultAsync(t => t.UsuarioId == user.Id),
            EsOrganizador = User.IsInRole("Organizador") || User.IsInRole("Admin")
        };

        return View(model);
    }

    // --- DE AQUÍ PARA ABAJO SOLO ENTRA EL ADMIN ---
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Crear(string email, string password, string nombreCompleto, string rol)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NombreCompleto = nombreCompleto,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, rol);

               
                try
                {
                    string mensaje = $@"
                    <h1>¡Bienvenido a Baratickets 2.0, {nombreCompleto}!</h1>
                    <p>Un administrador ha creado tu cuenta con el rol de: <strong>{rol}</strong>.</p>
                    <p>Tus credenciales de acceso son:</p>
                    <ul>
                        <li><strong>Usuario:</strong> {email}</li>
                        <li><strong>Contraseña Temporal:</strong> {password}</li>
                    </ul>
                    <p>Te recomendamos cambiar tu contraseña desde 'Mi Cuenta' al ingresar.</p>
                    <br>
                    <a href='https://localhost:7124/Identity/Account/Login' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Iniciar Sesión</a>";

                    await _emailService.EnviarCorreoAsync(email, "Acceso a Baratickets 2.0", mensaje);
                    TempData["Success"] = "Usuario creado y correo de acceso enviado.";
                }
                catch (Exception ex)
                {
                    TempData["Success"] = "Usuario creado, pero hubo un problema al enviar el correo.";
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View();
    }
    [AllowAnonymous]
    public async Task<IActionResult> DesbloquearCuenta(string userId)
    {
        if (userId == null) return RedirectToAction("Index", "Home");

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
          
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = "Tu cuenta ha sido desbloqueada. Ya puedes iniciar sesión.";
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var usuarios = await _userManager.Users.ToListAsync();
        return View(usuarios);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> EliminarUsuario(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser.Id == id)
        {
            TempData["Error"] = "No puedes eliminar tu propia cuenta.";
            return RedirectToAction(nameof(Index));
        }
        if (user.Email == "Enocr28@gmail.com")
        {
            TempData["Error"] = "No puedes eliminar al administrador principal del sistema.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Borrar eventos del usuario antes de eliminar
        var eventosUsuario = await _context.Eventos
            .Include(e => e.Tickets)
            .Include(e => e.CategoriasTickets)
            .Where(e => e.OrganizadorId == user.Id)
            .ToListAsync();

        foreach (var evento in eventosUsuario)
        {
            var solicitud = await _context.SolicitudesAlquiler
                .FirstOrDefaultAsync(s => s.EventoId == evento.Id);
            if (solicitud != null)
            {
                solicitud.EventoId = null;
                _context.Update(solicitud);
            }

            if (evento.Tickets != null && evento.Tickets.Any())
            {
                var ticketIds = evento.Tickets.Select(t => t.Id).ToList();
                await _context.Devoluciones
                    .Where(d => ticketIds.Contains(d.TicketId))
                    .ExecuteDeleteAsync();
            }

            if (evento.Tickets != null && evento.Tickets.Any())
                _context.Tickets.RemoveRange(evento.Tickets);

            if (evento.CategoriasTickets != null && evento.CategoriasTickets.Any())
                _context.CategoriasTickets.RemoveRange(evento.CategoriasTickets);

            _context.Eventos.Remove(evento);
        }

        // ✅ Borrar solicitudes de alquiler del usuario
        var solicitudesUsuario = await _context.SolicitudesAlquiler
            .Where(s => s.ClienteId == user.Id)
            .ToListAsync();
        _context.SolicitudesAlquiler.RemoveRange(solicitudesUsuario);

        // ✅ Borrar tickets comprados por el usuario
        var ticketsUsuario = await _context.Tickets
            .Where(t => t.UsuarioId == user.Id)
            .ToListAsync();

        foreach (var ticket in ticketsUsuario)
        {
            await _context.Devoluciones
                .Where(d => d.TicketId == ticket.Id)
                .ExecuteDeleteAsync();
        }
        _context.Tickets.RemoveRange(ticketsUsuario);

        // ✅ Borrar devoluciones restantes del usuario
        await _context.Devoluciones
            .Where(d => d.UsuarioId == user.Id)
            .ExecuteDeleteAsync();

        // ✅ Borrar órdenes del usuario
        var ordenesUsuario = await _context.Ordenes
            .Where(o => o.ClienteId == user.Id)
            .ToListAsync();
        _context.Ordenes.RemoveRange(ordenesUsuario);

        await _context.SaveChangesAsync();

        // ✅ Ahora sí eliminar el usuario
        await _userManager.DeleteAsync(user);
        TempData["Success"] = "Usuario eliminado.";
        return RedirectToAction(nameof(Index));
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AsignarRol(string userId, string nombreRol)
    {
        var usuario = await _userManager.FindByIdAsync(userId);
        if (usuario != null)
        {
            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            await _userManager.AddToRoleAsync(usuario, nombreRol);
        }
        return RedirectToAction(nameof(Index));
    }
}