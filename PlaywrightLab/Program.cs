#region TESTE

//using Microsoft.Playwright;

//var playwright = await Playwright.CreateAsync();

//Console.WriteLine("Playwright carregado com sucesso!");
#endregion

#region Abre Pagina Inicial

//using Microsoft.Playwright;

//var playwright = await Playwright.CreateAsync();

//await using var browser =
//    await playwright.Chromium.LaunchAsync(new()
//    {
//        Channel = "msedge",
//        Headless = false
//    });

//var page = await browser.NewPageAsync();

//await page.GotoAsync("https://www.nfse.gov.br/EmissorNacional/Login");

//Console.ReadLine();

#endregion

#region LOGIN

//using Microsoft.Playwright;

//var playwright = await Playwright.CreateAsync();
//var browser = await playwright.Chromium.LaunchAsync(new()
//{
//    Channel = "msedge",
//    Headless = false
//});

//var page = await browser.NewPageAsync();

//await page.GotoAsync("https://www.nfse.gov.br/EmissorNacional/Login");

//// CPF/CNPJ
//await page.Locator("#Inscricao")
//    .FillAsync("00467702918");

//// Senha
//await page.Locator("#Senha")
//    .FillAsync("Wonder@79");

//// Clicar em Entrar
//await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" })
//    .ClickAsync();

//Console.WriteLine("Login realizado.");
//Console.WriteLine("Pressione ENTER para fechar o navegador...");

//Console.ReadLine();

//await browser.CloseAsync();


#endregion



using System;
using System.Threading.Tasks;
using EasyNota_DES.Services;

namespace TestePdfNfSe
{
    /// <summary>
    /// Classe auxiliar de teste para validar a geração do PDF da visualização
    /// da NFS-e usando EmissaoNFPlaywright sem alterar o fluxo principal da aplicação web.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Ponto de entrada assíncrono do console de teste.
        /// </summary>
        private static async Task Main()
        {
            // Caminho temporário onde o PDF gerado no teste será salvo para inspeção visual.
            var caminhoPdfTeste = @"C:\Temp\teste_visualizacao_nfse.pdf";

            // 'await using' garante que DisposeAsync() será chamado ao final do bloco,
            // fechando navegador, contexto e instância do Playwright corretamente.
            await using var servicoPlaywright = new EmissaoNFPlaywright();

            // 1. Inicializa o navegador com interface visível (headless: false) para você
            // poder acompanhar ou fazer o login/navegar até a tela da nota fiscal.
            await servicoPlaywright.IniciarNavegadorAsync(headless: false);

            // TODO: Navegue até a página da nota fiscal (via FazerLoginAsync ou manualmente na janela aberta)
            // e deixe a tela de visualização carregada antes de gerar o PDF.
            Console.WriteLine("Abra/navegue até a tela da nota no navegador e pressione ENTER para gerar o PDF...");
            Console.ReadLine();

            // 2. Gera o PDF a partir da visualização carregada no navegador.
            var caminhoGerado = await servicoPlaywright.GerarPdfVisualizacaoAsync(caminhoPdfTeste);

            Console.WriteLine($"PDF de teste gerado em: {caminhoGerado}");
        }
    }
}