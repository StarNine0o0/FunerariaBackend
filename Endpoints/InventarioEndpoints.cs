using Microsoft.EntityFrameworkCore;
using FunerariaBackend.Data;
using FunerariaBackend.Models;

namespace FunerariaBackend.Endpoints;

public static class InventarioEndpoints
{
    public static void MapInventarioEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/inventario").WithTags("Inventario y Almacenes");

        // GET: Consultar existencias por sucursal (Guerrero o Cuauhtémoc)
        grupo.MapGet("/sucursal/{sucursalId:int}", async (int sucursalId, AppDbContext db) =>
        {
            var stock = await db.InventariosSucursales
                .Include(i => i.Producto)
                .Include(i => i.Sucursal)
                .Where(i => i.SucursalId == sucursalId)
                .Select(i => new
                {
                    i.ProductoId,
                    Producto = i.Producto!.Nombre,
                    i.Producto.Codigo,
                    i.Stock
                })
                .ToListAsync();

            return Results.Ok(stock);
        });

        // POST: Registrar entrada de mercancía (Compra a proveedor)
        grupo.MapPost("/entrada", async (RegistrarEntradaDto dto, AppDbContext db) =>
        {
            var inventario = await db.InventariosSucursales
                .FirstOrDefaultAsync(i => i.SucursalId == dto.SucursalId && i.ProductoId == dto.ProductoId);

            if (inventario is null)
            {
                inventario = new InventarioSucursal
                {
                    SucursalId = dto.SucursalId,
                    ProductoId = dto.ProductoId,
                    Stock = dto.Cantidad
                };
                db.InventariosSucursales.Add(inventario);
            }
            else
            {
                inventario.Stock += dto.Cantidad;
            }

            // Registrar movimiento en Kárdex
            var movimiento = new MovimientoInventario
            {
                SucursalId = dto.SucursalId,
                ProductoId = dto.ProductoId,
                Cantidad = dto.Cantidad,
                TipoMovimiento = TipoMovimiento.EntradaCompra,
                FolioReferencia = dto.NotaRemision
            };
            db.MovimientosInventario.Add(movimiento);

            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Entrada de mercancía registrada exitosamente", stockActual = inventario.Stock });
        });

        // POST: Traslado entre sucursales (Guerrero <-> Cuauhtémoc)
        grupo.MapPost("/traslado", async (TrasladoDto dto, AppDbContext db) =>
        {
            using var transaccion = await db.Database.BeginTransactionAsync();

            try
            {
                // 1. Validar y descontar del origen
                var origen = await db.InventariosSucursales
                    .FirstOrDefaultAsync(i => i.SucursalId == dto.SucursalOrigenId && i.ProductoId == dto.ProductoId);

                if (origen is null || origen.Stock < dto.Cantidad)
                    return Results.BadRequest(new { mensaje = "Stock insuficiente en la sucursal de origen" });

                origen.Stock -= dto.Cantidad;

                // 2. Incrementar en destino
                var destino = await db.InventariosSucursales
                    .FirstOrDefaultAsync(i => i.SucursalId == dto.SucursalDestinoId && i.ProductoId == dto.ProductoId);

                if (destino is null)
                {
                    destino = new InventarioSucursal
                    {
                        SucursalId = dto.SucursalDestinoId,
                        ProductoId = dto.ProductoId,
                        Stock = dto.Cantidad
                    };
                    db.InventariosSucursales.Add(destino);
                }
                else
                {
                    destino.Stock += dto.Cantidad;
                }

                // 3. Registrar los dos movimientos en Kárdex
                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    SucursalId = dto.SucursalOrigenId,
                    ProductoId = dto.ProductoId,
                    Cantidad = -dto.Cantidad,
                    TipoMovimiento = TipoMovimiento.TrasladoSalida
                });

                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    SucursalId = dto.SucursalDestinoId,
                    ProductoId = dto.ProductoId,
                    Cantidad = dto.Cantidad,
                    TipoMovimiento = TipoMovimiento.TrasladoEntrada
                });

                await db.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Results.Ok(new { mensaje = "Traslado entre sucursales realizado con éxito" });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();
                return Results.Problem("Ocurrió un error al procesar el traslado");
            }
        });
    }
}

public record RegistrarEntradaDto(int SucursalId, int ProductoId, int Cantidad, string? NotaRemision);
public record TrasladoDto(int SucursalOrigenId, int SucursalDestinoId, int ProductoId, int Cantidad);