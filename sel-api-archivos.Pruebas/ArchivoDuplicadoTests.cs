using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using sel_api_archivos.Api.Filters;
using sel_api_archivos.Datos;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Archivo;
using sel_api_archivos.Negocio.Storage;
using Xunit;

namespace sel_api_archivos.Pruebas
{
    /// <summary>
    /// Pruebas del flujo de detección de archivos duplicados (mismo checksum SHA256 activo),
    /// que corresponde al índice único filtrado <c>API_FILE_ARCHIVO_IDX_03</c>.
    /// </summary>
    public class ArchivoServicioDuplicadoTests
    {
        private readonly IArchivoRepositorio _repositorioMock;
        private readonly IStorageProviderResolver _resolverMock;
        private readonly IStorageProvider _storageMock;
        private readonly ILogger<ArchivoServicio> _loggerMock;
        private readonly IHttpContextAccessor _httpContextAccessorMock;
        private readonly IMemoryCache _cacheMock;
        private readonly ArchivoServicio _servicio;
        private readonly ProveedorEntity _proveedorActivo;

        public ArchivoServicioDuplicadoTests()
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

            _proveedorActivo = new ProveedorEntity
            {
                IdeProveedor = 1,
                CodProveedor = "LOCAL",
                JsnConfiguracion = "{}",
                FlgActivo = true
            };

            _repositorioMock.ListarProveedoresActivosAsync()
                .Returns(Task.FromResult<IEnumerable<ProveedorEntity>>(new List<ProveedorEntity> { _proveedorActivo }));
            _resolverMock.Resolve("LOCAL").Returns(_storageMock);
        }

