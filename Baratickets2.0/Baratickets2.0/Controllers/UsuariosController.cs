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
            TotalTicketsComprados = await _context.Tickets.CountAsync(t => t.UsuarioId == user.Id),
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