using System.ComponentModel.DataAnnotations;

namespace sel_api_archivos.Api.Options
{
    /// <summary>
    /// Opciones de configuración para la autenticación JWT.
    /// Se vincula a la sección <c>JwtSettings</c> en <c>appsettings.json</c>.
    /// </summary>
    public sealed class JwtSettingsOptions
    {
        /// <summary>
        /// Clave secreta utilizada para firmar los tokens JWT. Mínimo 32 caracteres.
        /// </summary>
        [Required(ErrorMessage = "JwtSettings:Secret es requerido.")]
        [MinLength(32, ErrorMessage = "JwtSettings:Secret debe tener al menos 32 caracteres.")]
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Emisor (Issuer) esperado en los tokens JWT.
        /// </summary>
        [Required(ErrorMessage = "JwtSettings:Issuer es requerido.")]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Audiencia (Audience) esperada en los tokens JWT.
        /// </summary>
        [Required(ErrorMessage = "JwtSettings:Audience es requerido.")]
        public string Audience { get; set; } = string.Empty;
    }
}
