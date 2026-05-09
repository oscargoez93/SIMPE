using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Dapper;
using SIMPE.Agent.Models;
using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Guardar la base de datos en %LOCALAPPDATA% para evitar problemas de permisos
            // cuando la app está instalada en Archivos de programa
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SIMPE Agent");
            Directory.CreateDirectory(appDataDir);
            string dbPath = Path.Combine(appDataDir, "simpe.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase(dbPath);
        }

        private IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private void InitializeDatabase(string dbPath)
        {
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"[DB] Base de datos no encontrada. Creando {dbPath}...");
                try
                {
                    // Create the database file and run schema from embedded resource
                    var assembly = typeof(DatabaseService).Assembly;
                    var resourceName = assembly.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith("schema.sql", StringComparison.OrdinalIgnoreCase));

                    if (resourceName != null)
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        using var reader = new StreamReader(stream!);
                        var schema = reader.ReadToEnd();

                        // Execute schema
                        using var connection = new SqliteConnection(_connectionString);
                        connection.Open();
                        connection.Execute(schema);
                        Console.WriteLine("[DB] Base de datos creada exitosamente.");
                    }
                    else
                    {
                        Console.WriteLine("[DB] ERROR: No se encontró el recurso schema.sql embebido.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB] ERROR al crear la base de datos: {ex.Message}");
                }
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

        public async Task InsertHistorialNavegacionAsync(string idEquipo, NavigationHistoryEntry entry)
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO historial_navegacion 
                (id_equipo, empleado, url, titulo_pagina, navegador, fecha_ingreso, duracion_segundos, synced)
                VALUES 
                (@id_equipo, @empleado, @url, @titulo_pagina, @navegador, @fecha_ingreso, @duracion_segundos, 0);
            ";

            // Para evitar insertar duplicados exactos, primero verificamos si existe.
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM historial_navegacion WHERE id_equipo = @id_equipo AND url = @url AND fecha_ingreso = @fecha_ingreso",
                new { id_equipo = idEquipo, url = entry.url, fecha_ingreso = entry.visitedAt });

            if (exists == 0)
            {
                // Intentar extraer segundos de "duration" si es posible, por ahora lo pasamos como string o lo dejamos en 0.
                // Como SQLite es flexible, duracion_segundos es INTEGER, pero el modelo tiene un string. 
                // Lo parsearemos o usaremos 0 si no se puede.
                int duracion = 0;
                if (entry.duration.EndsWith("s") && int.TryParse(entry.duration.Replace("s", ""), out int s)) duracion = s;

                await connection.ExecuteAsync(sql, new 
                { 
                    id_equipo = idEquipo, 
                    empleado = Environment.UserName,
                    url = entry.url, 
                    titulo_pagina = entry.title,
                    navegador = entry.browser,
                    fecha_ingreso = entry.visitedAt,
                    duracion_segundos = duracion
                });
            }
        }

        public async Task InsertMetricasRendimientoAsync(string idEquipo, PerformanceMetrics metrics)
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO metricas_rendimiento 
                (tiempo, id_equipo, cpu_uso, ram_uso, disk_uso, procesos_activos, apps_detalles_red, synced)
                VALUES
                (@tiempo, @id_equipo, @cpu_uso, @ram_uso, @disk_uso, @procesos_activos, @apps_detalles_red, 0);
            ";
            
            await connection.ExecuteAsync(sql, new
            {
                tiempo = metrics.generatedAt,
                id_equipo = idEquipo,
                cpu_uso = metrics.cpu.usagePercent,
                ram_uso = metrics.memory.usagePercent,
                disk_uso = metrics.disks.FirstOrDefault()?.usagePercent ?? 0,
                procesos_activos = metrics.cpu.processCount,
                apps_detalles_red = JsonSerializer.Serialize(metrics.networks)
            });
        }

        public async Task InsertEventoSeguridadAsync(string idEquipo, string tipoEvento, string detalle, string metadata)
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO eventos_seguridad 
                (tiempo, id_equipo, tipo_evento, detalle, metadata, synced)
                VALUES
                (@tiempo, @id_equipo, @tipo_evento, @detalle, @metadata, 0);
            ";
            
            await connection.ExecuteAsync(sql, new
            {
                tiempo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                id_equipo = idEquipo,
                tipo_evento = tipoEvento,
                detalle = detalle,
                metadata = metadata
            });
        }

        public async Task DeleteOldDataAsync(int diasRetencion)
        {
            using var connection = CreateConnection();
            string fechaCorte = DateTime.Now.AddDays(-diasRetencion).ToString("yyyy-MM-dd HH:mm:ss");

            var sqlNavegacion = "DELETE FROM historial_navegacion WHERE fecha_ingreso < @fechaCorte";
            var sqlRendimiento = "DELETE FROM metricas_rendimiento WHERE tiempo < @fechaCorte";
            var sqlSeguridad = "DELETE FROM eventos_seguridad WHERE tiempo < @fechaCorte";

            await connection.ExecuteAsync(sqlNavegacion, new { fechaCorte });
            await connection.ExecuteAsync(sqlRendimiento, new { fechaCorte });
            await connection.ExecuteAsync(sqlSeguridad, new { fechaCorte });
        }
    }
}
