using System.Globalization;
using LayoutValidator.Core;

namespace LayoutValidator.Tests.Modelos;

public sealed class RegistroTesteMapper : ILayoutMapper<RegistroRawTeste, RegistroTeste>
{
    public RegistroTeste Map(RegistroRawTeste raw) => new()
    {
        Nome = raw.Nome,
        Idade = int.Parse(raw.Idade),
        DataNascimento = DateTime.ParseExact(raw.DataNascimento, "dd/MM/yyyy", CultureInfo.InvariantCulture)
    };
}
