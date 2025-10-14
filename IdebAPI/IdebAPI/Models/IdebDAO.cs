using OfficeOpenXml;

namespace IdebAPI.Models
{
    // Interface para o serviço (facilita testes)
    public interface IIdebService
    {
        Task<List<EscolaIdeb>> LerTodasPlanilhasAsync();
        List<EscolaIdeb> FiltrarEscolas(List<EscolaIdeb> escolas, string uf = null, string municipio = null, string rede = null, string tipoEnsino = null);
    }

    // Serviço para leitura das planilhas
    public class IdebService : IIdebService
    {
        private readonly string _basePath;
        private readonly ILogger<IdebService> _logger;

        private static readonly Dictionary<string, string> PLANILHAS_CONFIG = new()
        {
            { "divulgacao_anos_iniciais_escolas_2023.xlsx", "Anos Iniciais" },
            { "divulgacao_anos_finais_escolas_2023.xlsx", "Anos Finais" },
            { "divulgacao_ensino_medio_escolas_2023.xlsx", "Ensino Médio" }
        };

        private static readonly Dictionary<string, (int[] colunas, int[] anos)> CONFIG_IDEB = new()
        {
            {
                "Anos Iniciais",
                (new[] { 116, 115, 114, 113, 112, 111, 110, 109, 108, 107 },
                 new[] { 2023, 2021, 2019, 2017, 2015, 2013, 2011, 2009, 2007, 2005 })
            },
            {
                "Anos Finais",
                (new[] { 106, 105, 104, 103, 102, 101, 100, 99, 98, 97 },
                 new[] { 2023, 2021, 2019, 2017, 2015, 2013, 2011, 2009, 2007, 2005 })
            },
            {
                "Ensino Médio",
                (new[] { 46, 45, 44, 43 },
                 new[] { 2023, 2021, 2019, 2017 })
            }
        };

        private const int LINHA_INICIO_DADOS = 11;

        public IdebService(IConfiguration configuration, ILogger<IdebService> logger)
        {
            _basePath = configuration["IdebService:BasePath"] ?? "PlanilhasEscolas";
            _logger = logger;

        }

        public async Task<List<EscolaIdeb>> LerTodasPlanilhasAsync()
        {
            var todasEscolas = new List<EscolaIdeb>();
            var tasks = new List<Task<List<EscolaIdeb>>>();

            foreach (var planilha in PLANILHAS_CONFIG)
            {
                var caminhoArquivo = Path.Combine(_basePath, planilha.Key);

                if (File.Exists(caminhoArquivo))
                {
                    tasks.Add(LerPlanilhaAsync(caminhoArquivo, planilha.Value));
                }
                else
                {
                    _logger.LogWarning("Arquivo não encontrado: {CaminhoArquivo}", caminhoArquivo);
                }
            }

            var resultados = await Task.WhenAll(tasks);

            foreach (var escolas in resultados)
            {
                todasEscolas.AddRange(escolas);
            }

            _logger.LogInformation("Total de escolas carregadas: {Total}", todasEscolas.Count);
            return todasEscolas;
        }

        private async Task<List<EscolaIdeb>> LerPlanilhaAsync(string caminhoArquivo, string tipoEnsino)
        {
            var escolas = new List<EscolaIdeb>();

            try
            {
                await Task.Run(() =>
                {
                    using var package = new ExcelPackage(new FileInfo(caminhoArquivo));
                    var worksheet = package.Workbook.Worksheets[0];

                    if (worksheet?.Dimension == null)
                    {
                        _logger.LogWarning("Planilha vazia ou inválida: {Arquivo}", caminhoArquivo);
                        return;
                    }

                    var ultimaLinha = worksheet.Dimension.End.Row;

                    if (tipoEnsino == "Ensino Médio")
                        ultimaLinha -= 14;
                    else
                        ultimaLinha -= 16;

                    for (int linha = LINHA_INICIO_DADOS; linha <= ultimaLinha; linha++)
                    {
                        if (worksheet.Cells[linha, 1].Value == null) continue;

                        var (ideb, anoReferencia) = BuscarIdebMaisRecente(worksheet, linha, tipoEnsino);

                        var escola = new EscolaIdeb
                        {
                            SiglaUF = worksheet.Cells[linha, 1].Value?.ToString()?.Trim() ?? string.Empty,
                            NomeMunicipio = worksheet.Cells[linha, 3].Value?.ToString()?.Trim() ?? string.Empty,
                            NomeEscola = worksheet.Cells[linha, 5].Value?.ToString()?.Trim() ?? string.Empty,
                            Rede = worksheet.Cells[linha, 6].Value?.ToString()?.Trim() ?? string.Empty,
                            Ideb = ideb,
                            TipoEnsino = tipoEnsino,
                            AnoReferencia = anoReferencia
                        };

                        escolas.Add(escola);
                    }
                });

                _logger.LogInformation("Carregadas {Count} escolas do arquivo {Arquivo}", escolas.Count, Path.GetFileName(caminhoArquivo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler arquivo {Arquivo}", caminhoArquivo);
                throw; // Re-throw para que o controller possa tratar
            }

            return escolas;
        }

        private (decimal? ideb, int anoReferencia) BuscarIdebMaisRecente(ExcelWorksheet worksheet, int linha, string tipoEnsino)
        {
            if (!CONFIG_IDEB.TryGetValue(tipoEnsino, out var config))
            {
                _logger.LogWarning("Tipo de ensino não configurado: {TipoEnsino}", tipoEnsino);
                return (null, 0);
            }

            var (colunas, anos) = config;

            for (int i = 0; i < colunas.Length; i++)
            {
                var valor = worksheet.Cells[linha, colunas[i]].Value?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(valor) && valor != "-" && decimal.TryParse(valor, out decimal idebValue))
                {
                    return (idebValue, anos[i]);
                }
            }

            return (null, 0);
        }

        public List<EscolaIdeb> FiltrarEscolas(List<EscolaIdeb> escolas, string uf = null, string municipio = null, string rede = null, string tipoEnsino = null)
        {
            if (escolas == null) return new List<EscolaIdeb>();

            var query = escolas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(uf))
                query = query.Where(e => e.SiglaUF.Contains(uf, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(municipio))
                query = query.Where(e => e.NomeMunicipio.Contains(municipio, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(rede))
                query = query.Where(e => e.Rede.Contains(rede, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(tipoEnsino))
                query = query.Where(e => e.TipoEnsino.Contains(tipoEnsino, StringComparison.OrdinalIgnoreCase));

            return query.ToList();
        }
    }
}
