using System;
using System.Collections.Generic;
using System.Linq;

namespace sel_api_archivos.Negocio.Storage
{
    /// <summary>
    /// Define la estrategia para resolver un proveedor de almacenamiento por su código.
    /// </summary>
    public interface IStorageProviderResolver
    {
        /// <summary>
        /// Resuelve el proveedor de almacenamiento correspondiente al código proporcionado.
        /// </summary>
        /// <param name="providerCode">Código del proveedor (ej: "LOCAL", "FTP").</param>
        /// <returns>Instancia del proveedor correspondiente.</returns>
        /// <exception cref="KeyNotFoundException">Cuando no existe un proveedor registrado con el código especificado.</exception>
        IStorageProvider Resolve(string providerCode);
    }

    /// <summary>
    /// Implementación de <see cref="IStorageProviderResolver"/> que selecciona el proveedor
    /// correspondiente a partir de una colección de proveedores registrados en DI.
    /// </summary>
    public sealed class StorageProviderResolver : IStorageProviderResolver
    {
        private readonly IEnumerable<IStorageProvider> _providers;

        /// <summary>
        /// Inicializa una nueva instancia con la colección de proveedores disponibles.
        /// </summary>
        /// <param name="providers">Colección de proveedores registrados en el contenedor DI.</param>
        /// <exception cref="ArgumentNullException">Cuando <paramref name="providers"/> es <c>null</c>.</exception>
        public StorageProviderResolver(IEnumerable<IStorageProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        /// <inheritdoc />
        public IStorageProvider Resolve(string providerCode)
        {
            if (_providers == null || !_providers.Any())
            {
                throw new ProveedorNoDisponibleException();
            }

            var provider = _providers.FirstOrDefault(
                p => p.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new ProveedorCodigoDesconocidoException(providerCode);
            }

            return provider;
        }
    }
}
