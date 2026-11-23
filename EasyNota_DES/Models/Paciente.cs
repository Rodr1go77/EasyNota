namespace EasyNota_DES.Models

{
    public class Paciente
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string NumeroLogradouro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;

        public List<Consulta> Consultas { get; set; } = [];


    }
}
