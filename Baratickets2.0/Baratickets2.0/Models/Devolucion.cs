namespace Baratickets2._0.Models
{
    public class Devolucion
    {
        public int Id { get; set; }
        public Guid TicketId { get; set; } // Relación con el Ticket
        public Ticket Ticket { get; set; }

        public string UsuarioId { get; set; } // Quién reclama
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public string Motivo { get; set; }

        // "CuentaBancaria" o "Cupon"
        public string TipoDevolucion { get; set; }

        public string Estado { get; set; } // "Pendiente", "Aprobada", "Rechazada"

        public bool EsCuponGenerado { get; set; }
        public string? CodigoCupon { get; set; } // Se llena si elige Cupón
        public bool CuponUsado { get; set; } = false;
        public decimal MontoOriginal { get; set; }
        public decimal MontoRestante { get; set; } // Por si usa 50 de los 100
    }
}
