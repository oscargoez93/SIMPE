-- Tabla: equipos
CREATE TABLE IF NOT EXISTS equipos (
    id_equipo TEXT PRIMARY KEY,
    nombre TEXT,
    ip TEXT,
    usuario TEXT,
    cpu_model TEXT,
    ram_total REAL,
    disco_tipo TEXT,
    os_version TEXT,
    antivirus_nombre TEXT,
    tiempo_arranque TEXT,
    tiempo_apagado TEXT,
    hardware_detalles TEXT,
    estado_seguridad TEXT,
    ultima_actualizacion TEXT,
    synced INTEGER
);

-- Tabla: eventos_seguridad
CREATE TABLE IF NOT EXISTS eventos_seguridad (
    tiempo TEXT,
    id_equipo TEXT,
    tipo_evento TEXT,
    detalle TEXT,
    metadata TEXT,
    synced INTEGER
);

-- Tabla: historial_navegacion
CREATE TABLE IF NOT EXISTS historial_navegacion (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_equipo TEXT,
    empleado TEXT,
    url TEXT,
    titulo_pagina TEXT,
    navegador TEXT,
    fecha_ingreso TEXT,
    duracion_segundos INTEGER,
    synced INTEGER
);

-- Tabla: metricas_rendimiento
CREATE TABLE IF NOT EXISTS metricas_rendimiento (
    tiempo TEXT,
    id_equipo TEXT,
    cpu_uso REAL,
    ram_uso REAL,
    disk_uso REAL,
    gpu_uso REAL,
    latencia REAL,
    jitter REAL,
    perdida_paquetes REAL,
    procesos_activos INTEGER,
    interfaz_red TEXT,
    ip_address TEXT,
    wifi INTEGER,
    apps_activas INTEGER,
    apps_detalles_red TEXT,
    synced INTEGER
);
