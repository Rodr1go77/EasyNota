using Microsoft.Playwright;
using EasyNota_DES.Models;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EasyNota_DES.Services
{
    public class EmissaoNFPlaywright : IAsyncDisposable

    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        //Método para iniciar Playwright e o navegador
        public async Task IniciarNavegadorAsync(bool headless = false)
        {
            if (_browser != null)
            {
                return;
            }

            _playwright = await Playwright.CreateAsync();


            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless
            });

            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                AcceptDownloads = true
            });

            _page = await _context.NewPageAsync();

        }

        //Método para realizar login
        public async Task FazerLoginAsync(string urlLogin, string usuario, string senha)
        {

            await IniciarNavegadorAsync();

            await _page!.GotoAsync(urlLogin, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await _page.FillAsync("#Inscricao", usuario);
            await _page.FillAsync("#Senha", senha);
            //await _page.ClickAsync("button[type='submit']");
            await _page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();
        }

        /* Método para aguardar interação do usuário - Human in the Loop
         Aguarda confirmacao ou mudança de estado da página*/
        public async Task HITLAsync(Task confirmacaoManualTask)
        {

            await Task.WhenAny(
                confirmacaoManualTask,
                _page!.WaitForLoadStateAsync(LoadState.NetworkIdle)
                );

        }

        //Método para capturar o número da nota pós emissão
        public async Task<string> CapturarNumeroNotaAsync(string seletor)
        {

            if (_page is null)
            {
                throw new InvalidOperationException("O navegador não foi iniciado.");
            }

            var elemento = _page.Locator(seletor);
            await elemento.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            var valorInput = await elemento.InputValueAsync();
            var texto = !string.IsNullOrWhiteSpace(valorInput) ? valorInput : await elemento.InnerTextAsync();

            return texto.Trim();

        }

        //Liberando contexto, navegador e instancia depois de usar
        public async ValueTask DisposeAsync()
        {
            if (_context is not null)
            {
                await _context.DisposeAsync();
            }

            if (_browser is not null)
            {
                await _browser.DisposeAsync();
            }

            _playwright?.Dispose();

        }

        //Método para capturar o numero da nota pós-emissão para uso posterior
        public async Task<(string NumeroNota, string CaminhoTempPdf)> CapturaNumNFGeraPdfTempAsync(string seletorNumNota, string caminhoTempPdf, params string[] seletoresAbas)
        {
            if (_page is null)
            {
                throw new InvalidOperationException("O navegador não foi iniciado.");
            }


            var numeroNota = await CapturarNumeroNotaAsync(seletorNumNota);

            var caminhoGerado = await GerarPdfNotaAsync(caminhoTempPdf, seletoresAbas);

            return (numeroNota, caminhoGerado);

        }

        //Método para gerar o arquivo PDF da nota fiscal a partir da visualização atual do navegador,
        //exibindo temporariamente os painéis das abas (hidden/visible) para incluir todo
        //o conteúdo e salvando o resultado em um caminho temporário para posterior
        //organização do arquivo.
        public async Task<string> GerarPdfNotaAsync(
            string caminhoTemporarioPdf, params string[] seletoresAbas)
        {

            if (_page is null)
            {
                throw new InvalidOperationException("O navegador não foi iniciado.");
            }

            if (string.IsNullOrWhiteSpace(caminhoTemporarioPdf))
            {
                throw new ArgumentException("O caminho temporário do PDF é obrigatório", nameof(caminhoTemporarioPdf));

            }

            var pastaTemporaria = Path.GetDirectoryName(caminhoTemporarioPdf);

            if (!string.IsNullOrWhiteSpace(pastaTemporaria))
            {
                Directory.CreateDirectory(pastaTemporaria);
            }

            //Se nenhum seletor tiver sido selecionado usa o padrão do site
            var paineis = (seletoresAbas is { Length: > 0 })
                ? seletoresAbas
                : new[] { "#nfse", "#pessoas", "#servicos", "#tributacao" };

            //Usa JavaScript para mudar propriedades style dos elementos para visible para que fiquem
            //visíveis na página e possam ser impressos no pdf. Isso evita ter que clicar em cada aba,
            //aguardar renderizacao e só então capturar dados.

            var estilosOriginais = await _page.EvaluateAsync<Dictionary<string, string?>>(
                @"(seletores) => {
                const originais = {};
                for (const seletor of seletores){
                const elemento = document.querySelector(seletor);
                if (elemento) {
                    originais[seletor] = elemento.getAttribute('style');
                    elemento.style.display = 'block';
                    elemento.style.visibility = 'visible';
                    elemento.style.opacity = '1';
                    }
                }
                return originais;
            }", paineis);


            try
            {
                var cdpSession = await _page.Context.NewCDPSessionAsync(_page);

                var parametrosPdf = new Dictionary<string, object>
                {
                    ["printBackground"] = true,
                    ["paperWidth"] = 8.27,
                    ["paperHeight"] = 11.69
                };


                var respostaCdp = await cdpSession.SendAsync("Page.printToPDF", parametrosPdf);
                var pdfBase64 = respostaCdp.Value.GetProperty("data").GetString();

                if (string.IsNullOrWhiteSpace(pdfBase64))
                {
                    throw new InvalidOperationException("Não foi possível obter o conteúdo do PDF a partir da página");

                }

                var bytesPdf = Convert.FromBase64String(pdfBase64);
                await File.WriteAllBytesAsync(caminhoTemporarioPdf, bytesPdf);

                return caminhoTemporarioPdf;


            }

            finally
            {
                await _page.EvaluateAsync(
                    @"(originais) => {
                        for (const [seletor, estiloAnterior] of Object.entries(originais || {})) {
                            const elemento = document.querySelector(seletor);
                            if (elemento) {
                                if (estiloAnterior == null) {
                                    elemento.removeAttribute ('style');
                                } else {
                                    elemento.setAttribute('style', estiloAnterior);
                                }
                            }
                        }
                    }", estilosOriginais);

            }


        }

        //Método para construir caminho temporario da nota 
        public async Task<(bool Sucesso, string Mensagem, string? CaminhoFinalPdf)> ConstroiTempPathPdfNFAsync(string notaFiscalId, string seletorNumNota, string caminhoTempPdf, OrganizadorNFsService organizadorNFsService, params string[] seletoresAbas)
        {

            if (string.IsNullOrWhiteSpace(notaFiscalId))
            {
                return (false, "O identificador da nota fiscal é obrigatório", null);
            }

            if (organizadorNFsService is null)
            {
                throw new ArgumentNullException(nameof(organizadorNFsService));

            }

            var (_, caminhoGerado) = await CapturaNumNFGeraPdfTempAsync(
                seletorNumNota,
                caminhoTempPdf,
                seletoresAbas);

            return await organizadorNFsService.OrganizarNFsAsync(notaFiscalId, caminhoGerado);

        }


        //Método que registra e faz a organização final do pdf

        public async Task<(bool Sucesso, string Mensagem, string? CaminhoFinalPdf, EasyNota_DES.Models.NotaFiscal? NotaFiscal)> RegistrarOrganizarNFAsync(string pagamentoId, DateOnly? dataEmissao, string seletorNumNota, string caminhoTempPdf, NotaFiscalService notaFiscalService, OrganizadorNFsService organizadorNFsService, bool moverArquivoTemp = false, params string[] seletoresAbas)
        {

        if (_page is null)
            {
                throw new InvalidOperationException("O navegador não foi iniciado.");
            }

        if (notaFiscalService is null)
            {
                throw new ArgumentNullException(nameof(notaFiscalService));
            }

        if (organizadorNFsService is null)
            {
                throw new ArgumentNullException(nameof(organizadorNFsService));
            }

            var numeroNota = await CapturarNumeroNotaAsync(seletorNumNota);

            var resultadoEmissao = await notaFiscalService.EmitirNFAsync(pagamentoId, numeroNota, dataEmissao);
            
            if(!resultadoEmissao.Sucesso || resultadoEmissao.NotaFiscal is null)
            {
                return (false, resultadoEmissao.Mensagem, null, null);
            }

            var caminhoGerado = await GerarPdfNotaAsync(caminhoTempPdf, seletoresAbas);

            var resultadoOrganizacao = await organizadorNFsService.OrganizarNFsAsync(resultadoEmissao.NotaFiscal.Id, caminhoGerado, moverArquivoTemp);


            return (
                resultadoOrganizacao.Sucesso,
                resultadoOrganizacao.Mensagem,
                resultadoOrganizacao.CaminhoFinal,
                resultadoEmissao.NotaFiscal);

        }


    }

}




