using Cadastro.Models;
using Cadastro.ModelView;
using IdebAPI.ModelView;
using Microsoft.AspNetCore.Mvc;

namespace Cadastro.Controllers
{
    public class UsuarioJaExisteException : Exception
    {
        public UsuarioJaExisteException(string message) : base(message) { }
    }

    public class DadosInvalidosException : Exception
    {
        public DadosInvalidosException(string message) : base(message) { }
    }

    // Controller da API para expor os dados
    [ApiController]
    [Route("api/[controller]")]
    public class CadastroController : ControllerBase
    {
        private readonly ICadastroDAO _CadastroDAO; 
        private readonly ILogger<CadastroController> _logger;

        public CadastroController(ICadastroDAO CadastroDAO, ILogger<CadastroController> logger)
        {
            _CadastroDAO = CadastroDAO;
            _logger = logger;
        }

        [HttpPost("PostCadastro")]
        public async Task<IActionResult> PostCadastro(InformacaoCadastroRequest informacoes)
        {
            try
            {
                await _CadastroDAO.CriarUsuario(informacoes);
                return Ok();
            }
            catch(UsuarioJaExisteException ex)
            {
                _logger.LogWarning(ex.Message);
                return Conflict(new { error = ex.Message });
            }
            catch (DadosInvalidosException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar usuário");
                return BadRequest("Erro interno do servidor");
            }
        }
        [HttpPost("PostLogin")]
        public async Task<IActionResult> PostLogin(InformacaoLoginRequest informacoes)
        {
            try
            {
                await _CadastroDAO.Login(informacoes);
                return Ok();
            }
            catch (DadosInvalidosException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar usuário");
                return BadRequest("Erro interno do servidor");
            }

        }

    }
}