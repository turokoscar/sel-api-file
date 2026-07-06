using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Datos
{
    public interface IArchivoRepositorio
    {
        Task<Guid> RegistrarArchivoAsync(ArchivoEntity archivo);
        Task<ArchivoEntity?> ObtenerArchivoPorIdAsync(Guid ideArchivo);
        Task<bool> EliminarArchivoAsync(Guid ideArchivo);
        Task RegistrarAuditoriaAsync(Guid ideArchivo, string txtAccion, string? txtUsuario, string? txtIpOrigen);
        Task<IEnumerable<ProveedorEntity>> ListarProveedoresActivosAsync();
    }
}
