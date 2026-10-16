namespace FunerariaBackend.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Medidas { get; set; }
        public TipoProducto Tipo { get; set; }
        public decimal CostoCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; } = true;
    }
}
