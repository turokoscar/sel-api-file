using System;

namespace sel_api_archivos.Entidad
{
    public class ArchivoEntity
    {
        public Guid IdeArchivo { get; set; }
        public string CodSistema { get; set; } = string.Empty;
        public string? CodProceso { get; set; }
        public string TxtNombreOriginal { get; set; } = string.Empty;
        public string TxtNombreFisico { get; set; } = string.Empty;
        public string TxtExtension { get; set; } = string.Empty;
        public long CanTamanioBytes { get; set; }
        public string TxtContentType { get; set; } = string.Empty;
        public string TxtChecksumSHA256 { get; set; } = string.Empty;
        public string TxtRutaRelativa { get; set; } = string.Empty;
        public int IdeProveedor { get; set; }
        public bool EstArchivo { get; set; }
        public string? TxtCreadoPor { get; set; }
        public DateTime FecCreacion { get; set; }
    }
}
