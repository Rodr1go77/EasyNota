using EasyNota_DES.Data;
using EasyNota_DES.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES.Services
{
    public class PagamentoService
    {

        private readonly EasyNotaDbContext _context;

        public PagamentoService(EasyNotaDbContext context)
        {
            _context = context;
        }

        //Método para registrar o pagamento de uma consulta e marcar ela como paga.
        public async Task<(bool Sucesso, string Mensagem, Pagamento? Pagamento)> RegistrarPagamentoAsync(List<string> consultaIds, DateOnly? dataRecebimento)
        {
            if (consultaIds is null || consultaIds.Count == 0)
            {
                return (false, "Selecione ao menos uma consulta para registrar o pagamento.", null);
            }


            // Remove eventuais IDs duplicados para evitar somar ou processar a mesma consulta duas vezes.
            var idsUnicos = consultaIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (idsUnicos.Count == 0)
            {
                return (false, "Os identificadores informados são inválidos.", null);
            }

            var consultas = await _context.Consultas
                .Where(c => idsUnicos.Contains(c.Id))
                .ToListAsync();

            if (consultas.Count != idsUnicos.Count)
            {
                return (false, "Uma ou mais consultas informadas não foram encontradas", null);
            }

            if (consultas.Any(c => c.Status == "Paga" || c.Status == "Faturada"))
            {
                return (false, "Existem consultas selecionadas já pagas ou faturadas.", null);
            }

            var valorTotal = consultas.Sum(c => c.Valor);

            var pagamento = new Pagamento
            {
                Id = Guid.NewGuid().ToString(),
                DataRecebimento = dataRecebimento ?? DateOnly.FromDateTime(DateTime.Today),
                ValorTotal = valorTotal,
                Consultas = consultas
            };

           foreach (var consulta in consultas)
            {
                consulta.Status = "Paga";
            }

            _context.Pagamentos.Add(pagamento);
            await _context.SaveChangesAsync();

            return (true, "Pagamento da(s) consulta(s) registrado com sucesso", pagamento);

        }


    }
}
