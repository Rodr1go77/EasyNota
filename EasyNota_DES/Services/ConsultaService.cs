using EasyNota_DES.Data;
using EasyNota_DES.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES.Services
{
    public class ConsultaService

    {
        private readonly EasyNotaDbContext _context;

        public ConsultaService(EasyNotaDbContext context)
        {
            _context = context;

        }

        //Método para obter todas as consultas
        public async Task<List<Consulta>> ObterTodasConsultasAsync()
        {
            return await _context.Consultas
                .Include(c => c.Paciente)
                .OrderByDescending(c => c.DataConsulta)
                .ToListAsync();
        }

        //Método para obter as consultas por Id
        public async Task<Consulta?> ObterConsultaPorIdAsync(string id)
        {
            return await _context.Consultas
                .Include(c => c.Paciente)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        //Método para incluir uma nova consulta
        public async Task<(bool Sucesso, string Mensagem, Consulta? Consulta)> RegistrarConsultaAsync(string pacienteId, DateOnly? dataConsulta, decimal valor)
        {
            if (string.IsNullOrWhiteSpace(pacienteId))
            {
                return (false, "O identificador do paciente é obrigatório", null);
            }

            if (valor <= 0m)
            {
                return (false, "O valor da consulta dever ser maior do que zero.", null);
            }

            var pacienteExiste = await _context.Pacientes
                .AnyAsync(p => p.Id == pacienteId);

            if (!pacienteExiste)
            {
                return (false, "Paciente não encontrado", null);
            }

            var consulta = new Consulta
            {
                Id = Guid.NewGuid().ToString(),
                PacienteId = pacienteId,
                DataConsulta = dataConsulta,
                Valor = valor,
                Status = "Pendente"
            };

            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();

            return (true, "Consulta incluída com sucesso.", consulta);

        }

    }

}
