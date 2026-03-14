using System.ComponentModel.DataAnnotations.Schema;

namespace Baratickets2._0.Models
{
    public class EventoValidador
    {
        public int EventoId { get; set; }
        [ForeignKey("EventoId")]
        public Evento Evento { get; set; }

        public string ValidadorId { get; set; }
        [ForeignKey("ValidadorId")]
        public ApplicationUser Validador { get; set; }
    }
}
