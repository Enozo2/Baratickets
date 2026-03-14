using Baratickets2._0.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Baratickets2._0.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Orden> Ordenes { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<EventoValidador> EventosValidadores { get; set; }

        // --- AGREGAMOS LA NUEVA TABLA AQUÍ ---
        public DbSet<CategoriaTicket> CategoriasTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar la llave primaria compuesta para la tabla de validadores
            builder.Entity<EventoValidador>()
                .HasKey(ev => new { ev.EventoId, ev.ValidadorId });

            // Relación Evento -> Categorias (Configuración de borrado)
            builder.Entity<CategoriaTicket>()
                .HasOne(c => c.Evento)
                .WithMany(e => e.CategoriasTickets)
                .HasForeignKey(c => c.EventoId)
                .OnDelete(DeleteBehavior.Cascade); // Si borras el evento, mueren sus categorías

            // Evitar conflictos de borrado en cascada en SQL Server (Tus configuraciones previas)
            builder.Entity<Orden>()
                .HasOne(o => o.Cliente)
                .WithMany(u => u.OrdenesDeCompra)
                .HasForeignKey(o => o.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Evento>()
                .HasOne(e => e.Organizador)
                .WithMany(u => u.EventosOrganizados)
                .HasForeignKey(e => e.OrganizadorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}