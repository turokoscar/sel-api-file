using System;

namespace sel_api_archivos.Entidad
{
    public class AuditoriaEntity
    {
        public long IdeAuditoria { get; set; }
        public Guid IdeArchivo { get; set; }
        public string TxtAccion { get; set; } = string.Empty;
        public string? TxtUsuario { get; set; }
        public string? TxtIpOrigen { get; set; }
        public DateTime FecAcceso { get; set; }
    }
}
