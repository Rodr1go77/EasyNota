using EasyNota_DES.Data;
using EasyNota_DES.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Adiciona servicos ao container.
            builder.Services.AddRazorPages();

            //Registro de Contexto e Services no container
            builder.Services.AddDbContext<EasyNotaDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<PacienteService>();
            builder.Services.AddScoped<ConsultaService>();
            builder.Services.AddScoped<PagamentoService>();
            builder.Services.AddScoped<NotaFiscalService>();
            builder.Services.AddScoped<OrganizadorNFsService>();
            builder.Services.AddScoped<EmissaoNFPlaywright>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
