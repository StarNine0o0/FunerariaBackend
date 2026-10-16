using Microsoft.EntityFrameworkCore;
using FunerariaBackend.Data;
using FunerariaBackend.Models;

namespace FunerariaBackend.Endpoints;

public static class ServicioFunerarioEndpoints
{
    public static void MapServicioFunerarioEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/servicios").WithTags("Servicios Funerarios y Expedientes");

        // 1. GET: Consultar expediente completo por ID o Folio
        grupo.MapGet("/{idOrFolio}", async (string idOrFolio, AppDbContext db) =>
        {
            bool esId = int.TryParse(idOrFolio, out int id);

            var servicio = await db.ServiciosFunerarios
                .Include(s => s.Sucursal)
                .Include(s => s.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(s => s.Documentos)
                .FirstOrDefaultAsync(s => esId ? s.Id == id : s.Folio == idOrFolio);

            if (servicio is null)
                return Results.NotFound(new { mensaje = "Expediente del servicio no encontrado." });

            return Results.Ok(servicio);
        });

        // 2. GET: Listar todos los servicios (con filtro opcional por nombre de finado)
        grupo.MapGet("/", async (string? buscar, AppDbContext db) =>
        {
            var query = db.ServiciosFunerarios
                .Include(s => s.Sucursal)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(s => s.NombreFinado.Contains(buscar) || s.FamiliarResponsable.Contains(buscar) || s.Folio.Contains(buscar));
            }

            var lista = await query
                .OrderByDescending(s => s.FechaServicio)
                .Select(s => new
                {
                    s.Id,
                    s.Folio,
                    Sucursal = s.Sucursal!.Nombre,
                    s.NombreFinado,
                    s.FamiliarResponsable,
                    s.TipoDisposicion,
                    s.LugarVelacion,
                    s.FechaServicio,
                    s.TotalCobrado,
                    s.CostoTotal,
                    Utilidad = s.TotalCobrado - s.CostoTotal,
                    s.Pagado
                })
                .ToListAsync();

            return Results.Ok(lista);
        });

