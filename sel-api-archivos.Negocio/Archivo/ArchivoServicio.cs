using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using sel_api_archivos.Datos;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Storage;

namespace sel_api_archivos.Negocio.Archivo
{
    /// <summary>
    /// Servicio de coordinación para operaciones de archivo. Gestiona la lógica de negocio,
    /// validaciones y auditoría, delegando el almacenamiento físico a los proveedores
    /// configurados via <see cref="IStorageProvider"/>.
    /// </summary>
    public sealed class ArchivoServicio : IArchivoServicio
    {
        private readonly IArchivoRepositorio _repositorio;
        private readonly IStorageProviderResolver _providerResolver;
        private readonly ILogger<ArchivoServicio> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ArchivoServicio"/>.
        /// </summary>
        /// <param name="repositorio">Repositorio de acceso a datos de archivos.</param>
        /// <param name="providerResolver">Resolvedor de proveedores de almacenamiento.</param>
        /// <param name="logger">Logger para trazas estructuradas.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public ArchivoServicio(
            IArchivoRepositorio repositorio,
            IStorageProviderResolver providerResolver,
            ILogger<ArchivoServicio> logger)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _providerResolver = providerResolver ?? throw new ArgumentNullException(nameof(providerResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Guid> SubirArchivoAsync(
            Stream stream,
            string nombreOriginal,
            string contentType,
            string codSistema,
            string? codProceso,
            string usuario,
            string ipOrigen)
        {
            var correlationId = Guid.NewGuid().ToString();
            var sw = Stopwatch.StartNew();

            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["CodSistema"] = codSistema,
                ["CodProceso"] = codProceso ?? "GENERAL",
                ["User"] = usuario,
                ["IpOrigen"] = ipOrigen
            });

            try
            {
                ArgumentNullException.ThrowIfNull(stream);
                ArgumentException.ThrowIfNullOrWhiteSpace(nombreOriginal);
                ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
                ArgumentException.ThrowIfNullOrWhiteSpace(codSistema);

                if (stream.Length == 0)
                {
                    _logger.LogError(
                        "ARCHIVO_VACIO_0001: El archivo a subir está vacío. " +
                        "NombreOriginal: {NombreOriginal}, ContentType: {ContentType}",
                        nombreOriginal, contentType);
                    throw new ArgumentException("ARCHIVO_VACIO_0001: El archivo a subir está vacío.", nameof(stream));
                }

                var proveedor = await ObtenerProveedorActivoAsync().ConfigureAwait(false);
                ValidarArchivoContraProveedor(stream, contentType, proveedor.JsnConfiguracion);

                if (stream.CanSeek) stream.Position = 0;
                string checksum = CalcularSHA256(stream);
                if (stream.CanSeek) stream.Position = 0;

                var storage = _providerResolver.Resolve(proveedor.CodProveedor);

                string extension = Path.GetExtension(nombreOriginal);
                string nombreFisico = $"{Guid.NewGuid()}{extension}";
                string rutaRelativa = Path.Combine(codSistema, codProceso ?? "GENERAL");

                await storage.UploadAsync(stream, nombreFisico, rutaRelativa, proveedor.JsnConfiguracion).ConfigureAwait(false);

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

                Guid ideArchivo = await _repositorio.RegistrarArchivoAsync(archivo).ConfigureAwait(false);
                await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "UPLOAD", usuario, ipOrigen).ConfigureAwait(false);

                sw.Stop();
                _logger.LogInformation(
                    "Archivo subido exitosamente. " +
                    "EventId: {EventId}, FileId: {FileId}, FileSizeBytes: {FileSizeBytes}, " +
                    "ContentType: {ContentType}, StorageProvider: {StorageProvider}, DurationMs: {DurationMs}",
                    1001, ideArchivo, stream.Length, contentType, proveedor.CodProveedor, sw.ElapsedMilliseconds);

                return ideArchivo;
            }
            catch (ArgumentException ex) when (ex.Message.StartsWith("ARCHIVO_"))
            {
                sw.Stop();
                _logger.LogError(
                    "Error de validación en subida de archivo. " +
                    "EventId: {EventId}, ErrorCode: {ErrorCode}, Message: {Message}, DurationMs: {DurationMs}",
                    1002, ex.Message.Split(':')[0], ex.Message, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Error inesperado en subida de archivo. " +
                    "EventId: {EventId}, ErrorCode: ERROR_INTERNO_0001, Message: {Message}, DurationMs: {DurationMs}",
                    1002, ex.Message, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(Stream Stream, ArchivoEntity Metadata)> DescargarArchivoAsync(
            Guid ideArchivo,
            string usuario,
            string ipOrigen)
        {
            var correlationId = Guid.NewGuid().ToString();
            var sw = Stopwatch.StartNew();

            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["FileId"] = ideArchivo,
                ["User"] = usuario,
                ["IpOrigen"] = ipOrigen
            });

            try
            {
                var metadata = await ObtenerMetadataAsync(ideArchivo).ConfigureAwait(false);
                var proveedor = await ObtenerProveedorPorIdAsync(metadata.IdeProveedor).ConfigureAwait(false);

                var storage = _providerResolver.Resolve(proveedor.CodProveedor);
                var stream = await storage
                    .DownloadAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion)
                    .ConfigureAwait(false);

                await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "DOWNLOAD", usuario, ipOrigen).ConfigureAwait(false);

