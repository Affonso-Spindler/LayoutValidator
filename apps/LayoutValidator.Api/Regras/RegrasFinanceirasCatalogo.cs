using System.Text.Json;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasFinanceirasExtensions.</summary>
internal static class RegrasFinanceirasCatalogo
{
    private const int CasasDecimaisPadrao = 2;

    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Moeda",
            "MoedaInvalida",
            new[] { new ParametroEsperado("casasDecimais", TipoParametro.Inteiro, false) },
            (valor, p) => Formatos.MoedaValida(valor, ObterCasasDecimais(p)),
            (nomeCampo, p) => $"'{nomeCampo}' deve estar no formato monetário com " +
                              $"{ObterCasasDecimais(p)} casas decimais (ex: 1234,56).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Percentual",
            "PercentualInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.DecimalEntre(valor, 0m, 100m),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um percentual entre 0 e 100.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "CartaoDeCredito",
            "CartaoDeCreditoInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.LuhnValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um número de cartão válido.");
    }

    /// <summary>
    /// "casasDecimais" é opcional — cai pro padrão quando ausente, em branco ou de tipo errado.
    /// A tela manda a chave mesmo quando o usuário não preenche (vira <c>null</c> no JSON), então
    /// não dá pra assumir que "existe a propriedade" significa "tem um inteiro aqui".
    /// </summary>
    private static int ObterCasasDecimais(JsonElement? parametros)
    {
        if (parametros is not { ValueKind: JsonValueKind.Object } objeto
            || !objeto.TryGetProperty("casasDecimais", out var casasDecimais)
            || casasDecimais.ValueKind != JsonValueKind.Number
            || !casasDecimais.TryGetInt32(out var valor))
        {
            return CasasDecimaisPadrao;
        }

        return valor;
    }
}
