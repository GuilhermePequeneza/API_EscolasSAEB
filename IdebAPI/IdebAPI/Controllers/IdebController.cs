using Microsoft.AspNetCore.Mvc;
using IdebAPI.Models;
using IdebAPI.ModelView;
using Microsoft.AspNetCore.Cors;

namespace IdebAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class IdebController : ControllerBase
    {
        private readonly IIdebService _idebService;
        private readonly ILogger<IdebController> _logger;

        public IdebController(IIdebService idebService, ILogger<IdebController> logger)
        {
            _idebService = idebService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para consultar escolas no banco de dados (GET)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<EscolasResponse>> GetEscolas(
            [FromQuery] FiltroEscolasRequest filtros = null)
        {
            try
            {
                _logger.LogInformation(
                    "Consulta recebida - UF: {UF}, Município: {Municipio}, Rede: {Rede}, TipoEnsino: {TipoEnsino}",
                    filtros?.UF ?? "Todos",
                    filtros?.Municipio ?? "Todos",
                    filtros?.Rede ?? "Todas",
                    filtros?.TipoEnsino ?? "Todos"
                );

                // Modo 0 = apenas consultar
                var escolas = await _idebService.ProcessarDadosAsync(
                    0,
                    filtros?.UF,
                    filtros?.Municipio,
                    filtros?.Rede,
                    filtros?.TipoEnsino
                );

                _logger.LogInformation("Retornando {Count} escolas", escolas.Count);

                // Mapear para o DTO de resposta consolidado (limitado a 1000 registros)
                var escolasResponse = escolas.Take(1000).Select(e => new EscolaIdebResponse
                {
                    SiglaUF = e.SiglaUF,
                    NomeMunicipio = e.NomeMunicipio,
                    NomeEscola = e.NomeEscola,
                    Rede = e.Rede,

                    // Flags de tipos de ensino
                    OfereceAnosIniciais = e.SNAnosIniciais,
                    OfereceAnosFinais = e.SNAnosFinais,
                    OfereceEnsinoMedio = e.SNEnsinoMedio,                    

                    // IDEB Anos Iniciais
                    IdebAnosIniciais = e.IdebAnosIniciais,
                    AnoReferenciaAnosIniciais = e.AnoReferenciaAnosIniciais,

                    // IDEB Anos Finais
                    IdebAnosFinais = e.IdebAnosFinais,
                    AnoReferenciaAnosFinais = e.AnoReferenciaAnosFinais,

                    // IDEB Ensino Médio
                    IdebEnsinoMedio = e.IdebEnsinoMedio,
                    AnoReferenciaEnsinoMedio = e.AnoReferenciaEnsinoMedio
                });

                var response = new EscolasResponse
                {
                    Total = escolas.Count,
                    Escolas = escolasResponse
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar escolas");
                return StatusCode(500, new
                {
                    erro = "Erro ao consultar dados do IDEB",
                    mensagem = ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint para carregar planilhas e inserir no banco (POST)
        /// </summary>
        [HttpPost("carregar")]
        public async Task<IActionResult> CarregarPlanilhas()
        {
            try
            {
                _logger.LogInformation("Iniciando carga de planilhas no banco de dados");

                // Modo 1 = carregar planilhas e inserir
                var escolas = await _idebService.ProcessarDadosAsync(1);

                _logger.LogInformation("Carga concluída. {Count} escolas consolidadas", escolas.Count);

                // Estatísticas da carga
                var estatisticas = new
                {
                    totalEscolas = escolas.Count,
                    escolasComAnosIniciais = escolas.Count(e => e.SNAnosIniciais),
                    escolasComAnosFinais = escolas.Count(e => e.SNAnosFinais),
                    escolasComEnsinoMedio = escolas.Count(e => e.SNEnsinoMedio),
                    escolasComTodosTipos = escolas.Count(e => e.SNAnosIniciais && e.SNAnosFinais && e.SNEnsinoMedio)
                };

                return Ok(new
                {
                    mensagem = "Planilhas carregadas e dados consolidados com sucesso",
                    dataProcessamento = DateTime.Now,
                    estatisticas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar planilhas");
                return StatusCode(500, new
                {
                    erro = "Erro ao carregar planilhas do IDEB",
                    mensagem = ex.Message
                });
            }
        }
    }
}