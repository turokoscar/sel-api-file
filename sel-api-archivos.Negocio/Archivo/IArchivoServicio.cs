using System;
using System.IO;
using System.Threading.Tasks;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Negocio.Archivo
{
    public interface IArchivoServicio
    {
        Task<Guid> SubirArchivoAsync(
            Stream stream, 
            string nombreOriginal, 
            string contentType, 
            string codSistema, 
            string? codProceso, 
            string usuario,
            string ipOrigen
        );

        Task<(Stream Stream, ArchivoEntity Metadata)> DescargarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen);
        Task<ArchivoEntity> ObtenerMetadataAsync(Guid ideArchivo);
        Task<string> LeerContenidoTextoAsync(Guid ideArchivo, string usuario, string ipOrigen);
        Task<bool> EliminarArchivoAsync(Guid ideArchivo, string usuario, string ipOrigen);
    }
}
