using Microsoft.EntityFrameworkCore;
using FunerariaBackend.Data;
using FunerariaBackend.Models;

namespace FunerariaBackend.Endpoints;

public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/productos").WithTags("Productos");//para que es whithTags? sirve para documentacion de swagger

        // GET: Obtener todos los productos activos
        grupo.MapGet("/", async (AppDbContext db) =>
            await db.Productos.Where(p => p.Activo).ToListAsync());

        // GET: Buscar por ID
        grupo.MapGet("/{id:int}", async (int id, AppDbContext db) =>
            await db.Productos.FindAsync(id) is Producto p
                ? Results.Ok(p)
                : Results.NotFound(new { mensaje = "Producto no encontrado" }));

        // POST: Crear nuevo producto en el catálogo
        grupo.MapPost("/", async (CrearProductoDto dto, AppDbContext db) =>
        {
            var producto = new Producto
            {
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Medidas = dto.Medidas,
                Tipo = dto.Tipo,
                CostoCompra = dto.CostoCompra,
                PrecioVenta = dto.PrecioVenta
            };

            db.Productos.Add(producto);
            await db.SaveChangesAsync();

            return Results.Created($"/api/productos/{producto.Id}", producto);
        });

        // PUT: Actualizar precio o datos del producto
        grupo.MapPut("/{id:int}", async (int id, CrearProductoDto dto, AppDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(id);
            if (producto is null) return Results.NotFound(new { mensaje = "Producto no encontrado" });
     
            producto.Codigo = dto.Codigo;
            producto.Nombre = dto.Nombre;
            producto.Medidas = dto.Medidas;
            producto.Tipo = dto.Tipo;
            producto.CostoCompra = dto.CostoCompra;
            producto.PrecioVenta = dto.PrecioVenta;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

// DTO para recepción de datos
public record CrearProductoDto(
    string Codigo,
    string Nombre,
    string? Medidas,
    TipoProducto Tipo,
    decimal CostoCompra,
    decimal PrecioVenta
);