        [Fact]
        public async Task SubirArchivoAsync_ChecksumYaRegistradoActivo_LanzaArchivoDuplicadoException()
        {
            // Arrange: el SP dispara la violación real del índice único de checksum.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido duplicado"));
            var sqlDuplicado = SqlExceptionTestFactory.Create(
                2601,
                "Cannot insert duplicate key row in object 'ARC.API_FILE_TMM_ARCHIVO' with unique index " +
                "'API_FILE_ARCHIVO_IDX_03'. The duplicate key value is (496164e187e0d82736ef4643eeeaa48c741a913d128a400f601b3ba7aa565f77).");

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromException<Guid>(sqlDuplicado));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream, "duplicado.txt", "text/plain", "KOFIX", null, "usuarioTest", "127.0.0.1");

            // Act & Assert
            await act.Should().ThrowAsync<ArchivoDuplicadoException>()
                .WithMessage("*ARCHIVO_DUPLICADO_0001*");
        }

        [Fact]
        public async Task SubirArchivoAsync_ChecksumDuplicado_EliminaArchivoFisicoHuerfano()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido duplicado"));
            var sqlDuplicado = SqlExceptionTestFactory.Create(
                2601,
                "Cannot insert duplicate key row in object 'ARC.API_FILE_TMM_ARCHIVO' with unique index 'API_FILE_ARCHIVO_IDX_03'.");

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromException<Guid>(sqlDuplicado));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream, "duplicado.txt", "text/plain", "KOFIX", null, "usuarioTest", "127.0.0.1");

            // Act
            await act.Should().ThrowAsync<ArchivoDuplicadoException>();

            // Assert: el archivo ya subido al storage se limpia para no dejar huérfanos.
            await _storageMock.Received(1).DeleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), _proveedorActivo.JsnConfiguracion);
        }

        [Fact]
        public async Task SubirArchivoAsync_ChecksumDuplicado_NoRegistraAuditoria()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido duplicado"));
            var sqlDuplicado = SqlExceptionTestFactory.Create(
                2601,
                "Cannot insert duplicate key row in object 'ARC.API_FILE_TMM_ARCHIVO' with unique index 'API_FILE_ARCHIVO_IDX_03'.");

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromException<Guid>(sqlDuplicado));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream, "duplicado.txt", "text/plain", "KOFIX", null, "usuarioTest", "127.0.0.1");

            // Act
            await act.Should().ThrowAsync<ArchivoDuplicadoException>();

            // Assert: no debe registrarse auditoría de un archivo que nunca quedó persistido.
            await _repositorioMock.DidNotReceive().RegistrarAuditoriaAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task SubirArchivoAsync_ViolacionUniqueDeOtroIndice_PropagaSqlExceptionOriginalSinLimpiarArchivo()
        {
            // Arrange: violación de índice único distinto (2627), no debe confundirse con un duplicado de checksum.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            var sqlOtraViolacion = SqlExceptionTestFactory.Create(
                2627,
                "Violation of UNIQUE KEY constraint 'ALGUN_OTRO_INDICE'. Cannot insert duplicate key in object 'ARC.OTRA_TABLA'.");

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromException<Guid>(sqlOtraViolacion));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream, "otro.txt", "text/plain", "KOFIX", null, "usuarioTest", "127.0.0.1");

            // Act & Assert: se propaga el SqlException original, no se traduce a ArchivoDuplicadoException.
            var excepcion = await act.Should().ThrowAsync<SqlException>();
            excepcion.Which.Number.Should().Be(2627);

            await _storageMock.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task SubirArchivoAsync_SqlExceptionNoRelacionadaAIndiceUnico_PropagaOriginal()
        {
            // Arrange: cualquier otro error SQL (ej. violación de FK) debe seguir siendo un error interno genérico.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
            var sqlOtroError = SqlExceptionTestFactory.Create(
                547,
                "The INSERT statement conflicted with the FOREIGN KEY constraint.");

            _repositorioMock.RegistrarArchivoAsync(Arg.Any<ArchivoEntity>())
                .Returns(Task.FromException<Guid>(sqlOtroError));

            Func<Task> act = async () => await _servicio.SubirArchivoAsync(
                stream, "otro.txt", "text/plain", "KOFIX", null, "usuarioTest", "127.0.0.1");

            var excepcion = await act.Should().ThrowAsync<SqlException>();
            excepcion.Which.Number.Should().Be(547);

            await _storageMock.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }
    }

    /// <summary>
    /// Pruebas del filtro global que traduce excepciones de negocio a respuestas HTTP
    /// estructuradas (<see cref="RespuestaEstandar"/>), en particular el caso de archivo duplicado.
    /// </summary>
    public class MidagriResponseFilterTests
    {
        private readonly MidagriResponseFilter _filter;

        public MidagriResponseFilterTests()
        {
            _filter = new MidagriResponseFilter(Substitute.For<ILogger<MidagriResponseFilter>>());
        }

        private static ExceptionContext CrearContexto(Exception exception)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        [Fact]
        public void OnException_ArchivoDuplicadoException_Retorna409ConCodigoDuplicado()
        {
            // Arrange
            var checksum = "496164e187e0d82736ef4643eeeaa48c741a913d128a400f601b3ba7aa565f77";
            var context = CrearContexto(new ArchivoDuplicadoException(checksum));

            // Act
            _filter.OnException(context);

            // Assert
            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);

            var body = result.Value.Should().BeOfType<RespuestaEstandar>().Subject;
            body.Respuesta.Should().Be("ERROR");
            body.Mensaje.Should().Contain("ARCHIVO_DUPLICADO_0001");
            body.Mensaje.Should().Contain(checksum);
            body.Datos.Should().BeNull();
        }

        [Fact]
        public void OnException_ArchivoVacioException_SigueRetornando400()
        {
            // Regresión: confirma que agregar el caso de duplicado no rompe el mapeo existente.
            var context = CrearContexto(new ArchivoVacioException());

            _filter.OnException(context);

            var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public void OnException_ExcepcionNoMapeada_Retorna500ConErrorInterno()
        {
            var context = CrearContexto(new InvalidOperationException("fallo inesperado"));

            _filter.OnException(context);

            var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
            result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

            var body = result.Value.Should().BeOfType<RespuestaEstandar>().Subject;
            body.Mensaje.Should().Contain("ERROR_INTERNO_0001");
        }
    }
}
