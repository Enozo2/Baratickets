namespace Baratickets2._0.Models
{
    public class MiCuentaViewModel
    {
        public ApplicationUser Usuario { get; set; }
        public int TotalTicketsComprados { get; set; }
        public Ticket? UltimoTicket { get; set; }
        public bool EsOrganizador { get; set; } // Agregado para quitar el error de la imagen 8
    }
}