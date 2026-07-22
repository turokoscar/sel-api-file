using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using sel_api_archivos.Entidad;

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
            if (exception is ArgumentException argEx && argEx.Message.Contains('_'))
            {
                mensajeError = exception.Message;
            }
            else if (exception is FileNotFoundException fnfEx && fnfEx.Message.Contains('_'))
            {
                mensajeError = exception.Message;
            }
            else if (exception is InvalidOperationException ioEx && ioEx.Message.Contains('_'))
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
                ArgumentException { Message: var msg } when msg.StartsWith("ARCHIVO_") || msg.StartsWith("PARAM_")
                    => (StatusCodes.Status400BadRequest, "BAD_REQUEST"),

                ArgumentException
                    => (StatusCodes.Status400BadRequest, "BAD_REQUEST"),

                FileNotFoundException { Message: var msg } when msg.StartsWith("ARCHIVO_NO_ENCONTRADO")
                    => (StatusCodes.Status404NotFound, "NOT_FOUND"),

                FileNotFoundException
                    => (StatusCodes.Status404NotFound, "NOT_FOUND"),

                InvalidOperationException { Message: var msg } when msg.StartsWith("PROVEEDOR_NO_DISPONIBLE")
                    => (StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE"),

                InvalidOperationException
                    => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR"),

                _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR")
            };
        }
    }
}
