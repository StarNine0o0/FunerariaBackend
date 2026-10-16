using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FunerariaBackend.Data;
using FunerariaBackend.Models;

namespace FunerariaBackend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var grupo = routes.MapGroup("/api/auth").WithTags("Autenticación y Usuarios");

        // POST: Registrar primer usuario (Semilla)
        grupo.MapPost("/registro", async (RegistroDto dto, AppDbContext db) =>
        {
            if (await db.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return Results.BadRequest(new { mensaje = "El email ya está registrado." });

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Rol = dto.Rol ?? "Administrador"
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            return Results.Ok(new { mensaje = "Usuario registrado exitosamente." });
        });

        // POST: Login para obtener el Token JWT
        grupo.MapPost("/login", async (LoginDto dto, AppDbContext db, IConfiguration config) =>
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email && u.Activo);
            if (usuario is null || !VerificarPassword(dto.Password, usuario.PasswordHash))
                return Results.Unauthorized();

            // Crear los Claims (Datos guardados dentro del token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // El token expira en 8 horas
                signingCredentials: creds
            );

            var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new
            {
                token = jwtString,
                usuario = usuario.Nombre,
                rol = usuario.Rol,
                expiracion = DateTime.UtcNow.AddHours(8)
            });
        });
    }

    // Funciones auxiliares para Hash de contraseña seguro
    
    private static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    private static bool VerificarPassword(string password, string storedHash) =>
        BCrypt.Net.BCrypt.Verify(password, storedHash);
}

public record RegistroDto(string Nombre, string Email, string Password, string? Rol);
public record LoginDto(string Email, string Password);