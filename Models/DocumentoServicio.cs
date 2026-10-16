namespace FunerariaBackend.Models
{
    public class DocumentoServicio
    {
        public int Id { get; set; }
        public int ServicioFunerarioId { get; set; }
        public ServicioFunerario? ServicioFunerario { get; set; }

        public TipoDocumento TipoDoc { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaAlmacenamiento { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;


    }
}
