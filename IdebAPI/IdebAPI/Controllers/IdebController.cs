using Microsoft.AspNetCore.Mvc;
using IdebAPI.Models;
using IdebAPI.ModelView;

namespace IdebAPI.Controllers
{
    // Controller da API para expor os dados
    [ApiController]
    [Route("api/[controller]")]
    public class IdebController : ControllerBase
    {
        private readonly IIdebService _idebService; // lógica de negócio (ler planilhas, filtrar dados)
        private readonly ILogger<IdebController> _logger;

        public IdebController(IIdebService idebService, ILogger<IdebController> logger)
        {
            _idebService = idebService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<EscolasResponse>> GetEscolasFiltradas([FromQuery] FiltroEscolasRequest filtros)
        {
            try
            {
                _logger.LogInformation("Buscando escolas com filtros: UF={UF}, Municipio={Municipio}, Rede={Rede}, TipoEnsino={TipoEnsino}",
                    filtros.UF, filtros.Municipio, filtros.Rede, filtros.TipoEnsino);

                var todasEscolas = await _idebService.LerTodasPlanilhasAsync();
                var escolasFiltradas = _idebService.FiltrarEscolas(
                    todasEscolas,
                    filtros.UF,
                    filtros.Municipio,
                    filtros.Rede,
                    filtros.TipoEnsino);

                _logger.LogInformation("Retornando {Count} escolas após filtros", escolasFiltradas.Count);

                // Mapear para o DTO de resposta
                var escolasResponse = escolasFiltradas.Take(1000).Select(e => new EscolaIdebResponse
                {
                    SiglaUF = e.SiglaUF,
                    NomeMunicipio = e.NomeMunicipio,
                    NomeEscola = e.NomeEscola,
                    Rede = e.Rede,
                    Ideb = e.Ideb,
                    TipoEnsino = e.TipoEnsino,
                    AnoReferencia = e.AnoReferencia
                });

                var response = new EscolasResponse
                {
                    Total = escolasFiltradas.Count,
                    Escolas = escolasResponse
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar escolas");
                return StatusCode(500, new { Erro = "Erro interno do servidor" });
            }
        }
    }
}