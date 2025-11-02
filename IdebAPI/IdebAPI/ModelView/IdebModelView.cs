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
        [Range(0, 10)]
        public decimal? IdebMinimo { get; set; } = 0;
    }
    // DTO para response da API
    public class EscolasResponse
    {
        public int Total { get; set; }
        public IEnumerable<EscolaIdebResponse> Escolas { get; set; } = new List<EscolaIdebResponse>();
    }
    // DTO para dados da escola na resposta (consolidado)
    public class EscolaIdebResponse
    {
        public string SiglaUF { get; set; } = string.Empty;
        public string NomeMunicipio { get; set; } = string.Empty;
        public string NomeEscola { get; set; } = string.Empty;
        public string Rede { get; set; } = string.Empty;
        // Tipos de ensino disponíveis
        public bool OfereceAnosIniciais { get; set; }
        public bool OfereceAnosFinais { get; set; }
        public bool OfereceEnsinoMedio { get; set; }

        // IDEB Anos Iniciais
        public decimal? IdebAnosIniciais { get; set; }
        public int? AnoReferenciaAnosIniciais { get; set; }
        // IDEB Anos Finais
        public decimal? IdebAnosFinais { get; set; }
        public int? AnoReferenciaAnosFinais { get; set; }
        // IDEB Ensino Médio
        public decimal? IdebEnsinoMedio { get; set; }
        public int? AnoReferenciaEnsinoMedio { get; set; }
    }
}

