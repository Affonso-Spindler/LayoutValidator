using System.Text.RegularExpressions;
using FluentValidation;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras;

public static class RegrasTextoExtensions
{
    /// <summary>
    /// Única regra do catálogo que reprova valor vazio — todas as outras deixam passar de
    /// propósito. Valor só com espaços conta como vazio.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Obrigatorio<T>(this IRuleBuilder<T, string> regra) =>
        regra.Must(valor => !string.IsNullOrWhiteSpace(valor))
            .WithErrorCode("CampoObrigatorio")
            .WithMessage("'{PropertyName}' é obrigatório.");

    public static IRuleBuilderOptions<T, string> ComprimentoEntre<T>(this IRuleBuilder<T, string> regra, int minimo, int maximo) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.ComprimentoEntre(valor, minimo, maximo),
            "ComprimentoInvalido",
            $"'{{PropertyName}}' deve ter entre {minimo} e {maximo} caracteres.");

    public static IRuleBuilderOptions<T, string> ComprimentoMaximo<T>(this IRuleBuilder<T, string> regra, int maximo) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.ComprimentoEntre(valor, 0, maximo),
            "ComprimentoInvalido",
            $"'{{PropertyName}}' deve ter no máximo {maximo} caracteres.");

    public static IRuleBuilderOptions<T, string> ComprimentoExato<T>(this IRuleBuilder<T, string> regra, int comprimento) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.ComprimentoEntre(valor, comprimento, comprimento),
            "ComprimentoInvalido",
            $"'{{PropertyName}}' deve ter exatamente {comprimento} caracteres.");

    public static IRuleBuilderOptions<T, string> SomenteDigitos<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Formatos.SomenteDigitos,
            "SomenteDigitosInvalido",
            "'{PropertyName}' deve conter somente dígitos.");

    /// <summary>Domínio fechado de valores aceitos, ignorando caixa — ex: <c>ValorEm("S", "N")</c>.</summary>
    public static IRuleBuilderOptions<T, string> ValorEm<T>(this IRuleBuilder<T, string> regra, params string[] aceitos)
    {
        var dominio = new HashSet<string>(aceitos, StringComparer.OrdinalIgnoreCase);

        return ConstrutorRegra.DeFormato(
            regra,
            dominio.Contains,
            "ValorForaDoDominio",
            $"'{{PropertyName}}' deve ser um destes valores: {string.Join(", ", aceitos)}.");
    }

    /// <summary>
    /// Saída de emergência para a regra pontual que não vale virar entrada de catálogo.
    /// O código de erro é seu — mantenha estável, é ele que o <c>ResumoValidacaoLayout</c> agrupa.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Formato<T>(
        this IRuleBuilder<T, string> regra,
        string expressaoRegular,
        string codigoErro,
        string mensagem)
    {
        var padrao = new Regex(expressaoRegular, RegexOptions.Compiled);

        return ConstrutorRegra.DeFormato(regra, padrao.IsMatch, codigoErro, mensagem);
    }
}
