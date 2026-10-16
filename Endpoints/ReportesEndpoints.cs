using Microsoft.EntityFrameworkCore;
using FunerariaBackend.Data;
using FunerariaBackend.Models;

namespace FunerariaBackend.Endpoints;

public static class ReportesEndpoints
{
    public static void MapReportesEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/reportes").WithTags("Dashboard y Reportes Financieros").RequireAuthorization();

        // 1. GET: Resumen Financiero General (Filtrable por fechas y sucursal)
        grupo.MapGet("/financiero", async (DateTime? fechaInicio, DateTime? fechaFin, int? sucursalId, AppDbContext db) =>
        {
            var query = db.ServiciosFunerarios.AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaServicio >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaServicio <= fechaFin.Value);

            if (sucursalId.HasValue)
                query = query.Where(s => s.SucursalId == sucursalId.Value);

            var totalServicios = await query.CountAsync();
            var ingresosTotales = await query.SumAsync(s => (decimal?)s.TotalCobrado) ?? 0;
            var costosTotales = await query.SumAsync(s => (decimal?)s.CostoTotal) ?? 0;
            var utilidadNeta = ingresosTotales - costosTotales;
            var margenGananciaPorcentaje = ingresosTotales > 0
                ? Math.Round((utilidadNeta / ingresosTotales) * 100, 2)
                : 0;

            // Desglose por disposición (Inhumaciones vs Cremaciones)
            var resumenDisposicion = await query
                .GroupBy(s => s.TipoDisposicion)
                .Select(g => new
                {
                    Tipo = g.Key.ToString(),
                    Cantidad = g.Count(),
                    TotalCobrado = g.Sum(s => s.TotalCobrado)
                })
                .ToListAsync();

            return Results.Ok(new
            {
                totalServicios,
                ingresosTotales,
                costosTotales,
                utilidadNeta,
                margenGananciaPorcentaje = $"{margenGananciaPorcentaje}%",
                desglosePorDisposicion = resumenDisposicion
            });
        });

        // 2. GET: Métricas por Sucursal (Comparativo Guerrero vs Cuauhtémoc)
        grupo.MapGet("/sucursales-comparativo", async (AppDbContext db) =>
        {
            var comparativo = await db.Sucursales
                .Select(suc => new
                {
                    suc.Id,
                    Sucursal = suc.Nombre,
                    TotalServicios = db.ServiciosFunerarios.Count(s => s.SucursalId == suc.Id),
                    Ingresos = db.ServiciosFunerarios.Where(s => s.SucursalId == suc.Id).Sum(s => (decimal?)s.TotalCobrado) ?? 0,
                    Costos = db.ServiciosFunerarios.Where(s => s.SucursalId == suc.Id).Sum(s => (decimal?)s.CostoTotal) ?? 0,
                    Utilidad = (db.ServiciosFunerarios.Where(s => s.SucursalId == suc.Id).Sum(s => (decimal?)s.TotalCobrado) ?? 0) -
                               (db.ServiciosFunerarios.Where(s => s.SucursalId == suc.Id).Sum(s => (decimal?)s.CostoTotal) ?? 0)
                })
                .ToListAsync();

            return Results.Ok(comparativo);
        });

        // 3. GET: Top Productos Más Vendidos / Utilizados
        grupo.MapGet("/top-productos", async (int limit, AppDbContext db) =>
        {
            int limiteResultados = limit > 0 ? limit : 5;

            var topProductos = await db.DetallesServicios
                .Include(d => d.Producto)
                .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre, d.Producto.Codigo, d.Producto.Tipo })
                .Select(g => new
                {
                    g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    g.Key.Codigo,
                    Tipo = g.Key.Tipo.ToString(),
                    TotalUnidadesVendidas = g.Sum(d => d.Cantidad),
                    TotalGenerado = g.Sum(d => d.PrecioCobrado * d.Cantidad)
                })
                .OrderByDescending(p => p.TotalUnidadesVendidas)
                .Take(limiteResultados)
                .ToListAsync();

            return Results.Ok(topProductos);
        });

        // 4. GET: Kárdex e Histórico de Movimientos de Inventario
        grupo.MapGet("/kardex", async (int? sucursalId, int? productoId, AppDbContext db) =>
        {
            var query = db.MovimientosInventario
                .Include(m => m.Sucursal)
                .Include(m => m.Producto)
                .AsQueryable();

            if (sucursalId.HasValue)
                query = query.Where(m => m.SucursalId == sucursalId.Value);

            if (productoId.HasValue)
                query = query.Where(m => m.ProductoId == productoId.Value);

            var kardex = await query
                .OrderByDescending(m => m.Fecha) // Corrección realizada a m.Fecha
                .Select(m => new
                {
                    m.Id,
                    Sucursal = m.Sucursal!.Nombre,
                    Producto = m.Producto!.Nombre,
                    m.Cantidad,
                    TipoMovimiento = m.TipoMovimiento.ToString(),
                    m.FolioReferencia,
                    m.Fecha
                })
                .ToListAsync();

            return Results.Ok(kardex);
        });
    }
}