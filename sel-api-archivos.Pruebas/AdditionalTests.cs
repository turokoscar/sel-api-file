using System;
using System.Collections.Generic;
using System.IO;
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
    public class StorageProviderResolverTests
    {
        [Fact]
        public void Resolve_ProveedorExistente_RetornaProveedor()
        {
            var localProvider = Substitute.For<IStorageProvider>();
            localProvider.ProviderCode.Returns("LOCAL");
            var ftpProvider = Substitute.For<IStorageProvider>();
            ftpProvider.ProviderCode.Returns("FTP");

            var resolver = new StorageProviderResolver(new[] { localProvider, ftpProvider });

            var result = resolver.Resolve("LOCAL");

            result.Should().BeSameAs(localProvider);
        }

        [Fact]
        public void Resolve_ProveedorMayusculas_EncuentraProveedor()
        {
            var localProvider = Substitute.For<IStorageProvider>();
            localProvider.ProviderCode.Returns("LOCAL");

            var resolver = new StorageProviderResolver(new[] { localProvider });

            var result = resolver.Resolve("local");

            result.Should().BeSameAs(localProvider);
        }

        [Fact]
        public void Resolve_ProveedorNoExiste_LanzaKeyNotFoundException()
        {
            var localProvider = Substitute.For<IStorageProvider>();
            localProvider.ProviderCode.Returns("LOCAL");

            var resolver = new StorageProviderResolver(new[] { localProvider });

            Action act = () => resolver.Resolve("S3");

            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*PROVEEDOR_NO_DISPONIBLE_0003*");
        }

        [Fact]
        public void Resolve_SinProveedores_LanzaKeyNotFoundException()
        {
            var resolver = new StorageProviderResolver(Array.Empty<IStorageProvider>());

            Action act = () => resolver.Resolve("LOCAL");

            act.Should().Throw<KeyNotFoundException>();
        }
    }

    public class ArchivoServicioHappyPathTests
    {
        private readonly IArchivoRepositorio _repositorioMock;
        private readonly IStorageProviderResolver _resolverMock;
        private readonly IStorageProvider _storageMock;
        private readonly ILogger<ArchivoServicio> _loggerMock;
        private readonly IHttpContextAccessor _httpContextAccessorMock;
        private readonly IMemoryCache _cacheMock;
        private readonly ArchivoServicio _servicio;

        public ArchivoServicioHappyPathTests()
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

            _servicio = new ArchivoServicio(
                _repositorioMock, _resolverMock, _loggerMock, _httpContextAccessorMock, _cacheMock);
        }

        [Fact]
        public async Task ObtenerMetadataAsync_ArchivoExiste_RetornaMetadata()
        {
            var idArchivo = Guid.NewGuid();
            var metadataEsperada = new ArchivoEntity
            {
                IdeArchivo = idArchivo,
                CodSistema = "KOFIX",
                TxtNombreOriginal = "documento.pdf",
                TxtContentType = "application/pdf",
                CanTamanioBytes = 1024
            };

            _repositorioMock.ObtenerArchivoPorIdAsync(idArchivo)
                .Returns(Task.FromResult<ArchivoEntity?>(metadataEsperada));

            var resultado = await _servicio.ObtenerMetadataAsync(idArchivo);

            resultado.Should().BeSameAs(metadataEsperada);
            resultado.TxtNombreOriginal.Should().Be("documento.pdf");
        }

        [Fact]
        public async Task LeerContenidoTextoAsync_ArchivoExiste_RetornaContenidoYAudita()
        {
            var idArchivo = Guid.NewGuid();
            var contenidoEsperado = "Contenido de texto del archivo";
            var metadata = new ArchivoEntity
            {
                IdeArchivo = idArchivo,
                IdeProveedor = 1,
                TxtNombreFisico = "archivo.txt",
                TxtRutaRelativa = "KOFIX/GENERAL"
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
            _resolverMock.Resolve("LOCAL").Returns(_storageMock);
            _storageMock.ReadTextContentAsync(
                    metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion)
                .Returns(Task.FromResult(contenidoEsperado));

            var resultado = await _servicio.LeerContenidoTextoAsync(idArchivo, "testuser", "127.0.0.1");

            resultado.Should().Be(contenidoEsperado);
            await _repositorioMock.Received(1).RegistrarAuditoriaAsync(idArchivo, "READ", "testuser", "127.0.0.1");
        }

        [Fact]
        public async Task EliminarArchivoAsync_ArchivoExiste_EliminaFisicoYBdYAudita()
        {
            var idArchivo = Guid.NewGuid();
            var metadata = new ArchivoEntity
            {
                IdeArchivo = idArchivo,
                IdeProveedor = 1,
                TxtNombreFisico = "archivo.pdf",
                TxtRutaRelativa = "KOFIX/GENERAL"
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
            _resolverMock.Resolve("LOCAL").Returns(_storageMock);
            _repositorioMock.EliminarArchivoAsync(idArchivo)
                .Returns(Task.FromResult(true));

            var resultado = await _servicio.EliminarArchivoAsync(idArchivo, "testuser", "127.0.0.1");

            resultado.Should().BeTrue();
            await _storageMock.Received(1).DeleteAsync(
                metadata.TxtNombreFisico, metadata.TxtRutaRelativa, proveedor.JsnConfiguracion);
            await _repositorioMock.Received(1).EliminarArchivoAsync(idArchivo);
            await _repositorioMock.Received(1).RegistrarAuditoriaAsync(idArchivo, "DELETE", "testuser", "127.0.0.1");
        }
    }
}
