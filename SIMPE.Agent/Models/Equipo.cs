namespace SIMPE.Agent.Models
{
    public class Equipo
    {
        public string id_equipo { get; set; }
        public string nombre { get; set; }
        public string ip { get; set; }
        public string usuario { get; set; }
        public string cpu_model { get; set; }
        public double ram_total { get; set; }
        public string disco_tipo { get; set; }
        public string os_version { get; set; }
        public string antivirus_nombre { get; set; }
        public string tiempo_arranque { get; set; }
        public string tiempo_apagado { get; set; }
        public string hardware_detalles { get; set; }
        public string estado_seguridad { get; set; }
        public string ultima_actualizacion { get; set; }
        public int synced { get; set; }
    }
}
