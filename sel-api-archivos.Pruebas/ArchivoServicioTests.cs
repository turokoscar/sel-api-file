using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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
        private readonly ArchivoServicio _servicio;

        public ArchivoServicioTests()
        {
            _repositorioMock = Substitute.For<IArchivoRepositorio>();
            _resolverMock = Substitute.For<IStorageProviderResolver>();
            _storageMock = Substitute.For<IStorageProvider>();

            _servicio = new ArchivoServicio(_repositorioMock, _resolverMock);
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
                .Returns(new List<ProveedorEntity> { proveedorActivo });

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
                .Returns(new List<ProveedorEntity> { proveedor });

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
