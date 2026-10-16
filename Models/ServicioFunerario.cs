namespace FunerariaBackend.Models
{
    public class ServicioFunerario
    {
        public int Id { get; set; }
        public string Folio { get; set; } = string.Empty;

        public int SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        public string NombreFinado { get; set; } = string.Empty;
        public string FamiliarResponsable { get; set; } = string.Empty;

        public TipoDisposicion TipoDisposicion { get; set; }
        public LugarVelacion LugarVelacion { get; set; }

        public DateTime FechaServicio { get; set; } = DateTime.UtcNow;
        public decimal TotalCobrado { get; set; }
        public decimal CostoTotal { get; set; }
        public bool Pagado { get; set; } = false;

        public ICollection<DetalleServicio> Detalles { get; set; } = new List<DetalleServicio>(); // Relación con DetalleServicio
        public ICollection<DocumentoServicio> Documentos { get; set; } = new List<DocumentoServicio>();


    }
}
