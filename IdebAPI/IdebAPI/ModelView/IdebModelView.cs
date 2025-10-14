using System.ComponentModel.DataAnnotations;

namespace IdebAPI.ModelView
{
    // DTO para request de filtros
    public class FiltroEscolasRequest
    {
        [MaxLength(2)]
        public string? UF { get; set; }

        [MaxLength(100)]
        public string? Municipio { get; set; }

        [MaxLength(50)]
        public string? Rede { get; set; }

        [MaxLength(50)]
        public string? TipoEnsino { get; set; }
    }

    // DTO para response da API
    public class EscolasResponse
    {
        public int Total { get; set; }
        public IEnumerable<EscolaIdebResponse> Escolas { get; set; } = new List<EscolaIdebResponse>();
    }

    // DTO para dados da escola na resposta
    public class EscolaIdebResponse
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