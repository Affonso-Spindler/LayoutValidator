using FluentValidation;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras;

public static class RegrasNumericasExtensions
{
    public static IRuleBuilderOptions<T, string> Inteiro<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Formatos.InteiroValido,
            "InteiroInvalido",
            "'{PropertyName}' deve ser um número inteiro.");

    public static IRuleBuilderOptions<T, string> InteiroPositivo<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.InteiroEntre(valor, 1, long.MaxValue),
            "InteiroPositivoInvalido",
            "'{PropertyName}' deve ser um número inteiro positivo.");

    public static IRuleBuilderOptions<T, string> InteiroNaoNegativo<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.InteiroEntre(valor, 0, long.MaxValue),
            "InteiroNaoNegativoInvalido",
            "'{PropertyName}' deve ser um número inteiro maior ou igual a zero.");

    public static IRuleBuilderOptions<T, string> InteiroEntre<T>(this IRuleBuilder<T, string> regra, long minimo, long maximo) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.InteiroEntre(valor, minimo, maximo),
            "InteiroForaDoIntervalo",
            $"'{{PropertyName}}' deve ser um inteiro entre {minimo} e {maximo}.");

    /// <summary>Decimal no padrão brasileiro: vírgula decimal, ponto opcional como separador de milhar.</summary>
    public static IRuleBuilderOptions<T, string> Decimal<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Formatos.DecimalValido,
            "DecimalInvalido",
            "'{PropertyName}' deve ser um número decimal (vírgula como separador decimal).");

    public static IRuleBuilderOptions<T, string> DecimalPositivo<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DecimalMaiorQue(valor, decimal.Zero),
            "DecimalPositivoInvalido",
            "'{PropertyName}' deve ser um número decimal positivo.");

    public static IRuleBuilderOptions<T, string> DecimalEntre<T>(this IRuleBuilder<T, string> regra, decimal minimo, decimal maximo) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DecimalEntre(valor, minimo, maximo),
            "DecimalForaDoIntervalo",
            $"'{{PropertyName}}' deve ser um decimal entre {minimo} e {maximo}.");
}
