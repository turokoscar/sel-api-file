using System;
using System.Collections.Generic;
using System.Linq;

namespace sel_api_archivos.Negocio.Storage
{
    public interface IStorageProviderResolver
    {
        IStorageProvider Resolve(string providerCode);
    }

    public class StorageProviderResolver : IStorageProviderResolver
    {
        private readonly IEnumerable<IStorageProvider> _providers;

        public StorageProviderResolver(IEnumerable<IStorageProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        public IStorageProvider Resolve(string providerCode)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                throw new KeyNotFoundException($"No se encontró un proveedor de almacenamiento implementado para el código: {providerCode}");
            }
            return provider;
        }
    }
}
