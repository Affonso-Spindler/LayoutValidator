using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasNumericasExtensions.</summary>
internal static class RegrasNumericasCatalogo
{
    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Inteiro",
            "InteiroInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.InteiroValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número inteiro.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "InteiroPositivo",
            "InteiroPositivoInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.InteiroEntre(valor, 1, long.MaxValue),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número inteiro positivo.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "InteiroNaoNegativo",
            "InteiroNaoNegativoInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.InteiroEntre(valor, 0, long.MaxValue),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número inteiro maior ou igual a zero.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "InteiroEntre",
            "InteiroForaDoIntervalo",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Inteiro, true),
                new ParametroEsperado("maximo", TipoParametro.Inteiro, true)
            },
            (valor, p) => Formatos.InteiroEntre(valor, p.ObterInteiro("minimo"), p.ObterInteiro("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um inteiro entre {p.ObterInteiro("minimo")} e {p.ObterInteiro("maximo")}.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Decimal",
            "DecimalInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.DecimalValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número decimal (vírgula como separador decimal).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "DecimalPositivo",
            "DecimalPositivoInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.DecimalMaiorQue(valor, decimal.Zero),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número decimal positivo.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "DecimalEntre",
            "DecimalForaDoIntervalo",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Decimal, true),
                new ParametroEsperado("maximo", TipoParametro.Decimal, true)
            },
            (valor, p) => Formatos.DecimalEntre(valor, p.ObterDecimal("minimo"), p.ObterDecimal("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um decimal entre {p.ObterDecimal("minimo")} e {p.ObterDecimal("maximo")}.");
    }
}