                sw.Stop();
                _logger.LogInformation(
                    "Archivo descargado exitosamente. " +
                    "EventId: {EventId}, FileId: {FileId}, FileSizeBytes: {FileSizeBytes}, " +
                    "StorageProvider: {StorageProvider}, DurationMs: {DurationMs}",
                    1003, ideArchivo, metadata.CanTamanioBytes, proveedor.CodProveedor, sw.ElapsedMilliseconds);

                return (stream, metadata);
            }
            catch (FileNotFoundException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    "Archivo no encontrado para descarga. " +
                    "EventId: {EventId}, FileId: {FileId}, ErrorCode: {ErrorCode}, Message: {Message}, DurationMs: {DurationMs}",
                    1004, ideArchivo, "ARCHIVO_NO_ENCONTRADO", ex.Message, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Error inesperado en descarga de archivo. " +
                    "EventId: {EventId}, FileId: {FileId}, ErrorCode: ERROR_INTERNO_0001, DurationMs: {DurationMs}",
                    1004, ideArchivo, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ArchivoEntity> ObtenerMetadataAsync(Guid ideArchivo)
        {
            var correlationId = Guid.NewGuid().ToString();
            var sw = Stopwatch.StartNew();

            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["FileId"] = ideArchivo
            });

            try
            {
                var metadata = await _repositorio.ObtenerArchivoPorIdAsync(ideArchivo).ConfigureAwait(false);
                if (metadata == null)
                {
                    _logger.LogWarning(
                        "Metadata de archivo no encontrada. " +
                        "EventId: {EventId}, FileId: {FileId}, ErrorCode: ARCHIVO_NO_ENCONTRADO_0001, DurationMs: {DurationMs}",
                        1010, ideArchivo, sw.ElapsedMilliseconds);
                    throw new FileNotFoundException($"ARCHIVO_NO_ENCONTRADO_0001: No se encontró la metadata del archivo con ID: {ideArchivo}");
                }

                sw.Stop();
                _logger.LogInformation(
                    "Metadata obtenida exitosamente. " +
                    "EventId: {EventId}, FileId: {FileId}, ContentType: {ContentType}, DurationMs: {DurationMs}",
                    1009, ideArchivo, metadata.TxtContentType, sw.ElapsedMilliseconds);

                return metadata;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Error inesperado al obtener metadata. " +
                    "EventId: {EventId}, FileId: {FileId}, ErrorCode: ERROR_INTERNO_0001, DurationMs: {DurationMs}",
                    1010, ideArchivo, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> LeerContenidoTextoAsync(Guid ideArchivo, string usuario, string ipOrigen)
        {
            var correlationId = Guid.NewGuid().ToString();
            var sw = Stopwatch.StartNew();

            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["FileId"] = ideArchivo,
                ["User"] = usuario,
                ["IpOrigen"] = ipOrigen
            });

            try
            {
                var metadata = await ObtenerMetadataAsync(ideArchivo).ConfigureAwait(false);
                var proveedor = await ObtenerProveedorPorIdAsync(metadata.IdeProveedor).ConfigureAwait(false);

                var storage = _providerResolver.Resolve(proveedor.CodProveedor);
                string contenido = await storage
                    .ReadTextContentAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion)
                    .ConfigureAwait(false);

                await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "READ", usuario, ipOrigen).ConfigureAwait(false);

                sw.Stop();
                _logger.LogInformation(
                    "Contenido de texto leído exitosamente. " +
                    "EventId: {EventId}, FileId: {FileId}, ContentLength: {ContentLength}, DurationMs: {DurationMs}",
                    1005, ideArchivo, contenido.Length, sw.ElapsedMilliseconds);

                return contenido;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Error inesperado al leer contenido de texto. " +
                    "EventId: {EventId}, FileId: {FileId}, ErrorCode: ERROR_INTERNO_0001, DurationMs: {DurationMs}",
                    1006, ideArchivo, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> EliminarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen)
        {
            var correlationId = Guid.NewGuid().ToString();
            var sw = Stopwatch.StartNew();

            using var _ = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["FileId"] = ideArchivo,
                ["User"] = usuario,
                ["IpOrigen"] = ipOrigen
            });

            try
            {
                var metadata = await ObtenerMetadataAsync(ideArchivo).ConfigureAwait(false);
                var proveedor = await ObtenerProveedorPorIdAsync(metadata.IdeProveedor).ConfigureAwait(false);

                var storage = _providerResolver.Resolve(proveedor.CodProveedor);
                await storage.DeleteAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion).ConfigureAwait(false);

                bool eliminado = await _repositorio.EliminarArchivoAsync(ideArchivo).ConfigureAwait(false);

                if (eliminado)
                {
                    await _repositorio.RegistrarAuditoriaAsync(ideArchivo, "DELETE", usuario, ipOrigen).ConfigureAwait(false);
                }

                sw.Stop();
                _logger.LogInformation(
                    "Archivo eliminado. " +
                    "EventId: {EventId}, FileId: {FileId}, Eliminado: {Eliminado}, DurationMs: {DurationMs}",
                    1007, ideArchivo, eliminado, sw.ElapsedMilliseconds);

                return eliminado;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Error inesperado al eliminar archivo. " +
                    "EventId: {EventId}, FileId: {FileId}, ErrorCode: ERROR_INTERNO_0001, DurationMs: {DurationMs}",
                    1008, ideArchivo, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Obtiene el proveedor de almacenamiento activo por defecto.
        /// </summary>
        /// <returns>Entidad del proveedor activo.</returns>
        /// <exception cref="InvalidOperationException">Cuando no hay ningún proveedor activo.</exception>
        private async Task<ProveedorEntity> ObtenerProveedorActivoAsync()
        {
            var proveedores = await _repositorio.ListarProveedoresActivosAsync().ConfigureAwait(false);
            var proveedorActivo = proveedores.FirstOrDefault(p => p.FlgActivo);
            if (proveedorActivo == null)
            {
                _logger.LogError(
                    "No se encontró ningún proveedor de almacenamiento activo. " +
                    "EventId: {EventId}, ErrorCode: PROVEEDOR_NO_DISPONIBLE_0001",
                    1201);
                throw new InvalidOperationException("PROVEEDOR_NO_DISPONIBLE_0001: No se encontró ningún proveedor de almacenamiento activo en la base de datos.");
            }
            return proveedorActivo;
        }

        /// <summary>
        /// Obtiene un proveedor de almacenamiento por su identificador.
        /// </summary>
        /// <param name="ideProveedor">Identificador del proveedor.</param>
        /// <returns>Entidad del proveedor.</returns>
        /// <exception cref="InvalidOperationException">Cuando el proveedor no existe o está inactivo.</exception>
        private async Task<ProveedorEntity> ObtenerProveedorPorIdAsync(int ideProveedor)
        {
            var proveedores = await _repositorio.ListarProveedoresActivosAsync().ConfigureAwait(false);
            var proveedor = proveedores.FirstOrDefault(p => p.IdeProveedor == ideProveedor);
            if (proveedor == null)
            {
                _logger.LogError(
                    "Proveedor de almacenamiento no disponible. " +
                    "EventId: {EventId}, IdeProveedor: {IdeProveedor}, ErrorCode: PROVEEDOR_NO_DISPONIBLE_0002",
                    1201, ideProveedor);
                throw new InvalidOperationException($"PROVEEDOR_NO_DISPONIBLE_0002: El proveedor con ID {ideProveedor} no está disponible o está inactivo.");
            }
            return proveedor;
        }

        /// <summary>
        /// Valida el archivo contra las restricciones configuradas en el proveedor.
        /// </summary>
        private static void ValidarArchivoContraProveedor(Stream stream, string contentType, string configJson)
        {
            var config = JsonSerializer.Deserialize<StorageProviderConfig>(configJson) ?? new StorageProviderConfig();

            if (config.MaxFileSizeBytes > 0 && stream.Length > config.MaxFileSizeBytes)
            {
                throw new ArgumentException(
                    $"ARCHIVO_TAMANIO_EXCEDIDO_0001: El archivo ({stream.Length:N0} bytes) excede el límite permitido de {config.MaxFileSizeBytes:N0} bytes por el proveedor.");
            }

            if (config.AllowedContentTypes.Count > 0 &&
                !config.AllowedContentTypes.Any(t => t.Equals(contentType, StringComparison.OrdinalIgnoreCase)))
            {
                var tiposPermitidos = string.Join(", ", config.AllowedContentTypes);
                throw new ArgumentException(
                    $"ARCHIVO_TIPO_NO_PERMITIDO_0001: El Content-Type '{contentType}' no está en la lista de tipos permitidos por el proveedor. Tipos permitidos: {tiposPermitidos}.");
            }
        }

        /// <summary>
        /// Calcula el hash SHA256 del contenido del stream.
        /// </summary>
        private static string CalcularSHA256(Stream stream)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
