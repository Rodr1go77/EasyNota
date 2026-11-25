using EasyNota_DES.Data;
using EasyNota_DES.Models;
using Microsoft.EntityFrameworkCore;
namespace EasyNota_DES.Services
{
    public class NotaFiscalService
    {
        private readonly EasyNotaDbContext _context;

        public NotaFiscalService(EasyNotaDbContext context)
        {
            _context = context;
        }

        //Obtem lista de todas as notas fiscais emitidas
        public async Task<List<NotaFiscal>> ObterTodasNFAsync()
        {
            return await _context.NotasFiscais
                .Include(nf => nf.Pagamento)
                    .ThenInclude(p => p!.Consultas)
                .OrderByDescending(nf => nf.DataEmissao)
                .ToListAsync();
        }


        //Gera a nota fiscal no banco de dados para posterior emissao automatica
        public async Task<(bool Sucesso, string Mensagem, NotaFiscal? NotaFiscal)> EmitirNFAsync(string pagamentoId, string numeroNota, DateOnly? dataEmissao)
        {
            if (string.IsNullOrWhiteSpace(pagamentoId))
            {
                return (false, "O identificador do pagamento é obrigatório.", null);
            }

            if (string.IsNullOrWhiteSpace(numeroNota))
            {
                return (false, "O número da nota fiscal é obrigatório.", null);
            }

            var pagamento = await _context.Pagamentos
                .Include(p => p.Consultas)
                .Include(p => p.NotaFiscal)
                .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento is null)
            {
                return (false, "Pagamento não encontrado", null);
            }

            if(pagamento.NotaFiscal is not null)
            {
                return (false, "Este pagamento já possui uma nota fiscal associada.", null);
            }

            var notaFiscal = new NotaFiscal
            {
                Id = Guid.NewGuid().ToString(),
                PagamentoId = pagamento.Id,
                NumeroNota = numeroNota.Trim(),
                DataEmissao = dataEmissao?.ToDateTime(TimeOnly.MinValue),
                StatusEmissao = "Pendente"
            };

            foreach (var consulta in pagamento.Consultas)
            {
                consulta.Status = "Faturada";
            }


            _context.NotasFiscais.Add(notaFiscal);
            await _context.SaveChangesAsync();

            return (true, "Nota fiscal registrada no banco de dados com sucesso.", notaFiscal);

        }
    }
}
