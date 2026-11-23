namespace EasyNota_DES.Models

{
    public class Pagamento

    {

        public string Id { get; set; } = string.Empty;

        //Removido provisoriamente para evitar conflitos com List<Consulta>
        //public List<string> ConsultaIds { get; set; } = [];
        public DateOnly? DataRecebimento { get; set; } = null;
        public decimal ValorTotal { get; set; } = 0m;

        public List<Consulta> Consultas { get; set; } = [];
        public NotaFiscal? NotaFiscal { get; set; }


    }

}





