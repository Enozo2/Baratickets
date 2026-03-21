using Baratickets2._0.Data;
using Baratickets2._0.Models;
using Baratickets2._0.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. CONFIGURACIÓN DE SERVICIOS (Contenedor de Dependencias)
// ==========================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configuración de Base de Datos SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de Seguridad (Identity)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); 
    options.Lockout.MaxFailedAccessAttempts = 5; 
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Registro de Servicios Personalizados
builder.Services.AddScoped<QrService>();
builder.Services.AddTransient<IEmailService, EmailService>(); 
builder.Services.AddTransient<IEmailSender, EmailSender>();   

// Redirección de Cookies para Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(2); 
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
});

var app = builder.Build();

// ==========================================================
// 2. PIPELINE DE CONFIGURACIÓN (Middleware)
// ==========================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ==========================================================
// 3. INICIALIZADOR DE DATOS (Seed Data)
// ==========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.EnsureCreated();

        // 1. Crear roles
        string[] rolesParaCrear = { "Admin", "Cliente", "Organizador", "Validador", "OrganizadorTemporal" };
        foreach (var nombreRol in rolesParaCrear)
        {
            if (!roleManager.RoleExistsAsync(nombreRol).Result)
                roleManager.CreateAsync(new IdentityRole(nombreRol)).Wait();
        }

        // 2. Crear Admin predeterminado si no existe
        var adminEmail = "Enocr28@gmail.com";
        var adminExistente = userManager.FindByEmailAsync(adminEmail).Result;

        if (adminExistente == null)
        {
            // ✅ Crear el usuario Admin automáticamente
            var nuevoAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador",
                EmailConfirmed = true
            };

            var resultado = userManager.CreateAsync(nuevoAdmin, "Admin123!").Result;

            if (resultado.Succeeded)
            {
                userManager.AddToRoleAsync(nuevoAdmin, "Admin").Wait();
                Console.WriteLine("✅ Admin creado: " + adminEmail + " / Admin123!");
            }
        }
        else
        {
            // Si ya existe, asegurarse que tiene el rol Admin
            if (!userManager.IsInRoleAsync(adminExistente, "Admin").Result)
                userManager.AddToRoleAsync(adminExistente, "Admin").Wait();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR EN SEED DATA: " + ex.Message);
    }
}

app.Run();