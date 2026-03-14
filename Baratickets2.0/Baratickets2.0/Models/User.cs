using Microsoft.AspNetCore.Identity;
using Stripe.Climate;
using System.ComponentModel.DataAnnotations;

namespace Baratickets2._0.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string NombreCompleto { get; set; }

        // Relaciones
        public ICollection<Evento> EventosOrganizados { get; set; }
        public ICollection<Orden> OrdenesDeCompra { get; set; }
        public ICollection<EventoValidador> EventosAsignadosParaValidar { get; set; }
    }
}