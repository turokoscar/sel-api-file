using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sel_api_archivos.Entidad;
using sel_api_archivos.Negocio.Archivo;

namespace sel_api_archivos.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("archivos")]
    public class ArchivosController : ControllerBase
    {
        private readonly IArchivoServicio _archivoServicio;

        public ArchivosController(IArchivoServicio archivoServicio)
        {
            _archivoServicio = archivoServicio ?? throw new ArgumentNullException(nameof(archivoServicio));
        }

        private string GetUsuarioActual()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value 
                ?? "Anonimo";
        }

        private string GetIpOrigen()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        [HttpPost]
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

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObtenerMetadata(Guid id)
        {
            var metadata = await _archivoServicio.ObtenerMetadataAsync(id);
            return Ok(metadata);
        }

        [HttpGet("{id:guid}/descarga")]
        public async Task<IActionResult> Descargar(Guid id)
        {
            var (stream, metadata) = await _archivoServicio.DescargarArchivoAsync(
                id, 
                GetUsuarioActual(), 
                GetIpOrigen()
            );

            // El filtro MidagriResponseFilter ignora FileResult, por lo que se envía el binario puro.
            return File(stream, metadata.TxtContentType, metadata.TxtNombreOriginal);
        }

        [HttpGet("{id:guid}/contenido")]
        public async Task<IActionResult> LeerContenido(Guid id)
        {
            var contenido = await _archivoServicio.LeerContenidoTextoAsync(
                id, 
                GetUsuarioActual(), 
                GetIpOrigen()
            );

            return Ok(new { contenido });
        }

        [HttpDelete("{id:guid}")]
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
