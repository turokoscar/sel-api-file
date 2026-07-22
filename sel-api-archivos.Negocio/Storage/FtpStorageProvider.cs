using System;
using System.IO;
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
            throw new NotSupportedException(
                "FtpStorageProvider es un stub no productivo. " +
                "Para uso en producción, implemente un proveedor real con FluentFTP, WinSCP o similar.");
        }

        /// <inheritdoc />
        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            throw new NotSupportedException(
                "FtpStorageProvider es un stub no productivo. " +
                "Para uso en producción, implemente un proveedor real con FluentFTP, WinSCP o similar.");
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            throw new NotSupportedException(
                "FtpStorageProvider es un stub no productivo. " +
                "Para uso en producción, implemente un proveedor real con FluentFTP, WinSCP o similar.");
        }

        /// <inheritdoc />
        public Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            throw new NotSupportedException(
                "FtpStorageProvider es un stub no productivo. " +
                "Para uso en producción, implemente un proveedor real con FluentFTP, WinSCP o similar.");
        }
    }
}
