using System;
using System.IO;
using System.Threading.Tasks;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Negocio.Archivo
{
    /// <summary>
    /// Define las operaciones de negocio para la gestión de archivos.
    /// </summary>
    public interface IArchivoServicio
    {
        /// <summary>
        /// Sube un archivo al almacenamiento configurado, registra su metadata en BD
        /// y emite un registro de auditoría.
        /// </summary>
        /// <param name="stream">Stream del contenido del archivo.</param>
        /// <param name="nombreOriginal">Nombre original del archivo.</param>
        /// <param name="contentType">Content-Type MIME del archivo.</param>
        /// <param name="codSistema">Código del sistema consumidor (ej: "KOFIX").</param>
        /// <param name="codProceso">Código opcional del proceso dentro del sistema.</param>
        /// <param name="usuario">Nombre del usuario que realiza la subida.</param>
        /// <param name="ipOrigen">Dirección IP de origen.</param>
        /// <returns>GUID del archivo registrado.</returns>
        /// <exception cref="ArgumentException">Cuando el archivo está vacío o supera los límites del proveedor.</exception>
        Task<Guid> SubirArchivoAsync(
            Stream stream,
            string nombreOriginal,
            string contentType,
            string codSistema,
            string? codProceso,
            string usuario,
            string ipOrigen
        );

        /// <summary>
        /// Descarga un archivo desde el almacenamiento y emite un registro de auditoría.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <param name="usuario">Nombre del usuario que realiza la descarga.</param>
        /// <param name="ipOrigen">Dirección IP de origen.</param>
        /// <returns>Tupla con el stream del contenido y la metadata del archivo.</returns>
        /// <exception cref="FileNotFoundException">Cuando el archivo no existe o no se encuentra el archivo físico.</exception>
        Task<(Stream Stream, ArchivoEntity Metadata)> DescargarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen);

        /// <summary>
        /// Obtiene la metadata de un archivo por su identificador.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <returns>Entidad con la metadata del archivo.</returns>
        /// <exception cref="FileNotFoundException">Cuando el archivo no existe.</exception>
        Task<ArchivoEntity> ObtenerMetadataAsync(Guid ideArchivo);

        /// <summary>
        /// Lee el contenido de texto de un archivo y emite un registro de auditoría.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <param name="usuario">Nombre del usuario que realiza la lectura.</param>
        /// <param name="ipOrigen">Dirección IP de origen.</param>
        /// <returns>Contenido del archivo como cadena de texto.</returns>
        /// <exception cref="FileNotFoundException">Cuando el archivo no existe o no se encuentra el archivo físico.</exception>
        Task<string> LeerContenidoTextoAsync(Guid ideArchivo, string usuario, string ipOrigen);

        /// <summary>
        /// Elimina un archivo del almacenamiento físico y realiza un soft delete en BD,
        /// emitiendo un registro de auditoría.
        /// </summary>
        /// <param name="ideArchivo">Identificador del archivo.</param>
        /// <param name="usuario">Nombre del usuario que realiza la eliminación.</param>
        /// <param name="ipOrigen">Dirección IP de origen.</param>
        /// <returns><c>true</c> si el archivo fue eliminado; <c>false</c> si no fue encontrado.</returns>
        Task<bool> EliminarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen);
    }
}
