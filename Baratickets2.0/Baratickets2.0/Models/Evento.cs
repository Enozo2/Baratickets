using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Baratickets2._0.Models
{
    public class Evento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Display(Name = "Nombre del Evento")]
        public string? Organizacion { get; set; } 
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [Display(Name = "Dirección del Evento")]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Capacidad Total del Recinto")]
        public int Capacidad { get; set; } // Esta es la capacidad física del lugar

        public string Descripcion { get; set; }

        [Display(Name = "URL de la Imagen/Flyer")]
        public string? ImagenUrl { get; set; }

        [Required]
        [Display(Name = "Fecha y Hora")]
        public DateTime FechaEvento { get; set; }

        // --- RELACIÓN CON EL ORGANIZADOR ---
        [Required]
        public string? OrganizadorId { get; set; }

        [ForeignKey("OrganizadorId")]
        public virtual ApplicationUser? Organizador { get; set; }

        // --- RELACIÓN DINÁMICA DE SEGMENTOS (Lo que pidió el profesor) ---
        // Aquí es donde el Organizador podrá agregar "VIP", "General", "Lado A", etc.
        public virtual ICollection<CategoriaTicket> CategoriasTickets { get; set; } = new List<CategoriaTicket>();

        // --- OTRAS RELACIONES ---
        public virtual ICollection<Ticket>? Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<ApplicationUser>? ValidadoresAsignados { get; set; } = new List<ApplicationUser>();
    }
}