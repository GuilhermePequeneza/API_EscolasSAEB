using System.ComponentModel.DataAnnotations;

namespace Cadastro.ModelView
{
    public class InformacaoCadastroRequest
    {
        public string Email { get; set; }
               
        public string Senha { get; set; }

        public string Nome { get; set; }

    }
    public class InformacaoLoginRequest
    {
        public string Email { get; set; }

        public string Senha { get; set; }

    }

}