using Microsoft.Extensions.Logging;

namespace sel_api_archivos.Api.Logging
{
    /// <summary>
    /// Identificadores de evento para logging estructurado.
    /// Usados como <see cref="EventId"/> en todos los logs del sistema.
    /// </summary>
    public static class LogEventIds
    {
        /// <summary>Upload de archivo exitoso.</summary>
        public static readonly EventId ArchivoUploadSuccess = new(1001, nameof(ArchivoUploadSuccess));
        /// <summary>Error en upload de archivo.</summary>
        public static readonly EventId ArchivoUploadError = new(1002, nameof(ArchivoUploadError));
        /// <summary>Descarga de archivo exitosa.</summary>
        public static readonly EventId ArchivoDownloadSuccess = new(1003, nameof(ArchivoDownloadSuccess));
        /// <summary>Error en descarga de archivo.</summary>
        public static readonly EventId ArchivoDownloadError = new(1004, nameof(ArchivoDownloadError));
        /// <summary>Lectura de contenido de archivo exitosa.</summary>
        public static readonly EventId ArchivoReadSuccess = new(1005, nameof(ArchivoReadSuccess));
        /// <summary>Error en lectura de contenido de archivo.</summary>
        public static readonly EventId ArchivoReadError = new(1006, nameof(ArchivoReadError));
        /// <summary>Eliminación de archivo exitosa.</summary>
        public static readonly EventId ArchivoDeleteSuccess = new(1007, nameof(ArchivoDeleteSuccess));
        /// <summary>Error en eliminación de archivo.</summary>
        public static readonly EventId ArchivoDeleteError = new(1008, nameof(ArchivoDeleteError));
        /// <summary>Obtención de metadatos de archivo exitosa.</summary>
        public static readonly EventId ArchivoMetadataSuccess = new(1009, nameof(ArchivoMetadataSuccess));
        /// <summary>Error al obtener metadatos de archivo.</summary>
        public static readonly EventId ArchivoMetadataError = new(1010, nameof(ArchivoMetadataError));

        /// <summary>Archivo vacío detectado en validación.</summary>
        public static readonly EventId ArchivoVacio = new(1101, nameof(ArchivoVacio));
        /// <summary>Tamaño de archivo excede el máximo permitido.</summary>
        public static readonly EventId ArchivoTamanioExcedido = new(1102, nameof(ArchivoTamanioExcedido));
        /// <summary>Tipo de contenido no permitido.</summary>
        public static readonly EventId ArchivoTipoNoPermitido = new(1103, nameof(ArchivoTipoNoPermitido));
        /// <summary>Archivo no encontrado en base de datos.</summary>
        public static readonly EventId ArchivoNoEncontrado = new(1104, nameof(ArchivoNoEncontrado));

        /// <summary>Proveedor de almacenamiento no disponible o inactivo.</summary>
        public static readonly EventId ProveedorNoDisponible = new(1201, nameof(ProveedorNoDisponible));
        /// <summary>Error al interactuar con el proveedor de almacenamiento.</summary>
        public static readonly EventId StorageProviderError = new(1202, nameof(StorageProviderError));

        /// <summary>Filtro de respuesta aplicado exitosamente.</summary>
        public static readonly EventId ResponseWrapperApplied = new(2001, nameof(ResponseWrapperApplied));
        /// <summary>Excepción manejada por el filtro global.</summary>
        public static readonly EventId ExceptionHandled = new(2002, nameof(ExceptionHandled));

        /// <summary>Inicio de procesamiento de solicitud HTTP.</summary>
        public static readonly EventId RequestStart = new(3001, nameof(RequestStart));
        /// <summary>Fin de procesamiento de solicitud HTTP.</summary>
        public static readonly EventId RequestEnd = new(3002, nameof(RequestEnd));
    }
}
