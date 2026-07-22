using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using sel_api_archivos.Entidad;

namespace sel_api_archivos.Datos
{
    /// <summary>
    /// Implementación de <see cref="IArchivoRepositorio"/> usando Dapper y SQL Server.
    /// Todas las operaciones se ejecutan a través de procedimientos almacenados en el schema <c>ARC</c>.
    /// </summary>
    public sealed class ArchivoRepositorio : IArchivoRepositorio
    {
        private readonly string _connectionString;

        /// <summary>
        /// Inicializa una nueva instancia con la cadena de conexión a SQL Server.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a la base de datos.</param>
        /// <exception cref="ArgumentNullException">Cuando <paramref name="connectionString"/> es <c>null</c>.</exception>
        public ArchivoRepositorio(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc />
        public async Task<Guid> RegistrarArchivoAsync(ArchivoEntity archivo)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@codSistema", archivo.CodSistema, DbType.String);
            parameters.Add("@codProceso", archivo.CodProceso, DbType.String);
            parameters.Add("@txtNombreOriginal", archivo.TxtNombreOriginal, DbType.String);
            parameters.Add("@txtNombreFisico", archivo.TxtNombreFisico, DbType.String);
            parameters.Add("@txtExtension", archivo.TxtExtension, DbType.String);
            parameters.Add("@canTamanioBytes", archivo.CanTamanioBytes, DbType.Int64);
            parameters.Add("@txtContentType", archivo.TxtContentType, DbType.String);
            parameters.Add("@txtChecksumSHA256", archivo.TxtChecksumSHA256, DbType.String);
            parameters.Add("@txtRutaRelativa", archivo.TxtRutaRelativa, DbType.String);
            parameters.Add("@ideProveedor", archivo.IdeProveedor, DbType.Int32);
            parameters.Add("@txtCreadoPor", archivo.TxtCreadoPor, DbType.String);
            parameters.Add("@ideArchivoGenerated", dbType: DbType.Guid, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "ARC.API_FILE_SP_C_ARCHIVO",
                parameters,
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);

            return parameters.Get<Guid>("@ideArchivoGenerated");
        }

        /// <inheritdoc />
        public async Task<ArchivoEntity?> ObtenerArchivoPorIdAsync(Guid ideArchivo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ArchivoEntity>(
                "ARC.API_FILE_SP_R_ARCHIVO",
                new { ideArchivo },
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> EliminarArchivoAsync(Guid ideArchivo)
        {
            using var connection = new SqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(
                "ARC.API_FILE_SP_D_ARCHIVO",
                new { ideArchivo },
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);
            return rowsAffected > 0;
        }

        /// <inheritdoc />
        public async Task RegistrarAuditoriaAsync(Guid ideArchivo, string txtAccion, string? txtUsuario, string? txtIpOrigen)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "ARC.API_FILE_SP_C_AUDITORIA",
                new { ideArchivo, txtAccion, txtUsuario, txtIpOrigen },
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ProveedorEntity>> ListarProveedoresActivosAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ProveedorEntity>(
                "ARC.API_FILE_SP_R_PROVEEDORES",
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);
        }
    }
}
