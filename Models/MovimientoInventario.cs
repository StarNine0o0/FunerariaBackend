namespace FunerariaBackend.Models
{
    public class MovimientoInventario
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public int Cantidad { get; set; }
        public TipoMovimiento TipoMovimiento { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string? FolioReferencia { get; set; }
    }
}
