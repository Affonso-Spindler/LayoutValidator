using FluentValidation;
using LayoutValidator.Core;

namespace LayoutValidator.Sample.Models;

public sealed class PessoaValidadorLayout : ValidadorLayoutBase<PessoaRaw, Pessoa>
{
    public PessoaValidadorLayout(IValidator<PessoaRaw> validador, ILayoutMapper<PessoaRaw, Pessoa> mapper)
        : base(validador, mapper)
    {
    }

    // Sem sobrescrever Opcoes: o arquivo deste layout é o padrão — delimitador ';' com cabeçalho.
}
