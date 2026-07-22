using System;

namespace sel_api_archivos.Negocio.Archivo
{
    /// <summary>
    /// Excepción base para errores de negocio en la gestión de archivos.
    /// </summary>
    public abstract class ArchivoException : Exception
    {
        /// <summary>Código de error interno.</summary>
        public string Codigo { get; }
        /// <summary>Código de estado HTTP asociado.</summary>
        public int StatusCode { get; }

        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        /// <param name="codigo">Código de error interno.</param>
        /// <param name="statusCode">Código HTTP.</param>
        /// <param name="mensaje">Mensaje descriptivo.</param>
        protected ArchivoException(string codigo, int statusCode, string mensaje) : base(mensaje)
        {
            Codigo = codigo;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// El archivo está vacío (cero bytes).
    /// </summary>
    public sealed class ArchivoVacioException : ArchivoException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        public ArchivoVacioException() : base("ARCHIVO_VACIO_0001", 400, "ARCHIVO_VACIO_0001: El archivo a subir está vacío.") { }
    }

    /// <summary>
    /// El archivo excede el tamaño máximo permitido por el proveedor.
    /// </summary>
    public sealed class ArchivoTamanioExcedidoException : ArchivoException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        /// <param name="tamanio">Tamaño real del archivo en bytes.</param>
        /// <param name="limite">Límite permitido en bytes.</param>
        public ArchivoTamanioExcedidoException(long tamanio, long limite)
            : base("ARCHIVO_TAMANIO_EXCEDIDO_0001", 400,
                   $"ARCHIVO_TAMANIO_EXCEDIDO_0001: El archivo ({tamanio:N0} bytes) excede el límite permitido de {limite:N0} bytes por el proveedor.") { }
    }

    /// <summary>
    /// El Content-Type del archivo no está en la lista de tipos permitidos por el proveedor.
    /// </summary>
    public sealed class ArchivoTipoNoPermitidoException : ArchivoException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        /// <param name="contentType">Content-Type rechazado.</param>
        /// <param name="tiposPermitidos">Lista de tipos permitidos separados por coma.</param>
        public ArchivoTipoNoPermitidoException(string contentType, string tiposPermitidos)
            : base("ARCHIVO_TIPO_NO_PERMITIDO_0001", 400,
                   $"ARCHIVO_TIPO_NO_PERMITIDO_0001: El Content-Type '{contentType}' no está en la lista de tipos permitidos por el proveedor. Tipos permitidos: {tiposPermitidos}.") { }
    }
}
