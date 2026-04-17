using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMPE.Dashboard.Data;
using SIMPE.Dashboard.Models;
using SIMPE.Dashboard.Services;
using System.Security.Cryptography;
using System.Text;

namespace SIMPE.Dashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly GraphEmailService _emailService;

        public AuthController(ApplicationDbContext context, GraphEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("El correo ya está registrado.");

            // Crear y popular el nuevo usuario
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                IsApproved = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Enviar correo de aprobación
            await _emailService.SendApprovalEmailAsync(user);

            return Ok(new { message = "Registro exitoso. Espera a que el administrador apruebe tu cuenta." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Nota: Para simplificar usamos el Email como Username para comparaciones en app.js (modificado a username pero contiene el mail realmente o adaptamos el API)
            // Permitimos el dummy 'admin' para no romper la maqueta si aún se necesita probar rápidamente
            if (request.Username == "admin" && request.Password == "admin")
                return Ok(new { redirect = "dashboard.html" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Username);
            if (user == null)
                return Unauthorized("Credenciales incorrectas o usuario inexistente.");

            if (user.PasswordHash != HashPassword(request.Password))
                return Unauthorized("Credenciales incorrectas.");

            if (!user.IsApproved)
                return Unauthorized("Tu cuenta aún NO ha sido aprobada por el administrador.");

            return Ok(new { redirect = "dashboard.html" });
        }

        [HttpGet("approve/{id}")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Usuario no encontrado.");

            string htmlBase = @"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>SIMPE Dashboard - Aprobación</title>
                <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap' rel='stylesheet'>
                <style>
                    body {
                        background-color: #0a0e17;
                        color: #f8fafc;
                        font-family: 'Outfit', sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                        background-image: radial-gradient(circle at 10% 20%, rgba(153, 55, 10, 0.4) 0%, transparent 40%);
                    }
                    .card {
                        background: rgba(22, 28, 45, 0.8);
                        backdrop-filter: blur(16px);
                        border: 1px solid rgba(255, 255, 255, 0.08);
                        border-radius: 20px;
                        padding: 40px;
                        text-align: center;
                        box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
                        max-width: 450px;
                        width: 90%;
                    }
                    h1 { color: #F47920; font-size: 1.8rem; margin-bottom: 20px; }
                    p { color: #94a3b8; font-size: 1.05rem; margin-bottom: 30px; line-height: 1.5; }
                    .btn {
                        background: #F47920;
                        color: #0a0e17;
                        border: none;
                        padding: 14px 28px;
                        border-radius: 12px;
                        font-weight: 600;
                        text-decoration: none;
                        display: inline-block;
                        transition: all 0.3s;
                        font-size: 1rem;
                    }
                    .btn:hover { background: #E3640C; transform: translateY(-2px); box-shadow: 0 10px 20px -10px rgba(244, 121, 32, 0.5); }
                </style>
            </head>
            <body>
                <div class='card'>
                    @@CONTENT@@
                </div>
            </body>
            </html>";

            if (user.IsApproved)
            {
                var msgAlready = htmlBase.Replace("@@CONTENT@@", "<h1>Aviso</h1><p>El usuario ya había sido aprobado anteriormente.</p><a href='/' class='btn'>Volver al Inicio</a>");
                return Content(msgAlready, "text/html", Encoding.UTF8);
            }

            user.IsApproved = true;
            await _context.SaveChangesAsync();

            var msgSuccess = htmlBase.Replace("@@CONTENT@@", "<h1>¡Aprobado Correctamente!</h1><p>El usuario de SIMPE Dashboard ya tiene permisos y puede iniciar sesión.</p><a href='/' class='btn'>Ir al Portal</a>");
            return Content(msgSuccess, "text/html", Encoding.UTF8);
        }

        private string HashPassword(string password)
        {
            // Simple SHA256 implementado con fines prácticos. Para nivel de producción se usa BCrypt/PBKDF2.
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
