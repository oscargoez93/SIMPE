using System.ComponentModel.DataAnnotations;

namespace SIMPE.Dashboard.Models
{
    public class Equipo
    {
        [Key]
        public string id_equipo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string ip { get; set; } = string.Empty;
        public string usuario { get; set; } = string.Empty;
        public string cpu_model { get; set; } = string.Empty;
        public double ram_total { get; set; }
        public string disco_tipo { get; set; } = string.Empty;
        public string os_version { get; set; } = string.Empty;
        public string antivirus_nombre { get; set; } = string.Empty;
        public string tiempo_arranque { get; set; } = string.Empty;
        public string tiempo_apagado { get; set; } = string.Empty;
        public string hardware_detalles { get; set; } = string.Empty;
        public string estado_seguridad { get; set; } = string.Empty;
        public string ultima_actualizacion { get; set; } = string.Empty;
        public int synced { get; set; }
    }
}
