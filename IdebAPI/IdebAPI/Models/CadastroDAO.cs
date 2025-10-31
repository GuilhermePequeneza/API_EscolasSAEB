
using Cadastro.Controllers;
using Cadastro.ModelView;
using MySql.Data.MySqlClient; 
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


namespace Cadastro.Models
{
    // Interface para o serviço (facilita testes)
    public interface ICadastroDAO
    {
        Task CriarUsuario(InformacaoCadastroRequest informacoes);
        Task Login(InformacaoLoginRequest informacoes);
    }

    // Serviço para leitura das planilhas
    public partial class CadastroDAO : ICadastroDAO
    {
        private readonly ILogger<CadastroDAO> _logger;
        private readonly string _connectionString;
        public CadastroDAO(ILogger<CadastroDAO> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task CriarUsuario(InformacaoCadastroRequest informacoes)
        {
            string email = informacoes.Email.Trim().ToLower(); 
            string senha = informacoes.Senha.Trim();
            string nome = informacoes.Nome.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new DadosInvalidosException("Email é obrigatório");
            }

            if (string.IsNullOrWhiteSpace(senha))
            {
                throw new DadosInvalidosException("Senha é obrigatório");
            }
            if (string.IsNullOrWhiteSpace(nome))
            {
                throw new DadosInvalidosException("Nome é obrigatório");
            }

            string pattern = "^[\\w\\.-]+@[\\w\\.-]+\\.\\w+$";

            if (!Regex.IsMatch(email, pattern))
            {
                throw new DadosInvalidosException("Email Invalido");
            }

            if (senha.Length < 8)
            {
                throw new DadosInvalidosException("Senha tem que ter pelo menos 8 caracteres");
            }

            if (!senha.Any(char.IsLetter))
            {
                throw new DadosInvalidosException("Senha tem que ter pelo menos 1 letra");
            }

            if (!senha.Any(char.IsDigit))
            {
                throw new DadosInvalidosException("Senha tem que ter pelo menos 1 número");
            }
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verificar se email já existe
                string checkQuery = "SELECT COUNT(*) FROM Usuarios WHERE Email = @Email";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", email);

                int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (count > 0)
                {
                    throw new DadosInvalidosException("Email já está cadastrado");
                }

                // Hash da senha
                string senhaHash = HashPassword(senha);

                // Inserir novo usuário
                string insertQuery = @"
                    INSERT INTO Usuarios (Email, Senha, Nome, DataCriacao) 
                    VALUES (@Email, @Senha, @Nome, @DataCriacao)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@Senha", senhaHash);
                insertCommand.Parameters.AddWithValue("@Nome", nome);
                insertCommand.Parameters.AddWithValue("@DataCriacao", DateTime.Now);

                await insertCommand.ExecuteNonQueryAsync();

                _logger.LogInformation($"Usuário criado com sucesso: {email}");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, $"Erro de banco de dados ao criar usuário: {email}");
                throw new Exception("Erro interno do servidor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro inesperado ao criar usuário: {email}");
                throw;
            }
        }
        public async Task Login(InformacaoLoginRequest informacoes)
        {
            string email = informacoes.Email.Trim().ToLower();
            string senha = informacoes.Senha.Trim();
            string senhaHash = HashPassword(senha);

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string searchQuery = "SELECT COUNT(*) FROM Usuarios WHERE Email = @Email AND Senha = @Senha";

                using var searchCommand = new MySqlCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@Email", email);
                searchCommand.Parameters.AddWithValue("@Senha", senhaHash);

                int count = Convert.ToInt32(await searchCommand.ExecuteScalarAsync());

                if (count > 0)
                {
                    _logger.LogInformation($"Login realizado com sucesso: {email}");
                }
                else
                {
                    throw new DadosInvalidosException("Email ou Senha Incorretos");
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, $"Erro de banco de dados ao fazer login: {email}");
                throw new Exception("Erro interno do servidor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro inesperado ao fazer login: {email}");
                throw;
            }
        }

        private string HashPassword(string password)
        {
            // Deixar mais seguro no futuro
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}


