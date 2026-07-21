using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using sel_api_archivos.Negocio.Storage;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Proveedor de almacenamiento en el sistema de archivos local.
    /// </summary>
    public sealed class LocalStorageProvider : IStorageProvider
    {
        /// <summary>
        /// Código identificador del proveedor: "LOCAL".
        /// </summary>
        public string ProviderCode => "LOCAL";

        /// <summary>
        /// Obtiene la ruta completa combinando la ruta base del proveedor con la ruta relativa
        /// y el nombre físico del archivo.
        /// </summary>
        /// <param name="relativePath">Ruta relativa dentro del almacenamiento.</param>
        /// <param name="physicalName">Nombre físico único del archivo.</param>
        /// <param name="config">Configuración del proveedor.</param>
        /// <returns>Ruta absoluta del archivo.</returns>
        private string GetFullPath(string relativePath, string physicalName, StorageProviderConfig config)
        {
            var basePath = string.IsNullOrWhiteSpace(config.LocalStoragePath)
                ? Path.GetTempPath()
                : config.LocalStoragePath;

            var fullDirectory = Path.Combine(basePath, relativePath);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            return Path.Combine(fullDirectory, physicalName);
        }

        /// <inheritdoc />
        public async Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson) ?? new StorageProviderConfig();
            var fullPath = GetFullPath(relativePath, fileName, config);
            using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fileStream.CopyToAsync(targetStream).ConfigureAwait(false);
            return fullPath;
        }

        /// <inheritdoc />
        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson) ?? new StorageProviderConfig();
            var fullPath = GetFullPath(relativePath, physicalName, config);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"El archivo '{physicalName}' no se encontró en la ruta '{relativePath}'.",
                    fullPath);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson) ?? new StorageProviderConfig();
            var fullPath = GetFullPath(relativePath, physicalName, config);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public async Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson) ?? new StorageProviderConfig();
            var fullPath = GetFullPath(relativePath, physicalName, config);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"El archivo '{physicalName}' no se encontró para lectura de contenido.",
                    fullPath);
            }

            return await File.ReadAllTextAsync(fullPath, Encoding.UTF8).ConfigureAwait(false);
        }
    }
}
