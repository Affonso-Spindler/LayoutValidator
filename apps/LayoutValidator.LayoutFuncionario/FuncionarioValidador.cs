using System.Globalization;
using FluentValidation;

namespace LayoutValidator.LayoutFuncionario;

public sealed class FuncionarioValidador : AbstractValidator<FuncionarioRaw>
{
    private static readonly HashSet<string> UfsValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
        "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public FuncionarioValidador()
    {
        RuleFor(f => f.MatriculaId)
            .Must(valor => int.TryParse(valor, out var n) && n > 0)
            .WithErrorCode("MatriculaIdDeveSerInteiroPositivo")
            .WithMessage("Matrícula deve ser um número inteiro positivo.");

        RuleFor(f => f.Nome)
            .NotEmpty()
            .WithErrorCode("NomeObrigatorio")
            .WithMessage("Nome é obrigatório.");

        RuleFor(f => f.Cpf)
            .Matches(@"^\d{11}$")
            .WithErrorCode("CpfFormatoInvalido")
            .WithMessage("CPF deve conter exatamente 11 dígitos numéricos.");

        RuleFor(f => f.Rg)
            .Matches(@"^\d{7,9}$")
            .WithErrorCode("RgFormatoInvalido")
            .WithMessage("RG deve conter entre 7 e 9 dígitos numéricos.");

        RuleFor(f => f.DataNascimento)
            .Must(SerDataValida)
            .WithErrorCode("DataNascimentoFormatoInvalido")
            .WithMessage("Data de nascimento deve estar no formato dd/MM/yyyy.");

        RuleFor(f => f.Email)
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithErrorCode("EmailFormatoInvalido")
            .WithMessage("E-mail em formato inválido.");

        RuleFor(f => f.Telefone)
            .Matches(@"^\(\d{2}\) \d{4,5}-\d{4}$")
            .WithErrorCode("TelefoneFormatoInvalido")
            .WithMessage("Telefone deve estar no formato (00) 00000-0000.");

        RuleFor(f => f.Cargo)
            .NotEmpty()
            .WithErrorCode("CargoObrigatorio")
            .WithMessage("Cargo é obrigatório.");

        RuleFor(f => f.Departamento)
            .NotEmpty()
            .WithErrorCode("DepartamentoObrigatorio")
            .WithMessage("Departamento é obrigatório.");

        RuleFor(f => f.Salario)
            .Matches(@"^\d+,\d{2}$")
            .WithErrorCode("SalarioFormatoInvalido")
            .WithMessage("Salário deve estar no formato 0000,00 (vírgula decimal).");

        RuleFor(f => f.DataAdmissao)
            .Must(SerDataValida)
            .WithErrorCode("DataAdmissaoFormatoInvalido")
            .WithMessage("Data de admissão deve estar no formato dd/MM/yyyy.");

        RuleFor(f => f.DataDemissao)
            .Must(valor => string.IsNullOrEmpty(valor) || SerDataValida(valor))
            .WithErrorCode("DataDemissaoFormatoInvalido")
            .WithMessage("Data de demissão, quando preenchida, deve estar no formato dd/MM/yyyy.");

        RuleFor(f => f.Ativo)
            .Must(valor => valor is "S" or "N")
            .WithErrorCode("AtivoDeveSerSouN")
            .WithMessage("Campo Ativo deve ser 'S' ou 'N'.");

        RuleFor(f => f.Cep)
            .Matches(@"^\d{5}-\d{3}$")
            .WithErrorCode("CepFormatoInvalido")
            .WithMessage("CEP deve estar no formato 00000-000.");

        RuleFor(f => f.Endereco)
            .NotEmpty()
            .WithErrorCode("EnderecoObrigatorio")
            .WithMessage("Endereço é obrigatório.");

        RuleFor(f => f.NumeroEndereco)
            .Must(valor => int.TryParse(valor, out _))
            .WithErrorCode("NumeroEnderecoDeveSerInteiro")
            .WithMessage("Número do endereço deve ser um valor inteiro.");

        RuleFor(f => f.Bairro)
            .NotEmpty()
            .WithErrorCode("BairroObrigatorio")
            .WithMessage("Bairro é obrigatório.");

        RuleFor(f => f.Cidade)
            .NotEmpty()
            .WithErrorCode("CidadeObrigatoria")
            .WithMessage("Cidade é obrigatória.");

        RuleFor(f => f.Uf)
            .Must(valor => UfsValidas.Contains(valor))
            .WithErrorCode("UfInvalida")
            .WithMessage("UF deve ser uma sigla de estado brasileiro válida.");

        RuleFor(f => f.CargaHoraria)
            .Must(valor => int.TryParse(valor, out var n) && n is > 0 and <= 60)
            .WithErrorCode("CargaHorariaForaDoIntervalo")
            .WithMessage("Carga horária deve ser um inteiro entre 1 e 60.");

        RuleFor(f => f.PercentualComissao)
            .Must(SerPercentualValido)
            .WithErrorCode("PercentualComissaoFormatoInvalido")
            .WithMessage("Percentual de comissão, quando preenchido, deve estar no formato 00,00 entre 0 e 100.");
    }

    private static bool SerDataValida(string valor) =>
        DateTime.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool SerPercentualValido(string valor)
    {
        if (string.IsNullOrEmpty(valor))
            return true;

        if (!decimal.TryParse(valor.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
            return false;

        return numero is >= 0 and <= 100;
    }
}
