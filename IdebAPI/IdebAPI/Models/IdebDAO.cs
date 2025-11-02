using OfficeOpenXml;
using MySql.Data.MySqlClient;
using System.Data;

namespace IdebAPI.Models
{
    // Interface para o serviço
    public interface IIdebService
    {
        Task<List<EscolaIdeb>> ProcessarDadosAsync(int modo, string uf = null, string municipio = null, string rede = null, string tipoEnsino = null, decimal? idebMinimo = null);
    }

    // DTO temporário para leitura das planilhas
    internal class EscolaTemp
    {
        public string SiglaUF { get; set; } = string.Empty;
        public string NomeMunicipio { get; set; } = string.Empty;
        public string NomeEscola { get; set; } = string.Empty;
        public string Rede { get; set; } = string.Empty;
        public decimal? Ideb { get; set; }
        public string TipoEnsino { get; set; } = string.Empty;
        public int AnoReferencia { get; set; }
    }

    // Serviço para leitura das planilhas e gerenciamento do banco
    public class IdebService : IIdebService
    {
        private readonly string _basePath;
        private readonly string _connectionString;
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

            var connStr = Environment.GetEnvironmentVariable("MYSQL_URL");
            if (!string.IsNullOrEmpty(connStr) && connStr.StartsWith("mysql://"))
            {
                var uri = new Uri(connStr);
                var userInfo = uri.UserInfo.Split(':');
                _connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Uid={userInfo[0]};Pwd={userInfo[1]};SslMode=Preferred;";
            }
            else
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string não configurada!");

            }
            _logger = logger;
        }

        public async Task<List<EscolaIdeb>> ProcessarDadosAsync(
            int modo,
            string uf = null,
            string municipio = null,
            string rede = null,
            string tipoEnsino = null,
            decimal? idebMinimo = null)
        {
            if (modo == 1)
            {
                _logger.LogInformation("Modo 1: Lendo planilhas e inserindo no banco de dados");

                var escolasTemp = await LerTodasPlanilhasAsync();
                await InserirEscolasConsolidadasAsync(escolasTemp);

                return await ConsultarEscolasAsync(uf, municipio, rede, tipoEnsino, idebMinimo);
            }
            else
            {
                _logger.LogInformation("Modo 0: Consultando dados do banco de dados");
                return await ConsultarEscolasAsync(uf, municipio, rede, tipoEnsino, idebMinimo);
            }
        }

        private async Task<List<EscolaTemp>> LerTodasPlanilhasAsync()
        {
            var todasEscolas = new List<EscolaTemp>();
            var tasks = new List<Task<List<EscolaTemp>>>();

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

            _logger.LogInformation("Total de registros carregados das planilhas: {Total}", todasEscolas.Count);
            return todasEscolas;
        }

        private async Task<List<EscolaTemp>> LerPlanilhaAsync(string caminhoArquivo, string tipoEnsino)
        {
            var escolas = new List<EscolaTemp>();

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

                        var escola = new EscolaTemp
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

                _logger.LogInformation("Carregadas {Count} registros do arquivo {Arquivo}", escolas.Count, Path.GetFileName(caminhoArquivo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler arquivo {Arquivo}", caminhoArquivo);
                throw;
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

        private async Task InserirEscolasConsolidadasAsync(List<EscolaTemp> escolasTemp)
        {
            if (escolasTemp == null || !escolasTemp.Any())
            {
                _logger.LogWarning("Nenhuma escola para inserir no banco de dados");
                return;
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Limpa a tabela antes de inserir novos dados
                using (var cmdTruncate = new MySqlCommand("TRUNCATE TABLE Escolas", connection, transaction as MySqlTransaction))
                {
                    await cmdTruncate.ExecuteNonQueryAsync();
                }
                _logger.LogInformation("Tabela Escolas limpa com sucesso");


                var sql = @"
                    INSERT INTO Escolas (
                        NomeEscola, Rede, SiglaUF, NomeMunicipio,
                        SNAnosIniciais, IdebAnosIniciais, AnoReferenciaAnosIniciais,
                        SNAnosFinais, IdebAnosFinais, AnoReferenciaAnosFinais,
                        SNEnsinoMedio, IdebEnsinoMedio, AnoReferenciaEnsinoMedio
                    ) VALUES (
                        @NomeEscola, @Rede, @SiglaUF, @NomeMunicipio,
                        @SNAnosIniciais, @IdebAnosIniciais, @AnoReferenciaAnosIniciais,
                        @SNAnosFinais, @IdebAnosFinais, @AnoReferenciaAnosFinais,
                        @SNEnsinoMedio, @IdebEnsinoMedio, @AnoReferenciaEnsinoMedio
                    )
                    ON DUPLICATE KEY UPDATE
                        Rede = COALESCE(VALUES(Rede), Rede),
                        
                        -- Anos Iniciais
                        SNAnosIniciais = SNAnosIniciais | VALUES(SNAnosIniciais),
                        IdebAnosIniciais = CASE 
                            WHEN VALUES(SNAnosIniciais) = 1 AND (IdebAnosIniciais IS NULL OR VALUES(IdebAnosIniciais) > IdebAnosIniciais) 
                            THEN VALUES(IdebAnosIniciais) 
                            ELSE IdebAnosIniciais 
                        END,
                        AnoReferenciaAnosIniciais = CASE 
                            WHEN VALUES(SNAnosIniciais) = 1 AND (AnoReferenciaAnosIniciais IS NULL OR VALUES(IdebAnosIniciais) > IdebAnosIniciais) 
                            THEN VALUES(AnoReferenciaAnosIniciais) 
                            ELSE AnoReferenciaAnosIniciais 
                        END,
                        
                        -- Anos Finais
                        SNAnosFinais = SNAnosFinais | VALUES(SNAnosFinais),
                        IdebAnosFinais = CASE 
                            WHEN VALUES(SNAnosFinais) = 1 AND (IdebAnosFinais IS NULL OR VALUES(IdebAnosFinais) > IdebAnosFinais) 
                            THEN VALUES(IdebAnosFinais) 
                            ELSE IdebAnosFinais 
                        END,
                        AnoReferenciaAnosFinais = CASE 
                            WHEN VALUES(SNAnosFinais) = 1 AND (AnoReferenciaAnosFinais IS NULL OR VALUES(IdebAnosFinais) > IdebAnosFinais) 
                            THEN VALUES(AnoReferenciaAnosFinais) 
                            ELSE AnoReferenciaAnosFinais 
                        END,
                        
                        -- Ensino Médio
                        SNEnsinoMedio = SNEnsinoMedio | VALUES(SNEnsinoMedio),
                        IdebEnsinoMedio = CASE 
                            WHEN VALUES(SNEnsinoMedio) = 1 AND (IdebEnsinoMedio IS NULL OR VALUES(IdebEnsinoMedio) > IdebEnsinoMedio) 
                            THEN VALUES(IdebEnsinoMedio) 
                            ELSE IdebEnsinoMedio 
                        END,
                        AnoReferenciaEnsinoMedio = CASE 
                            WHEN VALUES(SNEnsinoMedio) = 1 AND (AnoReferenciaEnsinoMedio IS NULL OR VALUES(IdebEnsinoMedio) > IdebEnsinoMedio) 
                            THEN VALUES(AnoReferenciaEnsinoMedio) 
                            ELSE AnoReferenciaEnsinoMedio 
                        END";

                int totalProcessado = 0;

                foreach (var escolaTemp in escolasTemp)
                {
                    using var cmd = new MySqlCommand(sql, connection, transaction as MySqlTransaction);

                    cmd.Parameters.AddWithValue("@NomeEscola", escolaTemp.NomeEscola);
                    cmd.Parameters.AddWithValue("@Rede", escolaTemp.Rede);
                    cmd.Parameters.AddWithValue("@SiglaUF", escolaTemp.SiglaUF);
                    cmd.Parameters.AddWithValue("@NomeMunicipio", escolaTemp.NomeMunicipio);

                    // Define os valores baseado no tipo de ensino
                    bool isAnosIniciais = escolaTemp.TipoEnsino == "Anos Iniciais";
                    bool isAnosFinais = escolaTemp.TipoEnsino == "Anos Finais";
                    bool isEnsinoMedio = escolaTemp.TipoEnsino == "Ensino Médio";

                    cmd.Parameters.AddWithValue("@SNAnosIniciais", isAnosIniciais);
                    cmd.Parameters.AddWithValue("@IdebAnosIniciais", isAnosIniciais && escolaTemp.Ideb.HasValue ? (object)escolaTemp.Ideb.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@AnoReferenciaAnosIniciais", isAnosIniciais && escolaTemp.AnoReferencia > 0 ? (object)escolaTemp.AnoReferencia : DBNull.Value);

                    cmd.Parameters.AddWithValue("@SNAnosFinais", isAnosFinais);
                    cmd.Parameters.AddWithValue("@IdebAnosFinais", isAnosFinais && escolaTemp.Ideb.HasValue ? (object)escolaTemp.Ideb.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@AnoReferenciaAnosFinais", isAnosFinais && escolaTemp.AnoReferencia > 0 ? (object)escolaTemp.AnoReferencia : DBNull.Value);

                    cmd.Parameters.AddWithValue("@SNEnsinoMedio", isEnsinoMedio);
                    cmd.Parameters.AddWithValue("@IdebEnsinoMedio", isEnsinoMedio && escolaTemp.Ideb.HasValue ? (object)escolaTemp.Ideb.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@AnoReferenciaEnsinoMedio", isEnsinoMedio && escolaTemp.AnoReferencia > 0 ? (object)escolaTemp.AnoReferencia : DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    totalProcessado++;
                }

                await transaction.CommitAsync();

                _logger.LogInformation("{Count} registros processados e consolidados no banco de dados", totalProcessado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao inserir escolas no banco de dados");
                throw;
            }
        }

        private async Task<List<EscolaIdeb>> ConsultarEscolasAsync(
            string uf = null,
            string municipio = null,
            string rede = null,
            string tipoEnsino = null,
            decimal? idebMinimo = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"SELECT ID, NomeEscola, Rede, SiglaUF, NomeMunicipio,
                        SNAnosIniciais, IdebAnosIniciais, AnoReferenciaAnosIniciais,
                        SNAnosFinais, IdebAnosFinais, AnoReferenciaAnosFinais,
                        SNEnsinoMedio, IdebEnsinoMedio, AnoReferenciaEnsinoMedio,
                        DataCriacao, DataAtualizacao 
                        FROM Escolas WHERE 1=1";
            var parametros = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(uf))
            {
                sql += " AND SiglaUF = @Uf";
                parametros.Add(new MySqlParameter("@Uf", uf.ToUpper()));
            }

            if (!string.IsNullOrWhiteSpace(municipio))
            {
                sql += " AND NomeMunicipio LIKE @Municipio";
                parametros.Add(new MySqlParameter("@Municipio", $"%{municipio}%"));
            }

            if (!string.IsNullOrWhiteSpace(rede))
            {
                sql += " AND Rede LIKE @Rede";
                parametros.Add(new MySqlParameter("@Rede", $"%{rede}%"));
            }

            if (!string.IsNullOrWhiteSpace(tipoEnsino))
            {
                sql += tipoEnsino.ToLower() switch
                {
                    var t when t.Contains("inicial") => " AND SNAnosIniciais = 1",
                    var t when t.Contains("finais") || t.Contains("final") => " AND SNAnosFinais = 1",
                    var t when t.Contains("médio") || t.Contains("medio") => " AND SNEnsinoMedio = 1",
                    _ => ""
                };
            }

            
            sql += @" AND (
                (SNAnosIniciais = 1 AND IdebAnosIniciais >= @IdebMinimo) OR
                (SNAnosFinais = 1 AND IdebAnosFinais >= @IdebMinimo) OR
                (SNEnsinoMedio = 1 AND IdebEnsinoMedio >= @IdebMinimo)
            )";
            parametros.Add(new MySqlParameter("@IdebMinimo", idebMinimo.Value));
            

            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddRange(parametros.ToArray());

            var escolas = new List<EscolaIdeb>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                escolas.Add(new EscolaIdeb
                {
                    ID = reader.GetInt32("ID"),
                    NomeEscola = reader.IsDBNull(reader.GetOrdinal("NomeEscola")) ? string.Empty : reader.GetString("NomeEscola"),
                    Rede = reader.IsDBNull(reader.GetOrdinal("Rede")) ? string.Empty : reader.GetString("Rede"),
                    SiglaUF = reader.GetString("SiglaUF"),
                    NomeMunicipio = reader.IsDBNull(reader.GetOrdinal("NomeMunicipio")) ? string.Empty : reader.GetString("NomeMunicipio"),

                    SNAnosIniciais = reader.GetBoolean("SNAnosIniciais"),
                    IdebAnosIniciais = reader.IsDBNull(reader.GetOrdinal("IdebAnosIniciais")) ? null : reader.GetDecimal("IdebAnosIniciais"),
                    AnoReferenciaAnosIniciais = reader.IsDBNull(reader.GetOrdinal("AnoReferenciaAnosIniciais")) ? null : reader.GetInt32("AnoReferenciaAnosIniciais"),

                    SNAnosFinais = reader.GetBoolean("SNAnosFinais"),
                    IdebAnosFinais = reader.IsDBNull(reader.GetOrdinal("IdebAnosFinais")) ? null : reader.GetDecimal("IdebAnosFinais"),
                    AnoReferenciaAnosFinais = reader.IsDBNull(reader.GetOrdinal("AnoReferenciaAnosFinais")) ? null : reader.GetInt32("AnoReferenciaAnosFinais"),

                    SNEnsinoMedio = reader.GetBoolean("SNEnsinoMedio"),
                    IdebEnsinoMedio = reader.IsDBNull(reader.GetOrdinal("IdebEnsinoMedio")) ? null : reader.GetDecimal("IdebEnsinoMedio"),
                    AnoReferenciaEnsinoMedio = reader.IsDBNull(reader.GetOrdinal("AnoReferenciaEnsinoMedio")) ? null : reader.GetInt32("AnoReferenciaEnsinoMedio"),

                    DataCriacao = reader.GetDateTime("DataCriacao"),
                    DataAtualizacao = reader.GetDateTime("DataAtualizacao")
                });
            }

            _logger.LogInformation("Consulta retornou {Count} escolas do banco de dados", escolas.Count);

            return escolas;
        }
    }
}