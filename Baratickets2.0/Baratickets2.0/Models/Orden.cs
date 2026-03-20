using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace Baratickets2._0.Models
{
    public class Orden
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaCompra { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPagado { get; set; }

        // Aquí guardaremos el ID de pago de Stripe
        public string TransaccionPasarelaId { get; set; }
        // ✅ Para distinguir si es pago de ticket o alquiler
        public string Concepto { get; set; } = "Ticket"; // "Ticket" o "Alquiler"

        // ✅ Para reembolsos
        public bool Reembolsado { get; set; } = false;
        public DateTime? FechaReembolso { get; set; }

        // Relación con el Cliente
        [Required]
        public string ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public ApplicationUser Cliente { get; set; }

        // Una orden contiene los tickets generados
        public ICollection<Ticket> Tickets { get; set; }
    }
}