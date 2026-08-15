using System.Globalization;
using FluentValidation;

namespace LayoutValidator.Tests.Modelos;

public sealed class ValidadorRegistroTeste : AbstractValidator<RegistroRawTeste>
{
    public ValidadorRegistroTeste()
    {
        RuleFor(r => r.Nome)
            .NotEmpty()
            .WithErrorCode("NomeObrigatorio");

        RuleFor(r => r.Idade)
            .Must(SerInteiroValido)
            .WithErrorCode("IdadeDeveSerInteiro")
            .WithMessage("Idade deve ser um número inteiro.");

        RuleFor(r => r.DataNascimento)
            .Must(SerDataValida)
            .WithErrorCode("DataNascimentoFormatoInvalido")
            .WithMessage("Data de nascimento deve estar no formato dd/MM/yyyy.");
    }

    private static bool SerInteiroValido(string valor) => int.TryParse(valor, out _);

    private static bool SerDataValida(string valor) =>
        DateTime.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
