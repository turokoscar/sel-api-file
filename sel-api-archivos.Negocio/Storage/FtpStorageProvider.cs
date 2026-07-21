using System;
using System.IO;
using System.Text;
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
    public class FtpStorageProvider : IStorageProvider
    {
        /// <summary>
        /// Código identificador del proveedor: "FTP".
        /// </summary>
        public string ProviderCode => "FTP";

        /// <summary>
        /// Simula la subida de un archivo al servidor FTP.
        /// </summary>
        /// <param name="fileStream">Stream del archivo a subir.</param>
        /// <param name="fileName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa de almacenamiento.</param>
        /// <param name="configJson">Configuración JSON con Host, Port, Username, Password.</param>
        /// <returns>URL simulada del archivo subido.</returns>
        public Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson)
        {
            EmitStubWarning("UploadAsync", fileName, relativePath);
            return Task.FromResult($"ftp://server/{relativePath}/{fileName}");
        }

        /// <summary>
        /// Simula la descarga de un archivo desde el servidor FTP.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa del archivo.</param>
        /// <param name="configJson">Configuración JSON con Host, Port, Username, Password.</param>
        /// <returns>Stream con contenido simulado.</returns>
        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("DownloadAsync", physicalName, relativePath);
            Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Contenido FTP Simulado"));
            return Task.FromResult(memoryStream);
        }

        /// <summary>
        /// Simula la eliminación de un archivo en el servidor FTP.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa del archivo.</param>
        /// <param name="configJson">Configuración JSON con Host, Port, Username, Password.</param>
        /// <returns>Siempre true.</returns>
        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("DeleteAsync", physicalName, relativePath);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Simula la lectura de contenido de texto desde el servidor FTP.
        /// </summary>
        /// <param name="physicalName">Nombre físico del archivo.</param>
        /// <param name="relativePath">Ruta relativa del archivo.</param>
        /// <param name="configJson">Configuración JSON con Host, Port, Username, Password.</param>
        /// <returns>Cadena de texto simulada.</returns>
        public Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            EmitStubWarning("ReadTextContentAsync", physicalName, relativePath);
            return Task.FromResult("Contenido de texto FTP Simulado");
        }

        private static void EmitStubWarning(string methodName, string fileName, string relativePath)
        {
            Console.Error.WriteLine(
                $"[FtpStorageProvider STUB] {methodName} llamado — esto es un stub no productivo. " +
                $"Archivo: {fileName}, Ruta: {relativePath}");
        }
    }
}
