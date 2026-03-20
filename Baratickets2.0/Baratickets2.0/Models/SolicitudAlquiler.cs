using System.ComponentModel.DataAnnotations;

namespace Baratickets2._0.Models
{
    public class SolicitudAlquiler
    {
        public int Id { get; set; }

        [Required]
        public string ClienteId { get; set; }

        [Required]
        public int LugarId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public decimal MontoAlquiler { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public string? MotivoRechazo { get; set; }

        public int? EventoId { get; set; }
        public int? OrdenId { get; set; }
        public Orden? Orden { get; set; }

        public string? NombreEvento { get; set; }      // Nombre tentativo del evento
        public string? DescripcionEvento { get; set; } // Descripción tentativa

        // Navegación
        public ApplicationUser Cliente { get; set; }
        public Lugar Lugar { get; set; }
        public Evento? Evento { get; set; }
    }
}