using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace sel_api_archivos.Negocio.Storage
{
    public class FtpStorageProvider : IStorageProvider
    {
        public string ProviderCode => "FTP";

        public Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson)
        {
            // Simulación/Estructura base para FTP usando FluentFTP o WebRequest
            Console.WriteLine($"[FTP] Subiendo archivo {fileName} a ruta {relativePath} usando config {configJson}");
            return Task.FromResult($"ftp://server/{relativePath}/{fileName}");
        }

        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            Console.WriteLine($"[FTP] Descargando archivo {physicalName} usando config {configJson}");
            Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Contenido FTP Simulado"));
            return Task.FromResult(memoryStream);
        }

        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            Console.WriteLine($"[FTP] Eliminando archivo {physicalName} usando config {configJson}");
            return Task.FromResult(true);
        }

        public Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            return Task.FromResult("Contenido de texto FTP Simulado");
        }
    }
}
