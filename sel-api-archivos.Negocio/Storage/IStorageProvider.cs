using System.IO;
using System.Threading.Tasks;

namespace sel_api_archivos.Negocio.Storage
{
    public interface IStorageProvider
    {
        string ProviderCode { get; }
        Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson);
        Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson);
        Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson);
        Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson);
    }
}
