using System;

namespace sel_api_archivos.Entidad
{
    /// <summary>
    /// Representa la metadata de un archivo almacenado en el sistema.
    /// </summary>
    public class ArchivoEntity
    {
        /// <summary>
        /// Identificador único del archivo (GUID).
        /// </summary>
        public Guid IdeArchivo { get; set; }

        /// <summary>
        /// Código del sistema consumidor que originó la subida (ej: "KOFIX", "SAT", "SEL").
        /// </summary>
        public string CodSistema { get; set; } = string.Empty;

        /// <summary>
        /// Código opcional del proceso dentro del sistema (ej: "FACTURACION", "CONTRATOS").
        /// </summary>
        public string? CodProceso { get; set; }

        /// <summary>
        /// Nombre original del archivo tal como fue subido por el usuario.
        /// </summary>
        public string TxtNombreOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Nombre físico único asignado internamente al archivo (GUID + extensión).
        /// </summary>
        public string TxtNombreFisico { get; set; } = string.Empty;

        /// <summary>
        /// Extensión del archivo incluyendo el punto (ej: ".pdf", ".png").
        /// </summary>
        public string TxtExtension { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del archivo en bytes.
        /// </summary>
        public long CanTamanioBytes { get; set; }

        /// <summary>
        /// Content-Type MIME del archivo (ej: "application/pdf", "image/png").
        /// </summary>
        public string TxtContentType { get; set; } = string.Empty;

        /// <summary>
        /// Hash SHA256 del contenido del archivo en minúsculas sin guiones.
        /// </summary>
        public string TxtChecksumSHA256 { get; set; } = string.Empty;

        /// <summary>
        /// Ruta relativa de almacenamiento dentro del proveedor (ej: "KOFIX/FACTURACION").
        /// </summary>
        public string TxtRutaRelativa { get; set; } = string.Empty;

        /// <summary>
        /// Identificador del proveedor de almacenamiento asociado.
        /// </summary>
        public int IdeProveedor { get; set; }

        /// <summary>
        /// Estado del archivo: <c>true</c> = activo, <c>false</c> = eliminado (soft delete).
        /// </summary>
        public bool EstArchivo { get; set; }

        /// <summary>
        /// Nombre del usuario que realizó la subida.
        /// </summary>
        public string? TxtCreadoPor { get; set; }

        /// <summary>
        /// Fecha y hora de creación del registro.
        /// </summary>
        public DateTime FecCreacion { get; set; }
    }
}
