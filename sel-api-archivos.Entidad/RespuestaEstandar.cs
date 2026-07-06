using System.Text.Json.Serialization;

namespace sel_api_archivos.Entidad
{
    public class RespuestaEstandar
    {
        [JsonPropertyName("respuesta")]
        public string Respuesta { get; set; } = "OK"; // "OK" o "ERROR"

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("datos")]
        public object? Datos { get; set; }

        public static RespuestaEstandar Exito(object? datos, string mensaje = "")
        {
            return new RespuestaEstandar
            {
                Respuesta = "OK",
                Mensaje = mensaje,
                Datos = datos
            };
        }

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
