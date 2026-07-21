using System;

namespace sel_api_archivos.Entidad
{
    /// <summary>
    /// Representa un registro de auditoría de acceso a un archivo.
    /// </summary>
    public class AuditoriaEntity
    {
        /// <summary>
        /// Identificador único del registro de auditoría.
        /// </summary>
        public long IdeAuditoria { get; set; }

        /// <summary>
        /// Identificador del archivo asociado.
        /// </summary>
        public Guid IdeArchivo { get; set; }

        /// <summary>
        /// Acción realizada: "UPLOAD", "DOWNLOAD", "READ", "DELETE".
        /// </summary>
        public string TxtAccion { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del usuario que realizó la acción.
        /// </summary>
        public string? TxtUsuario { get; set; }

        /// <summary>
        /// Dirección IP de origen de la solicitud.
        /// </summary>
        public string? TxtIpOrigen { get; set; }

        /// <summary>
        /// Fecha y hora en que se realizó la acción.
        /// </summary>
        public DateTime FecAcceso { get; set; }
    }
}
