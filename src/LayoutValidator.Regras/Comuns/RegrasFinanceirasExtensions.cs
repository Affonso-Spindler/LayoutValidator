using FluentValidation;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras;

public static class RegrasFinanceirasExtensions
{
    /// <summary>
    /// Valor monetário com casas decimais exatas e sem separador de milhar: "1234,56".
    /// Mais estrito que <c>Decimal()</c> de propósito — é o formato que arquivo de carga
    /// costuma exigir.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Moeda<T>(this IRuleBuilder<T, string> regra, int casasDecimais = 2) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.MoedaValida(valor, casasDecimais),
            "MoedaInvalida",
            $"'{{PropertyName}}' deve estar no formato monetário com {casasDecimais} casas decimais (ex: 1234,56).");

    /// <summary>Decimal entre 0 e 100, no padrão brasileiro.</summary>
    public static IRuleBuilderOptions<T, string> Percentual<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DecimalEntre(valor, 0m, 100m),
            "PercentualInvalido",
            "'{PropertyName}' deve ser um percentual entre 0 e 100.");

    /// <summary>Número de cartão pelo algoritmo de Luhn — 13 a 19 dígitos, sem máscara.</summary>
    public static IRuleBuilderOptions<T, string> CartaoDeCredito<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            Documentos.LuhnValido,
            "CartaoDeCreditoInvalido",
            "'{PropertyName}' não é um número de cartão válido.");
}
