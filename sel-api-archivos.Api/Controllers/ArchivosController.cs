using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Archivo;

namespace sel_api_archivos.Api.Controllers
{
    /// <summary>
    /// Controlador REST para la gestión de archivos: subida, descarga, lectura de contenido,
    /// consulta de metadata y eliminación.
    /// </summary>
    /// <remarks>
    /// Todos los endpoints requieren autenticación JWT Bearer.
    /// Las respuestas están envueltas en <see cref="RespuestaEstandar"/> salvo las descargas de archivo.
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("archivos")]
    public sealed class ArchivosController : ControllerBase, IActionFilter
    {
        private readonly IArchivoServicio _archivoServicio;
        private readonly ILogger<ArchivosController> _logger;
        private const string StopwatchKey = "RequestStopwatch";
        private const string CorrelationIdKey = "CorrelationId";

        /// <summary>
        /// Inicializa una nueva instancia del controlador.
        /// </summary>
        /// <param name="archivoServicio">Servicio de coordinación de archivos.</param>
        /// <param name="logger">Logger para eventos de control.</param>
        /// <exception cref="ArgumentNullException">Cuando algún parámetro es <c>null</c>.</exception>
        public ArchivosController(IArchivoServicio archivoServicio, ILogger<ArchivosController> logger)
        {
            _archivoServicio = archivoServicio ?? throw new ArgumentNullException(nameof(archivoServicio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;

            context.HttpContext.Items[StopwatchKey] = stopwatch;
            context.HttpContext.Items[CorrelationIdKey] = correlationId;

            _logger.LogInformation(
                LogEventIds.RequestStart,
                "Solicitud iniciada. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
                method,
                path,
                correlationId
            );
        }

        /// <inheritdoc />
        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            var stopwatch = (Stopwatch?)context.HttpContext.Items[StopwatchKey];
            var correlationId = (string?)context.HttpContext.Items[CorrelationIdKey] ?? string.Empty;
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;

            stopwatch?.Stop();
            var durationMs = stopwatch?.ElapsedMilliseconds ?? 0;

            if (context.Exception != null && !context.ExceptionHandled)
            {
                _logger.LogWarning(
                    LogEventIds.RequestEnd,
                    "Solicitud finalizada con error. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, DuracionMs: {DurationMs}, Excepcion: {Exception}",
                    method,
                    path,
                    correlationId,
                    durationMs,
                    context.Exception.Message
                );
            }
            else
            {
                var statusCode = context.Result is ObjectResult objectResult
                    ? objectResult.StatusCode
                    : StatusCodes.Status200OK;

                _logger.LogInformation(
                    LogEventIds.RequestEnd,
                    "Solicitud finalizada. Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, DuracionMs: {DurationMs}, StatusCode: {StatusCode}",
                    method,
                    path,
                    correlationId,
                    durationMs,
                    statusCode ?? StatusCodes.Status200OK
                );
            }
        }

        /// <summary>
        /// Obtiene el nombre del usuario autenticado desde el token JWT.
        /// Intenta los claims Name, NameIdentifier y "sub" en ese orden.
        /// </summary>
        /// <returns>Nombre del usuario o "Anonimo" si no se encuentra.</returns>
        private string GetUsuarioActual()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? "Anonimo";
        }

        /// <summary>
        /// Obtiene la dirección IP de origen de la solicitud.
        /// </summary>
        /// <returns>Dirección IP o "0.0.0.0" si no está disponible.</returns>
        private string GetIpOrigen()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        /// <summary>
        /// Sube un archivo al almacenamiento.
        /// </summary>
        /// <param name="archivo">Archivo a subir (multipart/form-data).</param>
        /// <param name="codSistema">Código del sistema consumidor (requerido).</param>
        /// <param name="codProceso">Código opcional del proceso dentro del sistema.</param>
        /// <returns>Identificador del archivo creado.</returns>
        /// <response code="200">Archivo subido exitosamente.</response>
        /// <response code="400">Archivo no proporcionado o código de sistema faltante.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespuestaEstandar), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Subir(
            [FromForm] IFormFile archivo,
            [FromForm] string codSistema,
            [FromForm] string? codProceso)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(RespuestaEstandar.Error("No se proporcionó ningún archivo válido para la carga."));
            }

            if (string.IsNullOrWhiteSpace(codSistema))
            {
                return BadRequest(RespuestaEstandar.Error("El parámetro 'codSistema' es requerido para clasificar el archivo."));
            }

            using var stream = archivo.OpenReadStream();
            var ideArchivo = await _archivoServicio.SubirArchivoAsync(
                stream,
                archivo.FileName,
                archivo.ContentType,
                codSistema,
                codProceso,
                GetUsuarioActual(),
                GetIpOrigen()
            );

            return Ok(new { ideArchivo });
        }

        /// <summary>
        /// Obtiene la metadata de un archivo por su identificador.
        /// </summary>
        /// <param name="id">Identificador único del archivo (GUID).</param>
        /// <returns>Metadata del archivo.</returns>
        /// <response code="200">Metadata obtenida exitosamente.</response>
        /// <response code="404">Archivo no encontrado.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ArchivoEntity), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespuestaEstandar), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerMetadata(Guid id)
        {
            var metadata = await _archivoServicio.ObtenerMetadataAsync(id);
            return Ok(metadata);
        }

        /// <summary>
        /// Descarga el contenido binario de un archivo.
        /// </summary>
        /// <param name="id">Identificador único del archivo (GUID).</param>
        /// <returns>Archivo binario con el Content-Type original.</returns>
        /// <response code="200">Archivo descarga correctamente.</response>
        /// <response code="404">Archivo no encontrado.</response>
        [HttpGet("{id:guid}/descarga")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespuestaEstandar), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Descargar(Guid id)
        {
            var (stream, metadata) = await _archivoServicio.DescargarArchivoAsync(
                id,
                GetUsuarioActual(),
                GetIpOrigen()
            );

            return File(stream, metadata.TxtContentType, metadata.TxtNombreOriginal);
        }

        /// <summary>
        /// Lee el contenido de texto de un archivo.
        /// Solo debe usarse para archivos de texto (txt, json, xml, etc.).
        /// </summary>
        /// <param name="id">Identificador único del archivo (GUID).</param>
        /// <returns>Contenido del archivo como texto.</returns>
        /// <response code="200">Contenido leído exitosamente.</response>
        /// <response code="404">Archivo no encontrado.</response>
        [HttpGet("{id:guid}/contenido")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespuestaEstandar), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LeerContenido(Guid id)
        {
            var contenido = await _archivoServicio.LeerContenidoTextoAsync(
                id,
                GetUsuarioActual(),
                GetIpOrigen()
            );

            return Ok(new { contenido });
        }

        /// <summary>
        /// Elimina un archivo (soft delete).
        /// </summary>
        /// <param name="id">Identificador único del archivo (GUID).</param>
        /// <returns>Confirmación de eliminación.</returns>
        /// <response code="200">Archivo eliminado exitosamente.</response>
        /// <response code="404">Archivo no encontrado.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespuestaEstandar), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var eliminado = await _archivoServicio.EliminarArchivoAsync(
                id,
                GetUsuarioActual(),
                GetIpOrigen()
            );

            if (!eliminado)
            {
                return NotFound(RespuestaEstandar.Error("El archivo no pudo ser eliminado o ya no existe."));
            }

            return Ok(new { eliminado });
        }
    }
}
