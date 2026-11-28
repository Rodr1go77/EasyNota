using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

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

        //Gera o PDF da nota a partir dos dados do DOM e salva o resultado em caminho temporario 



        /// <summary>
        /// [MODIFICADO] Gera um arquivo PDF incluindo a visualização principal e os painéis
        /// das abas (#nfse, #pessoas, #servicos, #tributacao), exibindo-os temporariamente
        /// durante a impressão via CDP e restaurando o estado original em seguida.
        /// </summary>
        /// <param name="caminhoDestinoTemporario">Caminho completo onde o PDF temporário será salvo.</param>
        /// <param name="seletoresAbas">Seletores dos painéis das abas a tornar visíveis na impressão.</param>
        /// <returns>Caminho final do arquivo PDF gerado.</returns>
        public async Task<string> GerarPdfVisualizacaoAsync(
            string caminhoDestinoTemporario,
            params string[] seletoresAbas) // [INCLUSAO] Permite informar ou usar os seletores padrão das abas.
        {
            // Garante que o navegador e a página tenham sido inicializados.
            if (_page is null)
            {
                throw new InvalidOperationException("O navegador não foi iniciado.");
            }

            if (string.IsNullOrWhiteSpace(caminhoDestinoTemporario))
            {
                throw new ArgumentException("O caminho de destino do PDF é obrigatório.", nameof(caminhoDestinoTemporario));
            }

            // Garante que a pasta temporária de destino exista.
            var pastaDestino = Path.GetDirectoryName(caminhoDestinoTemporario);
            if (!string.IsNullOrWhiteSpace(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino);
            }

            var alvos = (seletoresAbas is { Length: > 0 })
                ? seletoresAbas
                : new[] { "#nfse", "#pessoas", "#servicos", "#tributacao" };

            // [INCLUSAO] Torna os painéis das abas visíveis no DOM apenas durante a geração do PDF,
            // guardando os estilos originais para restaurar após a captura.
            var estadosOriginais = await _page.EvaluateAsync<Dictionary<string, string?>>(@"
        (seletores) => {
            const originais = {};
            for (const sel of seletores) {
                const el = document.querySelector(sel);
                if (el) {
                    originais[sel] = el.getAttribute('style');
                    el.style.display = 'block';
                    el.style.visibility = 'visible';
                    el.style.opacity = '1';
                }
            }
            return originais;
        }", alvos);

            try
            {
                // Abre uma sessão CDP na página ativa para gerar o PDF mesmo com janela visível.
                var cdpSession = await _page.Context.NewCDPSessionAsync(_page);

                var parametrosImpressao = new Dictionary<string, object>
                {
                    ["printBackground"] = true,
                    ["paperWidth"] = 8.27,
                    ["paperHeight"] = 11.69
                };

                var resultado = await cdpSession.SendAsync("Page.printToPDF", parametrosImpressao);

                var base64Pdf = resultado.Value.GetProperty("data").GetString();
                if (string.IsNullOrWhiteSpace(base64Pdf))
                {
                    throw new InvalidOperationException("Não foi possível obter o conteúdo do PDF a partir da página.");
                }

                var bytesPdf = Convert.FromBase64String(base64Pdf);
                await File.WriteAllBytesAsync(caminhoDestinoTemporario, bytesPdf);

                return caminhoDestinoTemporario;
            }
            finally
            {
                // [INCLUSAO] Restaura o atributo style original de cada aba para não alterar a tela da aplicação.
                await _page.EvaluateAsync(@"
            (originais) => {
                for (const [sel, estiloAnterior] of Object.entries(originais || {})) {
                    const el = document.querySelector(sel);
                    if (el) {
                        if (estiloAnterior === null) {
                            el.removeAttribute('style');
                        } else {
                            el.setAttribute('style', estiloAnterior);
                        }
                    }
                }
            }", estadosOriginais);
            }
        }

    }
}
