namespace FunerariaBackend.Models
{
    public class DetalleServicio
    {
        public int Id { get; set; }

        public int ServicioFunerarioId { get; set; }
        public ServicioFunerario? ServicioFunerario { get; set; }

        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public int Cantidad { get; set; }
        public bool EsRenta { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PrecioCobrado { get; set; }
    }
}
