using FluentValidation;
using LayoutValidator.Core;

namespace LayoutValidator.LayoutFuncionario;

public sealed class FuncionarioValidadorLayout : ValidadorLayoutBase<FuncionarioRaw, Funcionario>
{
    public FuncionarioValidadorLayout(IValidator<FuncionarioRaw> validador, ILayoutMapper<FuncionarioRaw, Funcionario> mapper)
        : base(validador, mapper)
    {
    }

    // Sem sobrescrever Opcoes: o arquivo deste layout é o padrão — delimitador ';' com cabeçalho.
}
