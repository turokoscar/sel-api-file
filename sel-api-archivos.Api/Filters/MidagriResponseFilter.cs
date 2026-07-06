using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Api.Filters
{
    public class MidagriResponseFilter : IActionFilter, IExceptionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // No action needed before executing
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Si hay un error, el filtro de excepción lo manejará
            if (context.Exception != null) return;

            // Si el resultado es un archivo para descarga directa, no envolver en JSON
            if (context.Result is FileResult) return;

            if (context.Result is ObjectResult objectResult)
            {
                // Si el objeto ya es una RespuestaEstandar, no envolverlo de nuevo
                if (objectResult.Value is RespuestaEstandar) return;

                var wrappedResponse = RespuestaEstandar.Exito(objectResult.Value);
                objectResult.Value = wrappedResponse;
            }
            else if (context.Result is EmptyResult)
            {
                context.Result = new ObjectResult(RespuestaEstandar.Exito(null));
            }
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var mensajeError = $"ERROR_INTERNO_0001: {exception.Message}";

            var errorResponse = RespuestaEstandar.Error(mensajeError);
            context.Result = new ObjectResult(errorResponse)
            {
                StatusCode = 500
            };

            context.ExceptionHandled = true;
        }
    }
}
