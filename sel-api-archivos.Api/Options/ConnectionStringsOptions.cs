using System.ComponentModel.DataAnnotations;

namespace sel_api_archivos.Api.Options
{
    /// <summary>
    /// Opciones de configuración para las cadenas de conexión.
    /// Se vincula a la sección <c>ConnectionStrings</c> en <c>appsettings.json</c>.
    /// </summary>
    public sealed class ConnectionStringsOptions
    {
        /// <summary>
        /// Cadena de conexión principal a la base de datos SQL Server.
        /// </summary>
        [Required(ErrorMessage = "ConnectionStrings:DefaultConnection es requerida.")]
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
