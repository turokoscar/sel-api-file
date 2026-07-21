using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Datos
{
    /// <summary>
    /// Define las operaciones de acceso a datos para la gestión de archivos y sus proveedores.
    /// </summary>
    public interface IArchivoRepositorio
    {
        /// <summary>
        /// Registra un nuevo archivo en la base de datos.
        /// </summary>
        /// <param name="archivo">Entidad con los datos del archivo a registrar.</param>
        /// <returns>GUID del archivo generado.</returns>
        Task<Guid> RegistrarArchivoAsync(ArchivoEntity archivo);

        /// <summary>
        /// Obtiene la metadata de un archivo por su identificador.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <returns>La entidad del archivo o <c>null</c> si no existe.</returns>
        Task<ArchivoEntity?> ObtenerArchivoPorIdAsync(Guid ideArchivo);

        /// <summary>
        /// Realiza una eliminación lógica (soft delete) de un archivo.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo a eliminar.</param>
        /// <returns><c>true</c> si el archivo fue encontrado y marcado como eliminado; de lo contrario, <c>false</c>.</returns>
        Task<bool> EliminarArchivoAsync(Guid ideArchivo);

        /// <summary>
        /// Registra una acción de auditoría sobre un archivo.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <param name="txtAccion">Acción realizada: "UPLOAD", "DOWNLOAD", "READ" o "DELETE".</param>
        /// <param name="txtUsuario">Nombre del usuario que realizó la acción.</param>
        /// <param name="txtIpOrigen">Dirección IP de origen de la solicitud.</param>
        Task RegistrarAuditoriaAsync(Guid ideArchivo, string txtAccion, string? txtUsuario, string? txtIpOrigen);

        /// <summary>
        /// Lista todos los proveedores de almacenamiento activos.
        /// </summary>
        /// <returns>Colección de entidades de proveedor activo.</returns>
        Task<IEnumerable<ProveedorEntity>> ListarProveedoresActivosAsync();
    }
}
