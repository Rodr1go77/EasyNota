namespace EasyNota_DES.Models

{
    public class Consulta

    {
        public string Id { get; set; } = string.Empty;
        public string PacienteId { get; set; } = string.Empty;
        public DateOnly? DataConsulta { get; set; } = null;
        public decimal Valor { get; set; } = 0m;
        public string Status { get; set; } = string.Empty;

        public Paciente? Paciente { get; set; }
        public string? PagamentoId { get; set; }
        public Pagamento? Pagamento { get; set; }
    }
}