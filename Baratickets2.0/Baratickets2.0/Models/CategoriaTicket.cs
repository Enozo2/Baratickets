using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Baratickets2._0.Models
{
    public class CategoriaTicket
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del segmento es obligatorio")]
        [Display(Name = "Nombre del Segmento")]
        public string Nombre { get; set; } // Ej: "Lado Derecho", "VIP"

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")] // <-- ESTA ES LA LÍNEA MÁGICA
        public decimal Precio { get; set; }

        [Required]
        [Display(Name = "Capacidad Máxima")]
        public int Capacidad { get; set; }

        // Relación con el Evento
        public int EventoId { get; set; }
        [ForeignKey("EventoId")]
        public Evento? Evento { get; set; }
    }
}