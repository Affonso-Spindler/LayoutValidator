using FluentValidation;
using FluentValidation.Results;

namespace LayoutValidator.Regras.Tests.Extensions;

/// <summary>Registro mínimo pra exercitar uma regra isolada.</summary>
public sealed class RegistroTeste
{
    public string Valor { get; set; } = string.Empty;

    public string Outro { get; set; } = string.Empty;
}

/// <summary>Valida só a propriedade <c>Valor</c>, com a regra que o teste passar.</summary>
public sealed class ValidadorDeUmaRegra : AbstractValidator<RegistroTeste>
{
    public ValidadorDeUmaRegra(Action<IRuleBuilder<RegistroTeste, string>> configurar) =>
        configurar(RuleFor(registro => registro.Valor));
}

public static class Exercitar
{
    public static IList<ValidationFailure> Regra(Action<IRuleBuilder<RegistroTeste, string>> configurar, string valor) =>
        new ValidadorDeUmaRegra(configurar).Validate(new RegistroTeste { Valor = valor }).Errors;

    public static string? CodigoDoUnicoErro(Action<IRuleBuilder<RegistroTeste, string>> configurar, string valor)
    {
        var erros = Regra(configurar, valor);
        return erros.Count == 1 ? erros[0].ErrorCode : null;
    }
}
