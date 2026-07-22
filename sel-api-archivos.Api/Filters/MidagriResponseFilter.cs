using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Archivo;
using sel_api_archivos.Negocio.Storage;

namespace sel_api_archivos.Api.Filters
{
    /// <summary>
    /// Filtro global que envuelve todas las respuestas JSON en <see cref="RespuestaEstandar"/>
    /// y transforma excepciones en respuestas de error estructuradas con código y mensaje.
    /// </summary>
    public sealed class MidagriResponseFilter : IActionFilter, IExceptionFilter
    {
        private readonly ILogger<MidagriResponseFilter> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del filtro.
        /// </summary>
        /// <param name="logger">Logger para eventos de filtro y excepciones.</param>
        public MidagriResponseFilter(ILogger<MidagriResponseFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null) return;

            if (context.Result is FileResult) return;

            if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.Value is RespuestaEstandar) return;
                var wrappedResponse = RespuestaEstandar.Exito(objectResult.Value);
                objectResult.Value = wrappedResponse;

                _logger.LogDebug(
                    LogEventIds.ResponseWrapperApplied,
                    "Respuesta envuelta en RespuestaEstandar. Tipo: {ResultType}",
                    objectResult.Value?.GetType().Name ?? "null"
                );
            }
            else if (context.Result is EmptyResult)
            {
                context.Result = new ObjectResult(RespuestaEstandar.Exito(null));
            }
        }

        /// <inheritdoc />
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var (statusCode, errorCode) = MapExceptionToStatusCode(exception);

            string mensajeError;
            if (exception is ArchivoException ae)
            {
                mensajeError = ae.Message;
            }
            else if (exception is ProveedorException pe)
            {
                mensajeError = pe.Message;
            }
            else if (exception is FileNotFoundException)
            {
                mensajeError = exception.Message;
            }
            else
            {
                mensajeError = $"ERROR_INTERNO_0001: {exception.Message}";
            }

            var errorResponse = RespuestaEstandar.Error(mensajeError);
            context.Result = new ObjectResult(errorResponse)
            {
                StatusCode = statusCode
            };

            context.ExceptionHandled = true;

            _logger.LogWarning(
                LogEventIds.ExceptionHandled,
                exception,
                "Excepcion manejada. Tipo: {ExceptionType}, Codigo: {ErrorCode}, Status: {StatusCode}, Mensaje: {Message}",
                exception.GetType().Name,
                errorCode,
                statusCode,
                exception.Message
            );
        }

        /// <summary>
        /// Determina el código de estado HTTP y el código de error interno
        /// según el tipo de excepción recibida.
        /// </summary>
        private static (int StatusCode, string ErrorCode) MapExceptionToStatusCode(Exception exception)
        {
            return exception switch
            {
                ArchivoVacioException => (StatusCodes.Status400BadRequest, "ARCHIVO_VACIO"),
                ArchivoTamanioExcedidoException => (StatusCodes.Status400BadRequest, "ARCHIVO_TAMANIO_EXCEDIDO"),
                ArchivoTipoNoPermitidoException => (StatusCodes.Status400BadRequest, "ARCHIVO_TIPO_NO_PERMITIDO"),
                ProveedorNoDisponibleException => (StatusCodes.Status503ServiceUnavailable, "PROVEEDOR_NO_DISPONIBLE"),
                ProveedorInactivoException => (StatusCodes.Status503ServiceUnavailable, "PROVEEDOR_NO_DISPONIBLE"),
                ProveedorCodigoDesconocidoException => (StatusCodes.Status503ServiceUnavailable, "PROVEEDOR_NO_DISPONIBLE"),
                FileNotFoundException { Message: var msg } when msg.StartsWith("ARCHIVO_NO_ENCONTRADO")
                    => (StatusCodes.Status404NotFound, "ARCHIVO_NO_ENCONTRADO"),
                FileNotFoundException
                    => (StatusCodes.Status404NotFound, "NOT_FOUND"),
                KeyNotFoundException
                    => (StatusCodes.Status503ServiceUnavailable, "PROVEEDOR_NO_DISPONIBLE"),
                _ => (StatusCodes.Status500InternalServerError, "ERROR_INTERNO")
            };
        }
    }
}
