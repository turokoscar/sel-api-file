using System;

namespace sel_api_archivos.Entidad
{
    /// <summary>
    /// Representa un proveedor de almacenamiento configurado en el sistema.
    /// </summary>
    public class ProveedorEntity
    {
        /// <summary>
        /// Identificador único del proveedor.
        /// </summary>
        public int IdeProveedor { get; set; }

        /// <summary>
        /// Código del proveedor (ej: "LOCAL", "FTP"). Usado para resolver el proveedor en runtime.
        /// </summary>
        public string CodProveedor { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo del proveedor.
        /// </summary>
        public string TxtNombre { get; set; } = string.Empty;

        /// <summary>
        /// Configuración JSON del proveedor. Contiene campos específicos según el tipo de proveedor
        /// (ej: <c>localStoragePath</c> para LOCAL, <c>host</c>, <c>port</c> para FTP).
        /// </summary>
        public string JsnConfiguracion { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el proveedor está activo. Solo los proveedores activos son considerados
        /// al momento de resolver el proveedor por defecto.
        /// </summary>
        public bool FlgActivo { get; set; }

        /// <summary>
        /// Fecha y hora de registro del proveedor.
        /// </summary>
        public DateTime FecRegistro { get; set; }
    }
}
