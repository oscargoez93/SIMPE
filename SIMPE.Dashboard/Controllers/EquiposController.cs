using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMPE.Dashboard.Data;
using SIMPE.Dashboard.Models;

namespace SIMPE.Dashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquiposController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EquiposController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEquipos()
        {
            var equipos = await _context.Equipos.ToListAsync();
            return Ok(equipos);
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncEquipo([FromBody] Equipo equipo)
        {
            if (equipo == null || string.IsNullOrEmpty(equipo.id_equipo))
            {
                return BadRequest("Datos de equipo inválidos.");
            }

            var existingEquipo = await _context.Equipos.FirstOrDefaultAsync(e => e.id_equipo == equipo.id_equipo);

            if (existingEquipo != null)
            {
                // Update properties
                existingEquipo.nombre = equipo.nombre;
                existingEquipo.ip = equipo.ip;
                existingEquipo.usuario = equipo.usuario;
                existingEquipo.cpu_model = equipo.cpu_model;
                existingEquipo.ram_total = equipo.ram_total;
                existingEquipo.disco_tipo = equipo.disco_tipo;
                existingEquipo.os_version = equipo.os_version;
                existingEquipo.antivirus_nombre = equipo.antivirus_nombre;
                existingEquipo.tiempo_arranque = equipo.tiempo_arranque;
                existingEquipo.tiempo_apagado = equipo.tiempo_apagado;
                existingEquipo.hardware_detalles = equipo.hardware_detalles;
                existingEquipo.estado_seguridad = equipo.estado_seguridad;
                existingEquipo.ultima_actualizacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                existingEquipo.synced = 1;
            }
            else
            {
                equipo.ultima_actualizacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                equipo.synced = 1;
                _context.Equipos.Add(equipo);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sincronización exitosa." });
        }
    }
}
