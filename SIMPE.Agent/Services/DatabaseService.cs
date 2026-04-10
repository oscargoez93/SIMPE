using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using SIMPE.Agent.Models;

namespace SIMPE.Agent.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Apuntar a simpe.db en la carpeta padre (c:\Users\Oscar\Documents\SIMPE\simpe.db)
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "simpe.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase(dbPath);
        }

        private IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private void InitializeDatabase(string dbPath)
        {
            // Si la base no existe, la lógica debería estar en init_db.py, 
            // pero nos aseguramos que existe. En este entorno asumiremos 
            // que schema.sql fue ejecutado o lo ejecutamos en BackgroundService.
            // SQLite by default will create the file if it doesn't exist,
            // but for tables we might need to read schema.sql later or rely on init_db.py
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"[DB] Asegúrate de haber ejecutado init_db.py. No se encontró {dbPath}");
            }
        }

        public async Task UpsertEquipoAsync(Equipo equipo)
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO equipos 
                (id_equipo, nombre, ip, usuario, cpu_model, ram_total, disco_tipo, os_version, 
                 antivirus_nombre, tiempo_arranque, tiempo_apagado, hardware_detalles, 
                 estado_seguridad, ultima_actualizacion, synced)
                VALUES
                (@id_equipo, @nombre, @ip, @usuario, @cpu_model, @ram_total, @disco_tipo, @os_version,
                 @antivirus_nombre, @tiempo_arranque, @tiempo_apagado, @hardware_detalles,
                 @estado_seguridad, @ultima_actualizacion, @synced)
                ON CONFLICT(id_equipo) DO UPDATE SET
                 nombre=excluded.nombre, ip=excluded.ip, usuario=excluded.usuario, 
                 cpu_model=excluded.cpu_model, ram_total=excluded.ram_total, 
                 disco_tipo=excluded.disco_tipo, os_version=excluded.os_version,
                 antivirus_nombre=excluded.antivirus_nombre, tiempo_arranque=excluded.tiempo_arranque,
                 ultima_actualizacion=excluded.ultima_actualizacion;
            ";

            await connection.ExecuteAsync(sql, equipo);
        }

        public async Task<Equipo> GetEquipoAsync(string idEquipo)
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM equipos WHERE id_equipo = @id_equipo LIMIT 1";
            return await connection.QueryFirstOrDefaultAsync<Equipo>(sql, new { id_equipo = idEquipo });
        }
        
        public async Task<IEnumerable<Equipo>> GetAllEquiposAsync()
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM equipos";
            return await connection.QueryAsync<Equipo>(sql);
        }
    }
}
