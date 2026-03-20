using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Baratickets2._0.Models
{
    public class Ticket
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // El futuro código del QR

        public int EventoId { get; set; }
        [ForeignKey("EventoId")]
        public virtual Evento? Evento { get; set; }
      

        public string? UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }


        // Esto le dice a Ticket que puede tener una devolución asociada
        public virtual Devolucion? Devolucion { get; set; }
        public string Tipo { get; set; } = "Normal";
        [Column(TypeName = "decimal(18,2)")] // Esto le dice a SQL: 18 dígitos, 2 decimales
        public decimal PrecioPagado { get; set; }
        public DateTime FechaCompra { get; set; } = DateTime.Now;
        public bool UsoCupon { get; set; } = false;
        [MaxLength(20)]
        public string Estado { get; set; } = "Activo"; // "Activo" o "Devuelto" [cite: 2026-02-21]
        public bool FueUsado { get; set; } = false;
        public DateTime? FechaUso { get; set; } // Para saber cuándo entró la persona
        public string? ValidadorId { get; set; }
    }
}