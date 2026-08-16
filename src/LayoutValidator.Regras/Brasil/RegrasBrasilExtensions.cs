using System.Text.RegularExpressions;
using FluentValidation;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras;

public static class RegrasBrasilExtensions
{
    // 00000-000 ou 00000000 — o hífen do CEP é forma canônica escrita, então as duas passam.
    private static readonly Regex PadraoCep = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);

    // (00) 0000-0000, (00) 00000-0000, ou só os 10/11 dígitos.
    private static readonly Regex PadraoTelefone = new(@"^(\(\d{2}\) \d{4,5}-\d{4}|\d{10,11})$", RegexOptions.Compiled);

    /// <summary>CPF com dígito verificador correto, 11 dígitos sem máscara.</summary>
    public static IRuleBuilderOptions<T, string> Cpf<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.CpfValido,
            "CpfInvalido",
            "'{PropertyName}' não é um CPF válido (11 dígitos, sem máscara, com dígito verificador correto).");

    /// <summary>CNPJ com dígito verificador correto, 14 dígitos sem máscara.</summary>
    public static IRuleBuilderOptions<T, string> Cnpj<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.CnpjValido,
            "CnpjInvalido",
            "'{PropertyName}' não é um CNPJ válido (14 dígitos, sem máscara, com dígito verificador correto).");

    /// <summary>Aceita CPF ou CNPJ — útil em layout com coluna única de documento.</summary>
    public static IRuleBuilderOptions<T, string> CpfOuCnpj<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.CpfOuCnpjValido,
            "CpfOuCnpjInvalido",
            "'{PropertyName}' não é um CPF nem um CNPJ válido.");

    public static IRuleBuilderOptions<T, string> Cep<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            PadraoCep.IsMatch,
            "CepInvalido",
            "'{PropertyName}' deve ser um CEP no formato 00000-000 ou 00000000.");

    /// <summary>Sigla de unidade federativa — as 27 reais, ignorando caixa. "CC" reprova.</summary>
    public static IRuleBuilderOptions<T, string> Uf<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            UnidadesFederativas.Valida,
            "UfInvalida",
            "'{PropertyName}' deve ser a sigla de uma unidade federativa brasileira.");

    public static IRuleBuilderOptions<T, string> Telefone<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            PadraoTelefone.IsMatch,
            "TelefoneInvalido",
            "'{PropertyName}' deve ser um telefone no formato (00) 00000-0000 ou só os dígitos.");

    public static IRuleBuilderOptions<T, string> Cnh<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.CnhValida,
            "CnhInvalida",
            "'{PropertyName}' não é uma CNH válida (11 dígitos com dígito verificador correto).");

    public static IRuleBuilderOptions<T, string> PisPasep<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.PisPasepValido,
            "PisPasepInvalido",
            "'{PropertyName}' não é um PIS/PASEP válido (11 dígitos com dígito verificador correto).");
}
