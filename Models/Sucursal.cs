namespace FunerariaBackend.Models
{
    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        public ICollection<InventarioSucursal> Inventarios { get; set; } = new List<InventarioSucursal>();
        
    }
}
