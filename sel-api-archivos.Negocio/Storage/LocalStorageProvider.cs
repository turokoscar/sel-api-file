using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sel_api_archivos.Negocio.Storage
{
    public class LocalStorageProvider : IStorageProvider
    {
        public string ProviderCode => "LOCAL";

        private class LocalConfig
        {
            public string LocalStoragePath { get; set; } = string.Empty;
        }

        private string GetFullPath(string relativePath, string physicalName, string configJson)
        {
            var config = JsonSerializer.Deserialize<LocalConfig>(configJson);
            var basePath = config?.LocalStoragePath ?? Path.GetTempPath();

            var fullDirectory = Path.Combine(basePath, relativePath);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            return Path.Combine(fullDirectory, physicalName);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string relativePath, string configJson)
        {
            var fullPath = GetFullPath(relativePath, fileName, configJson);
            using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fileStream.CopyToAsync(targetStream);
            return fullPath;
        }

        public Task<Stream> DownloadAsync(string physicalName, string relativePath, string configJson)
        {
            var fullPath = GetFullPath(relativePath, physicalName, configJson);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("El archivo físico no se encuentra en el almacenamiento local.", fullPath);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task<bool> DeleteAsync(string physicalName, string relativePath, string configJson)
        {
            var fullPath = GetFullPath(relativePath, physicalName, configJson);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task<string> ReadTextContentAsync(string physicalName, string relativePath, string configJson)
        {
            var fullPath = GetFullPath(relativePath, physicalName, configJson);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("El archivo de texto no existe en el almacenamiento local.", fullPath);
            }

            return await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
        }
    }
}
