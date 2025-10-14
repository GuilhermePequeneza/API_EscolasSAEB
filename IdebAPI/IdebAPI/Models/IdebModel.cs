

namespace IdebAPI.Models
{
    // Modelo para representar os dados da escola
    public class EscolaIdeb
    {
        public string SiglaUF { get; set; } = string.Empty;
        public string NomeMunicipio { get; set; } = string.Empty;
        public string NomeEscola { get; set; } = string.Empty;
        public string Rede { get; set; } = string.Empty;
        public decimal? Ideb { get; set; }
        public string TipoEnsino { get; set; } = string.Empty;
        public int AnoReferencia { get; set; }
    }
}