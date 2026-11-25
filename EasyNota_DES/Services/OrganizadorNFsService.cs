using EasyNota_DES.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES.Services
{
    public class OrganizadorNFsService
    {
        private readonly EasyNotaDbContext _context;

        private const string DiretorioPadraoNFs = @"C:\EasyNotas\Notas Fiscais";

        public OrganizadorNFsService(EasyNotaDbContext context)
        {
            _context = context;
        }

        //Método para limpeza/ajuste de nomes de arquivos
        public static string SanitizarNomeArquivo(string? texto, string padraoQuandoVazio)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return padraoQuandoVazio;
            }

            var limpo = string.Concat(texto.Trim()
                .Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));

            return string.IsNullOrWhiteSpace(limpo)
                ? padraoQuandoVazio
                : limpo.Replace(' ', '_');
        }

        //Método para organizar as notas fiscais (arquivos pdf) geradas
        public async Task<(bool Sucesso, string Mensagem, string? CaminhoFinal)> OrganizarNFsAsync(
            string notaFiscalId,
            string caminhoOrigemPDF,
            bool moverArquivo = false)
        {
            if (string.IsNullOrWhiteSpace(notaFiscalId) || string.IsNullOrWhiteSpace(caminhoOrigemPDF))
            {
                return (false, "Informe a nota fiscal, o arquivo pdf de origem e diretório de destino.", null);
            }
            ;

            if (!File.Exists(caminhoOrigemPDF))
            {
                return (false, "O arquivo PDF da nota não foi encontrado no local informado.", null);
            }


            var notaFiscal = await _context.NotasFiscais
                .Include(nf => nf.Pagamento)
                .ThenInclude(p => p!.Consultas)
                .ThenInclude(c => c.Paciente)
                .FirstOrDefaultAsync(nf => nf.Id == notaFiscalId);


            if (notaFiscal is null)
            {
                return (false, "Nota fiscal não encontrada no banco de dados", null);
            }

            var dataRef = notaFiscal.DataEmissao ?? DateTime.Today;

            var pastaOrganizada = Path.Combine(
                DiretorioPadraoNFs,
                dataRef.Year.ToString("0000"),
                dataRef.Month.ToString("00"));


            Directory.CreateDirectory(pastaOrganizada);

            var nomePacienteBruto = notaFiscal.Pagamento?.Consultas
                .Select(c => c.Paciente?.Nome)
                .FirstOrDefault(nome => !string.IsNullOrWhiteSpace(nome));

            var numeroLimpo = SanitizarNomeArquivo(notaFiscal.NumeroNota, "NFSe_Sem_Numero");
            var nomePacienteLimpo = SanitizarNomeArquivo(nomePacienteBruto, "Paciente");

            var nomeArquivoFinal = $"NFSe_{numeroLimpo}_{nomePacienteLimpo}.pdf";
            var caminhoFinal = Path.Combine(pastaOrganizada, nomeArquivoFinal);


            //Movimentacao dos arquivos para pasta correta

            if (moverArquivo)
            {
                if (File.Exists(caminhoFinal))
                {
                    File.Delete(caminhoFinal);
                }

                File.Move(caminhoOrigemPDF, caminhoFinal);

            }
            else
            {
                File.Copy(caminhoOrigemPDF, caminhoFinal, overwrite: true);
            }

            notaFiscal.CaminhoArquivoPDF = caminhoFinal;
            notaFiscal.StatusEmissao = "Emitida";

            await _context.SaveChangesAsync();

            return (true, "Documento fiscal armazenado com sucesso", caminhoFinal);



        }





    }
}
