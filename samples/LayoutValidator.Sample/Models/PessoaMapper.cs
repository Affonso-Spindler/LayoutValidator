using System.Globalization;
using LayoutValidator.Core;

namespace LayoutValidator.Sample.Models;

public sealed class PessoaMapper : ILayoutMapper<PessoaRaw, Pessoa>
{
    public Pessoa Map(PessoaRaw raw) => new()
    {
        Nome = raw.Nome,
        Idade = int.Parse(raw.Idade),
        DataNascimento = DateTime.ParseExact(raw.DataNascimento, "dd/MM/yyyy", CultureInfo.InvariantCulture),
        Cpf = raw.Cpf
    };
}
