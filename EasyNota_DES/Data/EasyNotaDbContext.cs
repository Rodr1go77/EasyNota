using EasyNota_DES.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES.Data
{
    public class EasyNotaDbContext : DbContext
    {
        public EasyNotaDbContext(DbContextOptions<EasyNotaDbContext> options)

            : base(options)
        {

        }

        public DbSet<Paciente> Pacientes => Set<Paciente>();

        public DbSet<Consulta> Consultas => Set<Consulta>();

        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

        public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();


    }
}
