
using Microsoft.EntityFrameworkCore;
using FunerariaBackend.Models;

namespace FunerariaBackend.Data

{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Sucursal> Sucursales => Set<Sucursal>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<InventarioSucursal> InventariosSucursales => Set<InventarioSucursal>();
        public DbSet<ServicioFunerario> ServiciosFunerarios => Set<ServicioFunerario>();
        public DbSet<DetalleServicio> DetallesServicios => Set<DetalleServicio>();
        public DbSet<DocumentoServicio> DocumentosServicio => Set<DocumentoServicio>();

        public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();// para registrar los movimientos de inventario


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Precargar las dos sucursales iniciales
            modelBuilder.Entity<Sucursal>().HasData(
                new Sucursal { Id = 1, Nombre = "Guerrero", Direccion = "Sucursal Guerrero" },
                new Sucursal { Id = 2, Nombre = "Cuauhtémoc", Direccion = "Sucursal Cuauhtémoc" }
            );

            //precargar un usuario administrador
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nombre = "Admin",
                    Email = "Admin@funeraria.com",
                    Rol = "Administrador",
                    PasswordHash = "$2a$11$eA83.pMvtP6eA811RByf.O67lFwKxGzHk0XU910d6N/eJtP7R.oSa", //Admin1234
                    Activo = true

                }
            );
        }
    }
}
