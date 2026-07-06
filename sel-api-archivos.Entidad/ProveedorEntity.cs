using System;

namespace sel_api_archivos.Entidad
{
    public class ProveedorEntity
    {
        public int IdeProveedor { get; set; }
        public string CodProveedor { get; set; } = string.Empty;
        public string TxtNombre { get; set; } = string.Empty;
        public string JsnConfiguracion { get; set; } = string.Empty;
        public bool FlgActivo { get; set; }
        public DateTime FecRegistro { get; set; }
    }
}
