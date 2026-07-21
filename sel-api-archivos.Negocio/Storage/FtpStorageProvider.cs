using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Proveedor de almacenamiento FTP stub.
    /// </summary>
    /// <remarks>
    /// <para><strong>⚠️ STUB — NO LISTO PARA PRODUCCIÓN</strong></para>
    /// Este proveedor simula operaciones FTP usando Console.WriteLine y streams en memoria.
    /// No debe usarse en entornos de producción. Para producción, implementar un proveedor
    /// real con FluentFTP, WinSCP o similar, y eliminar esta clase.
    /// </remarks>
    public sealed class FtpStorageProvider : IStorageProvider
    {
        /// <summary>
        /// Código identificador del proveedor: "FTP".
        /// </summary>
        public string ProviderCode => "FTP";

        /// <inheritdoc />
        public Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson)
        {
            EmitStubWarning("UploadAsync", fileName, relativePath, configJson);
            return Task.FromResult($"ftp://server/{relativePath}/{fileName}");
        }

        /// <inheritdoc />
        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("DownloadAsync", physicalName, relativePath, configJson);
            Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Contenido FTP Simulado"));
            return Task.FromResult(memoryStream);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("DeleteAsync", physicalName, relativePath, configJson);
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("ReadTextContentAsync", physicalName, relativePath, configJson);
            return Task.FromResult("Contenido de texto FTP Simulado");
        }

        private static void EmitStubWarning(string methodName, string fileName, string relativePath, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson);
            Console.Error.WriteLine(
                $"[FtpStorageProvider STUB] {methodName} — stub no productivo. " +
                $"Archivo: {fileName}, Ruta: {relativePath}, " +
                $"Límite: {config?.MaxFileSizeBytes ?? 0} bytes, " +
                $"Tipos permitidos: {config?.AllowedContentTypes?.Count ?? 0}");
        }
    }
}
