namespace FunerariaBackend.Models
{
    public class InventarioSucursal
    {
        public int Id { get; set; }
        public int SucursalId { get; set; } // clave foránea para la relación con Sucursal
        public Sucursal? Sucursal { get; set; }  // relación con la clase Sucursal

        public int ProductoId { get; set; } // clave foránea para la relación con Producto
        public Producto? Producto { get; set; } // relación con la clase Producto

        public int Stock { get; set; }
    }
}
