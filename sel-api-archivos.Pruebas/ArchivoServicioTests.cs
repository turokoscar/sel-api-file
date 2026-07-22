using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using sel_api_archivos.Datos;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Archivo;
using sel_api_archivos.Negocio.Storage;
using Xunit;

namespace sel_api_archivos.Pruebas
{
    public class ArchivoServicioTests
    {
        private readonly IArchivoRepositorio _repositorioMock;
        private readonly IStorageProviderResolver _resolverMock;
        private readonly IStorageProvider _storageMock;
        private readonly ILogger<ArchivoServicio> _loggerMock;
        private readonly IHttpContextAccessor _httpContextAccessorMock;
        private readonly IMemoryCache _cacheMock;
        private readonly ArchivoServicio _servicio;

        public ArchivoServicioTests()
        {
            _repositorioMock = Substitute.For<IArchivoRepositorio>();
            _resolverMock = Substitute.For<IStorageProviderResolver>();
            _storageMock = Substitute.For<IStorageProvider>();
            _loggerMock = Substitute.For<ILogger<ArchivoServicio>>();
            _httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
            _cacheMock = Substitute.For<IMemoryCache>();

            var httpContext = new DefaultHttpContext();
            httpContext.Items["CorrelationId"] = Guid.NewGuid().ToString();
            _httpContextAccessorMock.HttpContext.Returns(httpContext);

            _servicio = new ArchivoServicio(_repositorioMock, _resolverMock, _loggerMock, _httpContextAccessorMock, _cacheMock);
        }

        [Fact]
        public async Task SubirArchivoAsync_StreamNull_LanzaArgumentNullException()
        {
            // Arrange
            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: null!,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("stream");
        }

        [Fact]
        public async Task SubirArchivoAsync_NombreOriginalNull_LanzaArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: null!,
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*nombreOriginal*");
        }

