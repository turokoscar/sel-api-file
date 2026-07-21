using System.ComponentModel.DataAnnotations;

namespace sel_api_archivos.Api.Options
{
    /// <summary>
    /// Opciones de configuración para CORS.
    /// Se vincula a la sección <c>Cors</c> en <c>appsettings.json</c>.
    /// </summary>
    public sealed class CorsOptions
    {
        /// <summary>
        /// Lista de orígenes permitidos para requests CORS.
        /// </summary>
        [Required(ErrorMessage = "Cors:Origenes es requerido.")]
        public string[] Origenes { get; set; } = Array.Empty<string>();
    }
}
