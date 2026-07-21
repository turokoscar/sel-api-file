using System.Text.Json.Serialization;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Configuración de un proveedor de almacenamiento. Se deserializa desde el campo
    /// <c>JsnConfiguracion</c> de la tabla <c>ARC.SEL_ARC_TG_PROVEEDOR</c>.
    /// </summary>
    public sealed class StorageProviderConfig
    {
        /// <summary>
        /// Ruta base del almacenamiento local. Usado únicamente por el proveedor <c>LOCAL</c>.
        /// </summary>
        [JsonPropertyName("localStoragePath")]
        public string LocalStoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño máximo permitido del archivo en bytes. Si es 0, no hay límite.
        /// </summary>
        [JsonPropertyName("maxFileSizeBytes")]
        public long MaxFileSizeBytes { get; set; }

        /// <summary>
        /// Lista de Content-Types permitidos. Si está vacío, se permiten todos.
        /// Ejemplo: ["application/pdf", "image/png", "image/jpeg"]
        /// </summary>
        [JsonPropertyName("allowedContentTypes")]
        public List<string> AllowedContentTypes { get; set; } = new();
    }
}