        [Fact]
        public async Task SubirArchivoAsync_StreamVacio_LanzaArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArchivoVacioException>()
                .WithMessage("*ARCHIVO_VACIO_0001*");
        }

        [Fact]
        public async Task SubirArchivoAsync_ProveedorNoDisponible_LanzaInvalidOperationException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity>()));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ProveedorNoDisponibleException>()
                .WithMessage("*PROVEEDOR_NO_DISPONIBLE_0001*");
        }

        [Fact]
        public async Task SubirArchivoAsync_TamanioExcedeLimite_LanzaArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido grande"));
            var proveedorActivo = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "LOCAL",
                JsnConfiguracion = "{\"maxFileSizeBytes\": 5}",
                FlgActivo = true
            };

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity> { proveedorActivo }));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArchivoTamanioExcedidoException>()
                .WithMessage("*ARCHIVO_TAMANIO_EXCEDIDO_0001*");
        }

        [Fact]
        public async Task SubirArchivoAsync_TipoNoPermitido_LanzaArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            var proveedorActivo = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "LOCAL",
                JsnConfiguracion = "{\"allowedContentTypes\": [\"application/pdf\"]}",
                FlgActivo = true
            };

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity> { proveedorActivo }));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArchivoTipoNoPermitidoException>()
                .WithMessage("*ARCHIVO_TIPO_NO_PERMITIDO_0001*");
        }

        [Fact]
        public async Task ObtenerMetadataAsync_ArchivoNoExiste_LanzaFileNotFoundException()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _repositorioMock.ObtenerArchivoPorIdAsync(idInexistente)
                .Returns(Task.FromResult<ArchivoEntity?>(null));

            Func<Task> act = () => _servicio.ObtenerMetadataAsync(idInexistente);

            // Act & Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*ARCHIVO_NO_ENCONTRADO_0001*");
        }

        [Fact]
        public async Task DescargarArchivoAsync_ArchivoNoExiste_LanzaFileNotFoundException()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _repositorioMock.ObtenerArchivoPorIdAsync(idInexistente)
                .Returns(Task.FromResult<ArchivoEntity?>(null));

            Func<Task> act = () => _servicio.DescargarArchivoAsync(idInexistente, "test", "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*ARCHIVO_NO_ENCONTRADO_0001*");
        }

        [Fact]
        public async Task DescargarArchivoAsync_ProveedorInactivo_LanzaInvalidOperationException()
        {
            // Arrange
            var idArchivo = Guid.NewGuid();
            var metadata = new ArchivoEntity
            {
                IdeArchivo = idArchivo,
                IdeProveedor = 99,
                TxtNombreFisico = "test.pdf",
                TxtRutaRelativa = "KOFIX/GENERAL"
            };

            _repositorioMock.ObtenerArchivoPorIdAsync(idArchivo)
                .Returns(Task.FromResult<ArchivoEntity?>(metadata));

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity>()));

            Func<Task> act = () => _servicio.DescargarArchivoAsync(idArchivo, "test", "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ProveedorInactivoException>()
                .WithMessage("*PROVEEDOR_NO_DISPONIBLE_0002*");
        }

        [Fact]
        public async Task EliminarArchivoAsync_ArchivoNoExiste_LanzaFileNotFoundException()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _repositorioMock.ObtenerArchivoPorIdAsync(idInexistente)
                .Returns(Task.FromResult<ArchivoEntity?>(null));

            Func<Task> act = () => _servicio.EliminarArchivoAsync(idInexistente, "test", "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*ARCHIVO_NO_ENCONTRADO_0001*");
        }

        [Fact]
        public async Task LeerContenidoTextoAsync_ArchivoNoExiste_LanzaFileNotFoundException()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _repositorioMock.ObtenerArchivoPorIdAsync(idInexistente)
                .Returns(Task.FromResult<ArchivoEntity?>(null));

            Func<Task> act = () => _servicio.LeerContenidoTextoAsync(idInexistente, "test", "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*ARCHIVO_NO_ENCONTRADO_0001*");
        }

        [Fact]
        public async Task SubirArchivoAsync_ProviderCodeDesconocido_LanzaKeyNotFoundException()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            var proveedorActivo = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "DESCONOCIDO",
                JsnConfiguracion = "{}",
                FlgActivo = true
            };

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity> { proveedorActivo }));

            _resolverMock.Resolve("DESCONOCIDO")
                .Returns(_ => throw new KeyNotFoundException("PROVEEDOR_NO_DISPONIBLE_0003: Proveedor no encontrado"));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream: stream,
                nombreOriginal: "test.txt",
                contentType: "text/plain",
                codSistema: "KOFIX",
                codProceso: null,
                usuario: "test",
                ipOrigen: "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*PROVEEDOR_NO_DISPONIBLE_0003*");
        }

        [Fact]
        public async Task SubirArchivoAsync_DeberiaSubirYRegistrarExitosamente()
        {
            // Arrange
            var fileContent = "Archivo de prueba de MIDAGRI";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var nombreOriginal = "midagri_test.txt";
            var contentType = "text/plain";
            var codSistema = "KOFIX";
            var codProceso = "FACTURACION";
            var usuario = "usuarioTest";
            var ipOrigen = "192.168.1.50";

            var proveedorActivo = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "LOCAL",
                TxtNombre = "Local Storage",
                JsnConfiguracion = "{}",
                FlgActivo = true
            };

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity> { proveedorActivo }));

            _resolverMock.Resolve("LOCAL")
                .Returns(_storageMock);

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromResult(Guid.NewGuid()));

            // Act
            var resultGuid = await _servicio.SubirArchivoAsync(
                stream, nombreOriginal, contentType, codSistema, codProceso, usuario, ipOrigen
            );

            // Assert
            resultGuid.Should().NotBeEmpty();

            await _storageMock.Received(1).UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                proveedorActivo.JsnConfiguracion
            );

            await _repositorioMock.Received(1).RegistrarArchivoAsync(Arg.Any<ArchivoEntity>());
            await _repositorioMock.Received(1).RegistrarAuditoriaAsync(resultGuid, "UPLOAD", usuario, ipOrigen);
        }

        [Fact]
        public async Task DescargarArchivoAsync_DeberiaDescargarYAuditar()
        {
            // Arrange
            var idArchivo = Guid.NewGuid();
            var usuario = "usuarioTest";
            var ipOrigen = "192.168.1.50";

            var metadata = new ArchivoEntity
            {
                IdeArchivo = idArchivo,
                TxtNombreFisico = "nombre_unico.pdf",
                TxtRutaRelativa = "KOFIX/CONTRATOS",
                TxtNombreOriginal = "contrato.pdf",
                TxtContentType = "application/pdf",
                IdeProveedor = 1
            };

            var proveedor = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "LOCAL",
                JsnConfiguracion = "{}",
                FlgActivo = true
            };

            _repositorioMock.ObtenerArchivoPorIdAsync(idArchivo)
                .Returns(Task.FromResult<ArchivoEntity?>(metadata));

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(
                    new List<ProveedorEntity> { proveedor }));

            _resolverMock.Resolve("LOCAL")
                .Returns(_storageMock);

            var dummyStream = new MemoryStream();
            _storageMock.DownloadAsync(metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion)
                .Returns(Task.FromResult<Stream>(dummyStream));

            // Act
            var (streamResult, metadataResult) = await _servicio.DescargarArchivoAsync(idArchivo, usuario, ipOrigen);

            // Assert
            streamResult.Should().BeSameAs(dummyStream);
            metadataResult.TxtNombreOriginal.Should().Be("contrato.pdf");

            await _repositorioMock.Received(1).RegistrarAuditoriaAsync(idArchivo, "DOWNLOAD", usuario, ipOrigen);
        }
    }
}
