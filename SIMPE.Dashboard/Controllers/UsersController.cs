using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMPE.Dashboard.Data;
using System.Security.Cryptography;
using System.Text;

namespace SIMPE.Dashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsApproved,
                    u.CreatedAt
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/users/{id}/reset-password
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("La contraseña no puede estar vacía.");
            }

            // Uso de mismo método criptográfico MD5/SHA de la aplicación cliente
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.NewPassword));
            user.PasswordHash = Convert.ToBase64String(bytes);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente." });
        }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
