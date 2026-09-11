using System;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace sel_api_archivos.Pruebas
{
    /// <summary>
    /// <see cref="SqlException"/> no expone constructores públicos, por lo que las pruebas
    /// que necesitan simular errores concretos de SQL Server (ej. violación de índice único)
    /// deben construirlo vía reflexión usando la misma ruta interna que usa Microsoft.Data.SqlClient.
    /// </summary>
    internal static class SqlExceptionTestFactory
    {
        public static SqlException Create(int number, string message)
        {
            var errorCtor = typeof(SqlError).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception) },
                null)
                ?? throw new InvalidOperationException("No se encontró el constructor esperado de SqlError.");

            var error = errorCtor.Invoke(new object?[] { number, (byte)0, (byte)0, "test-server", message, "test-procedure", 0, null });

            var collectionCtor = typeof(SqlErrorCollection).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
                ?? throw new InvalidOperationException("No se encontró el constructor esperado de SqlErrorCollection.");

            var collection = collectionCtor.Invoke(null);

            var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("No se encontró el método Add esperado de SqlErrorCollection.");

            addMethod.Invoke(collection, new[] { error });

            var createExceptionMethod = typeof(SqlException).GetMethod(
                "CreateException",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(SqlErrorCollection), typeof(string) },
                null)
                ?? throw new InvalidOperationException("No se encontró el método CreateException esperado de SqlException.");

            return (SqlException)createExceptionMethod.Invoke(null, new[] { collection, "" })!;
        }
    }
}
