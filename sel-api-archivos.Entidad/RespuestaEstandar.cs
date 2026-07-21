using System.Text.Json.Serialization;

namespace sel_api_archivos.Entidad
{
    /// <summary>
    /// Envoltorio estándar de respuesta JSON para todos los endpoints de la API,
    /// conforme al estándar MIDAGRI definido en el ADR MCVS-604.
    /// </summary>
    public class RespuestaEstandar
    {
        /// <summary>
        /// Estado de la respuesta: "OK" para éxito, "ERROR" para fallos.
        /// </summary>
        [JsonPropertyName("respuesta")]
        public string Respuesta { get; set; } = "OK";

        /// <summary>
        /// Mensaje descriptivo. En caso de error incluye el código de error
        /// (ej: "ARCHIVO_TAMANIO_EXCEDIDO_0001: El archivo excede el límite permitido...").
        /// </summary>
        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Datos de respuesta. Contiene la estructura específica del endpoint en caso de éxito.
        /// Es <c>null</c> en caso de error.
        /// </summary>
        [JsonPropertyName("datos")]
        public object? Datos { get; set; }

        /// <summary>
        /// Crea una respuesta de éxito con datos opcionales.
        /// </summary>
        /// <param name="datos">Datos a incluir en la respuesta. Puede ser <c>null</c>.</param>
        /// <param name="mensaje">Mensaje opcional.</param>
        /// <returns>Nueva instancia de <see cref="RespuestaEstandar"/> con estado "OK".</returns>
        public static RespuestaEstandar Exito(object? datos, string mensaje = "")
        {
            return new RespuestaEstandar
            {
                Respuesta = "OK",
                Mensaje = mensaje,
                Datos = datos
            };
        }

        /// <summary>
        /// Crea una respuesta de error.
        /// </summary>
        /// <param name="mensaje">Mensaje de error, preferentemente con código de error prefijado
        /// (ej: "ARCHIVO_NO_ENCONTRADO_0001: No se encontró el archivo...").</param>
        /// <returns>Nueva instancia de <see cref="RespuestaEstandar"/> con estado "ERROR".</returns>
        public static RespuestaEstandar Error(string mensaje)
        {
            return new RespuestaEstandar
            {
                Respuesta = "ERROR",
                Mensaje = mensaje,
                Datos = null
            };
        }
    }
}
