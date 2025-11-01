

namespace IdebAPI.Models
{
    // Modelo para representar os dados da escola
    public class EscolaIdeb
    {
        public int ID { get; set; }
        public string SiglaUF { get; set; } = string.Empty;
        public string NomeMunicipio { get; set; } = string.Empty;
        public string NomeEscola { get; set; } = string.Empty;
        public string Rede { get; set; } = string.Empty;        
        public bool SNAnosIniciais { get; set; } 
        public decimal? IdebAnosIniciais { get; set; }
        public int? AnoReferenciaAnosIniciais { get; set; }
        public bool SNAnosFinais { get; set; } 
        public decimal? IdebAnosFinais { get; set; }
        public int? AnoReferenciaAnosFinais { get; set; }
        public bool SNEnsinoMedio { get; set; } 
        public decimal? IdebEnsinoMedio { get; set; }
        public int? AnoReferenciaEnsinoMedio { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
}