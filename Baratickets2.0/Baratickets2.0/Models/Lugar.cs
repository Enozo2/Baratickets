using System.ComponentModel.DataAnnotations;

namespace Baratickets2._0.Models
{
public class Lugar
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } // Ejemplo: "Piscina Olímpica"

        public string? Descripcion { get; set; } // Ejemplo: "Capacidad para 500 personas"

        public bool EstaActivo { get; set; } = true;
        public virtual ICollection<Evento> Eventos { get; set; }
    }

}
