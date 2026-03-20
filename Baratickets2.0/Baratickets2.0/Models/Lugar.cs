using System.ComponentModel.DataAnnotations;

namespace Baratickets2._0.Models
{
    public class Lugar
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }

        public bool EstaActivo { get; set; } = true;

        [Required]
        public decimal PrecioPorHora { get; set; } = 500;

        public virtual ICollection<Evento> Eventos { get; set; }
        public virtual ICollection<SolicitudAlquiler> SolicitudesAlquiler { get; set; }
    }

}
