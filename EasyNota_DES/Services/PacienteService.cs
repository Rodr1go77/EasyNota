using EasyNota_DES.Data;
using EasyNota_DES.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyNota_DES.Services
{
    public class PacienteService
    {
        private readonly EasyNotaDbContext _context;

        public PacienteService(EasyNotaDbContext context)
        {
            _context = context;
        }

        //Método para Obter todos os clientes
        public async Task<List<Paciente>> ObterTodosPacientesAsync()
        {
            return await _context.Pacientes
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }


        //Método para Cadastrar Clientes
        public async Task<(bool Sucesso, string Mensagem, Paciente? Paciente)> CadastrarPacienteAsync(Paciente paciente)
        {
            if (string.IsNullOrWhiteSpace(paciente.Nome) || string.IsNullOrWhiteSpace(paciente.Cpf))
            {
                return (false, "Nome e CPF são obrigatórios/", null);
            }

            if (string.IsNullOrWhiteSpace(paciente.Id))
            {
                paciente.Id = Guid.NewGuid().ToString();
            }

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            return (true, "Paciente cadastrado com sucesso.", paciente);


        }

        //Método para Obter cliente pelo Id
        public async Task<Paciente?> ObterPacientePorIdAsync(string id)
        {
            return await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        //Método para atualizar dados dos pacientes
        public async Task<(bool Sucesso, string Mensagem, Paciente? Paciente)> AtualizarDadosPaciente(Paciente pacienteAtualizado)
        {

            if (string.IsNullOrWhiteSpace(pacienteAtualizado.Id))
            {
                return (false, "O identificador do paciente é obrigatório", null);
            }

            if (string.IsNullOrWhiteSpace(pacienteAtualizado.Nome) || string.IsNullOrWhiteSpace(pacienteAtualizado.Cpf))
            {
                return (false, "Nome e cpf são obrigatórios.", null);
            }

            var existente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteAtualizado.Id);

            if (existente is null)
            {
                return (false, "Paciente não encontrado.", null);
            }

            existente.Nome = pacienteAtualizado.Nome;
            existente.Cpf = pacienteAtualizado.Cpf;
            existente.Telefone = pacienteAtualizado.Telefone;
            existente.Email = pacienteAtualizado.Email;
            existente.Cep = pacienteAtualizado.Cep;
            existente.NumeroLogradouro = pacienteAtualizado.NumeroLogradouro;
            existente.Complemento = pacienteAtualizado.Complemento;

            await _context.SaveChangesAsync();
            return (true, "Dados do paciente atualizados com sucesso.", existente);

        }

        //Método para excluir pacientes
        public async Task<(bool Sucesso, string Mensagem)> ExcluirPacienteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return (false, "Identificador do paciente é obrigatório.");
            }

            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente is null)
            {
                return (false, "Paciente não encontrado na base de dados.");
            }

            var temConsultasVinculadas = await _context.Consultas
                .AnyAsync(c => c.PacienteId == id);

            if (temConsultasVinculadas)
            {
                return (false, "Não é possível excluir o paciente pois existem consultas vinculadas.");
            }

            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();

            return (true, "Paciente excluído com sucesso.");


        }

    }


}

