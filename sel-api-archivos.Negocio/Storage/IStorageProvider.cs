using System.IO;
using System.Threading.Tasks;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Define la abstracción para un proveedor de almacenamiento de archivos.
    /// Implementa el patrón Strategy para permitir múltiples backends de almacenamiento
    /// (local, FTP, S3, etc.).
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Código identificador del proveedor (ej: "LOCAL", "FTP", "AWS_S3").
        /// Usado por el <see cref="IStorageProviderResolver"/> para seleccionar el proveedor correcto.
        /// </summary>
        string ProviderCode { get; }

        /// <summary>
        /// Sube un archivo al almacenamiento.
        /// </summary>
        /// <param name="fileStream">Stream de solo lectura del contenido del archivo.</param>
        /// <param name="fileName">Nombre físico único del archivo.</param>
        /// <param name="relativePath">Ruta relativa dentro del almacenamiento (ej: "KOFIX/FACTURACION").</param>
        /// <param name="configJson">JSON de configuración específico del proveedor.</param>
        /// <returns>Ruta completa o URL del archivo almacenado.</returns>
        Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson);

        /// <summary>
        /// Descarga un archivo desde el almacenamiento.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa donde se encuentra el archivo.</param>
        /// <param name="configJson">JSON de configuración específico del proveedor.</param>
        /// <returns>Stream de solo lectura del contenido del archivo.</returns>
        /// <exception cref="FileNotFoundException">Cuando el archivo no existe en el almacenamiento.</exception>
        Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson);

        /// <summary>
        /// Elimina un archivo del almacenamiento.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa donde se encuentra el archivo.</param>
        /// <param name="configJson">JSON de configuración específico del proveedor.</param>
        /// <returns><c>true</c> si el archivo fue eliminado; <c>false</c> si no existía.</returns>
        Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson);

        /// <summary>
        /// Lee el contenido de texto de un archivo.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa donde se encuentra el archivo.</param>
        /// <param name="configJson">JSON de configuración específico del proveedor.</param>
        /// <returns>Contenido del archivo como texto UTF-8.</returns>
        /// <exception cref="FileNotFoundException">Cuando el archivo no existe en el almacenamiento.</exception>
        Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson);
    }
}