        // 3. POST: Aperturar Servicio Funerario (Aplica lógica de Inhumación vs Cremación)
        grupo.MapPost("/", async (CrearServicioDto dto, AppDbContext db) =>
        {
            using var transaccion = await db.Database.BeginTransactionAsync();

            try
            {
                decimal totalCobradoServicio = 0;
                decimal costoTotalServicio = 0;
                var detallesServicio = new List<DetalleServicio>();

                // Procesar cada producto asignado al servicio
                foreach (var item in dto.Productos)
                {
                    var producto = await db.Productos.FindAsync(item.ProductoId);
                    if (producto is null)
                        return Results.BadRequest(new { mensaje = $"El producto ID {item.ProductoId} no existe." });

                    bool esRenta = false;

                    // Regla de Negocio de Inventario según la Disposición
                    if (dto.TipoDisposicion == TipoDisposicion.Inhumacion)
                    {
                        // En Inhumación: Salida definitiva del stock
                        var inventario = await db.InventariosSucursales
                            .FirstOrDefaultAsync(i => i.SucursalId == dto.SucursalId && i.ProductoId == item.ProductoId);

                        if (inventario is null || inventario.Stock < item.Cantidad)
                            return Results.BadRequest(new { mensaje = $"Stock insuficiente de '{producto.Nombre}' en la sucursal." });

                        inventario.Stock -= item.Cantidad;

                        // Registrar salida en Kárdex
                        db.MovimientosInventario.Add(new MovimientoInventario
                        {
                            SucursalId = dto.SucursalId,
                            ProductoId = item.ProductoId,
                            Cantidad = -item.Cantidad,
                            TipoMovimiento = TipoMovimiento.SalidaVenta,
                            FolioReferencia = dto.Folio
                        });
                    }
                    else if (dto.TipoDisposicion == TipoDisposicion.Cremacion)
                    {
                        // En Cremación: Si es ataúd se marca como RENTA (no descuenta stock). Si es urna, sí descuenta.
                        if (producto.Tipo != TipoProducto.Urna)
                        {
                            esRenta = true; // Se presta para la velación
                        }
                        else
                        {
                            var inventario = await db.InventariosSucursales
                                .FirstOrDefaultAsync(i => i.SucursalId == dto.SucursalId && i.ProductoId == item.ProductoId);

                            if (inventario is null || inventario.Stock < item.Cantidad)
                                return Results.BadRequest(new { mensaje = $"Stock insuficiente de urnas '{producto.Nombre}'." });

                            inventario.Stock -= item.Cantidad;

                            db.MovimientosInventario.Add(new MovimientoInventario
                            {
                                SucursalId = dto.SucursalId,
                                ProductoId = item.ProductoId,
                                Cantidad = -item.Cantidad,
                                TipoMovimiento = TipoMovimiento.SalidaVenta,
                                FolioReferencia = dto.Folio
                            });
                        }
                    }

                    decimal precioCobrado = item.PrecioCobradoOverride ?? producto.PrecioVenta;
                    totalCobradoServicio += precioCobrado * item.Cantidad;
                    costoTotalServicio += producto.CostoCompra * item.Cantidad;

                    detallesServicio.Add(new DetalleServicio
                    {
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        EsRenta = esRenta,
                        CostoUnitario = producto.CostoCompra,
                        PrecioCobrado = precioCobrado
                    });
                }

                var nuevoServicio = new ServicioFunerario
                {
                    Folio = dto.Folio,
                    SucursalId = dto.SucursalId,
                    NombreFinado = dto.NombreFinado,//  nombre finado es el nombre de la persona fallecida, es decir, el difunto.
                    FamiliarResponsable = dto.FamiliarResponsable,
                    TipoDisposicion = dto.TipoDisposicion,
                    LugarVelacion = dto.LugarVelacion,
                    FechaServicio = dto.FechaServicio ?? DateTime.UtcNow,
                    TotalCobrado = totalCobradoServicio,
                    CostoTotal = costoTotalServicio,
                    Pagado = dto.Pagado,
                    Detalles = detallesServicio
                };

                db.ServiciosFunerarios.Add(nuevoServicio);
                await db.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Results.Ok(new
                {
                    id = nuevoServicio.Id,
                    folio = nuevoServicio.Folio,
                    nombreFinado = nuevoServicio.NombreFinado,
                    totalCobrado = nuevoServicio.TotalCobrado,
                    costoTotal = nuevoServicio.CostoTotal,
                    utilidad = nuevoServicio.TotalCobrado - nuevoServicio.CostoTotal,
                    mensaje = "Servicio guardado con éxito"
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return Results.Problem(detail: ex.ToString(), statusCode: 500);

                //return Results.Problem("Error crítico al registrar el servicio funerario.");
            }
        });

        // POST: Subir un documento (Certificado, Recibo, Factura, Nota) a un expediente con validación de seguridad
        grupo.MapPost("/{id:int}/documentos", async (int id, IFormFile archivo, TipoDocumento tipoDoc, AppDbContext db, IWebHostEnvironment env) =>
        {
            // 1. Validar que el expediente exista
            var servicio = await db.ServiciosFunerarios.FindAsync(id);
            if (servicio is null)
                return Results.NotFound(new { mensaje = "El servicio funerario no existe." });

            if (archivo is null || archivo.Length == 0)
                return Results.BadRequest(new { mensaje = "Debe adjuntar un archivo válido." });

            // 2. VALIDACIÓN DE SEGURIDAD A: Límite de tamaño (Máximo 7 MB)
            const long maxTamanoPermitido = 7 * 1024 * 1024; // 7 MB en bytes
            if (archivo.Length > maxTamanoPermitido)
                return Results.BadRequest(new { mensaje = "El archivo excede el tamaño máximo permitido de 7 MB." });

            // 3. VALIDACIÓN DE SEGURIDAD B: Extensiones de archivo permitidas
            var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !extensionesPermitidas.Contains(extension))
                return Results.BadRequest(new { mensaje = "Formato no permitido. Solo se aceptan archivos PDF e imágenes (JPG, PNG)." });

            // 4. VALIDACIÓN DE SEGURIDAD C: Tipos MIME autorizados
            var mimesPermitidos = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!mimesPermitidos.Contains(archivo.ContentType.ToLower()))
                return Results.BadRequest(new { mensaje = "El tipo de contenido del archivo no es válido." });

            // 5. Guardar en carpeta física segura
            var carpetaUploads = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "expedientes");

            if (!Directory.Exists(carpetaUploads))
                Directory.CreateDirectory(carpetaUploads);

            // Renombrar con GUID único para evitar sobreescritura y sanitizar el nombre
            var nombreArchivoUnico = $"{servicio.Folio}_{Guid.NewGuid()}{extension}";
            var rutaFisicaCompleta = Path.Combine(carpetaUploads, nombreArchivoUnico);

            using (var stream = new FileStream(rutaFisicaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var rutaRelativaWeb = $"/uploads/expedientes/{nombreArchivoUnico}";

            var nuevoDocumento = new DocumentoServicio
            {
                ServicioFunerarioId = id,
                TipoDoc = tipoDoc,
                NombreArchivo = archivo.FileName,
                RutaAlmacenamiento = rutaRelativaWeb,
                FechaSubida = DateTime.UtcNow
            };

            db.DocumentosServicio.Add(nuevoDocumento);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = nuevoDocumento.Id,
                nombreOriginal = archivo.FileName,
                tipoDocumento = tipoDoc.ToString(),
                urlDescarga = rutaRelativaWeb,
                mensaje = "Documento adjuntado y validado exitosamente."
            });
        }).DisableAntiforgery();

        // GET: Consultar todos los documentos adjuntos de un expediente
        grupo.MapGet("/{id:int}/documentos", async (int id, AppDbContext db) =>
        {
            var documentos = await db.DocumentosServicio
                .Where(d => d.ServicioFunerarioId == id)
                .Select(d => new
                {
                    d.Id,
                    d.TipoDoc,
                    d.NombreArchivo,
                    d.RutaAlmacenamiento,
                    d.FechaSubida
                })
                .ToListAsync();

            return Results.Ok(documentos);
        });
    }
}

// DTOs auxiliares para recepción de peticiones
public record ItemServicioDto(int ProductoId, int Cantidad, decimal? PrecioCobradoOverride);
public record CrearServicioDto(
    string Folio,
    int SucursalId,
    string NombreFinado,
    string FamiliarResponsable,
    TipoDisposicion TipoDisposicion,
    LugarVelacion LugarVelacion,
    bool Pagado,
    List<ItemServicioDto> Productos,
    DateTime? FechaServicio
);