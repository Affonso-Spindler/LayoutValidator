using System.Globalization;
using FluentValidation;

namespace LayoutValidator.Sample.Models;

public sealed class PessoaValidador : AbstractValidator<PessoaRaw>
{
    public PessoaValidador()
    {
        RuleFor(p => p.Nome)
            .NotEmpty()
            .WithErrorCode("NomeObrigatorio")
            .WithMessage("Nome é obrigatório.");

        RuleFor(p => p.Idade)
            .Must(SerInteiroValido)
            .WithErrorCode("IdadeDeveSerInteiro")
            .WithMessage("Idade deve ser um número inteiro.");

        RuleFor(p => p.DataNascimento)
            .Must(SerDataValida)
            .WithErrorCode("DataNascimentoFormatoInvalido")
            .WithMessage("Data de nascimento deve estar no formato dd/MM/yyyy.");

        RuleFor(p => p.Cpf)
            .Matches(@"^\d{11}$")
            .WithErrorCode("CpfFormatoInvalido")
            .WithMessage("CPF deve conter exatamente 11 dígitos numéricos.");
    }

    private static bool SerInteiroValido(string valor) => int.TryParse(valor, out _);

    private static bool SerDataValida(string valor) =>
        DateTime.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
