using System;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Excepción base para errores de proveedores de almacenamiento.
    /// </summary>
    public abstract class ProveedorException : Exception
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
        protected ProveedorException(string codigo, int statusCode, string mensaje) : base(mensaje)
        {
            Codigo = codigo;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// No existe ningún proveedor de almacenamiento activo en la base de datos.
    /// </summary>
    public sealed class ProveedorNoDisponibleException : ProveedorException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        public ProveedorNoDisponibleException() : base(
            "PROVEEDOR_NO_DISPONIBLE_0001", 503,
            "PROVEEDOR_NO_DISPONIBLE_0001: No se encontró ningún proveedor de almacenamiento activo en la base de datos.") { }
    }

    /// <summary>
    /// El proveedor específico no existe o está inactivo.
    /// </summary>
    public sealed class ProveedorInactivoException : ProveedorException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        /// <param name="ideProveedor">Identificador del proveedor.</param>
        public ProveedorInactivoException(int ideProveedor) : base(
            "PROVEEDOR_NO_DISPONIBLE_0002", 503,
            $"PROVEEDOR_NO_DISPONIBLE_0002: El proveedor con ID {ideProveedor} no está disponible o está inactivo.") { }
    }

    /// <summary>
    /// No existe un proveedor registrado con el código especificado.
    /// </summary>
    public sealed class ProveedorCodigoDesconocidoException : ProveedorException
    {
        /// <summary>
        /// Inicializa una nueva instancia.
        /// </summary>
        /// <param name="providerCode">Código del proveedor no reconocido.</param>
        public ProveedorCodigoDesconocidoException(string providerCode) : base(
            "PROVEEDOR_NO_DISPONIBLE_0003", 503,
            $"PROVEEDOR_NO_DISPONIBLE_0003: No se encontró un proveedor de almacenamiento para el código '{providerCode}'.") { }
    }
}
