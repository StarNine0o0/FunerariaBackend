namespace FunerariaBackend.Models
{
    public enum TipoProducto
    {
        AtaudMadera,
        AtaudMetalico,
        AtaudJumbo,
        AtaudInfantilMadera,
        AtaudInfantilMetalico,
        Urna
    }

    public enum  TipoDisposicion
    {
        Inhumacion,
        Cremacion
    }

    public enum LugarVelacion
    {
        CapillaFuneraria,
        Domicilio
    }

    public enum TipoMovimiento
    {
        EntradaCompra,
        SalidaVenta,
        TrasladoSalida,
        TrasladoEntrada,
        MermaDanio
    }


    public enum TipoDocumento
    {
        CertificadoDefuncion,
        ReciboPago,
        Factura,
        NotaRemision
    }


}
