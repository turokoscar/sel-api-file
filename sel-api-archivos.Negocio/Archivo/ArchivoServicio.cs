using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using sel_api_archivos.Datos;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Storage;

namespace sel_api_archivos.Negocio.Archivo
{
    public class ArchivoServicio : IArchivoServicio
    {
        private readonly IArchivoRepositorio _repositorio;
        private readonly IStorageProviderResolver _providerResolver;

        public ArchivoServicio(IArchivoRepositorio repositorio, IStorageProviderResolver providerResolver)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _providerResolver = providerResolver ?? throw new ArgumentNullException(nameof(providerResolver));
        }

        private async Task<ProveedorEntity> ObtenerProveedorActivoAsync()
        {
            var proveedores = await _repositorio.ListarProveedoresActivosAsync();
            var proveedorActivo = proveedores.FirstOrDefault(p => p.FlgActivo);
            if (proveedorActivo == null)
            {
                throw new InvalidOperationException("No se encontró ningún proveedor de almacenamiento activo en la base de datos.");
            }
            return proveedorActivo;
        }

        private string CalcularSHA256(Stream stream)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public async Task<Guid> SubirArchivoAsync(
            Stream stream, 
            string nombreOriginal, 
            string contentType, 
            string codSistema, 
            string? codProceso, 
            string usuario,
            string ipOrigen)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("El archivo a subir está vacío.", nameof(stream));

            // Asegurar que el stream esté al inicio para calcular el hash
            if (stream.CanSeek) stream.Position = 0;
            string checksum = CalcularSHA256(stream);
            if (stream.CanSeek) stream.Position = 0;

            // Obtener el proveedor de almacenamiento configurado
            var proveedor = await ObtenerProveedorActivoAsync();
            var storage = _providerResolver.Resolve(proveedor.CodProveedor);

            // Generar un nombre físico único
            string extension = Path.GetExtension(nombreOriginal);
            string nombreFisico = $"{Guid.NewGuid()}{extension}";
            string rutaRelativa = Path.Combine(codSistema, codProceso ?? "GENERAL");

            // Subir al almacenamiento físico
            await storage.UploadAsync(stream, nombreFisico, rutaRelativa, proveedor.JsnConfiguracion);

            // Registrar en base de datos
            var archivo = new ArchivoEntity
            {
                CodSistema = codSistema,
                CodProceso = codProceso,
                TxtNombreOriginal = nombreOriginal,
                TxtNombreFisico = nombreFisico,
                TxtExtension = extension,
                CanTamanioBytes = stream.Length,
                TxtContentType = contentType,
                TxtChecksumSHA256 = checksum,
                TxtRutaRelativa = rutaRelativa,
                IdeProveedor = proveedor.IdeProveedor,
                TxtCreadoPor = usuario
            };

            Guid ideArchivo = await _repositorio.RegistrarArchivoAsync(archivo);

            // Registrar auditoría de carga
            await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "UPLOAD", usuario, ipOrigen);

            return ideArchivo;
        }

        public async Task<(Stream Stream, ArchivoEntity Metadata)> DescargarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen)
        {
            var metadata = await ObtenerMetadataAsync(ideArchivo);

            var proveedores = await _repositorio.ListarProveedoresActivosAsync();
            var proveedor = proveedores.FirstOrDefault(p => p.IdeProveedor == metadata.IdeProveedor);
            if (proveedor == null)
            {
                throw new InvalidOperationException("El proveedor asociado al archivo no está disponible o está inactivo.");
            }

            var storage = _providerResolver.Resolve(proveedor.CodProveedor);
            var stream = await storage.DownloadAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion);

            // Registrar auditoría de descarga
            await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "DOWNLOAD", usuario, ipOrigen);

            return (stream, metadata);
        }

        public async Task<ArchivoEntity> ObtenerMetadataAsync(Guid ideArchivo)
        {
            var metadata = await _repositorio.ObtenerArchivoPorIdAsync(ideArchivo);
            if (metadata == null)
            {
                throw new FileNotFoundException($"No se encontró la metadata del archivo con ID: {ideArchivo}");
            }
            return metadata;
        }

        public async Task<string> LeerContenidoTextoAsync(Guid ideArchivo, string usuario, string ipOrigen)
        {
            var metadata = await ObtenerMetadataAsync(ideArchivo);

            var proveedores = await _repositorio.ListarProveedoresActivosAsync();
            var proveedor = proveedores.FirstOrDefault(p => p.IdeProveedor == metadata.IdeProveedor);
            if (proveedor == null)
            {
                throw new InvalidOperationException("El proveedor asociado al archivo no está disponible.");
            }

            var storage = _providerResolver.Resolve(proveedor.CodProveedor);
            string contenido = await storage.ReadTextContentAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion);

            // Registrar auditoría de lectura
            await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "READ", usuario, ipOrigen);

            return contenido;
        }

        public async Task<bool> EliminarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen)
        {
            var metadata = await ObtenerMetadataAsync(ideArchivo);

            var proveedores = await _repositorio.ListarProveedoresActivosAsync();
            var proveedor = proveedores.FirstOrDefault(p => p.IdeProveedor == metadata.IdeProveedor);
            if (proveedor == null)
            {
                throw new InvalidOperationException("El proveedor asociado al archivo no está disponible.");
            }

            var storage = _providerResolver.Resolve(proveedor.CodProveedor);
            
            // Eliminación física
            await storage.DeleteAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion);

            // Eliminación lógica en BD
            bool eliminado = await _repositorio.EliminarArchivoAsync(ideArchivo);

            if (eliminado)
            {
                // Registrar auditoría de eliminación
                await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "DELETE", usuario, ipOrigen);
            }

            return eliminado;
        }
    }
}
