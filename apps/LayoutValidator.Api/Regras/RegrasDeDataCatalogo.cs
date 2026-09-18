using System.Globalization;
using System.Text.Json;
using LayoutValidator.Regras;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasDataExtensions.</summary>
internal static class RegrasDeDataCatalogo
{
    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Data",
            "DataInvalida",
            new[] { new ParametroEsperado("formato", TipoParametro.Texto, false) },
            (valor, p) => Formatos.DataValida(valor, ObterFormato(p)),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser uma data válida no formato {ObterFormato(p)}.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "DataEntre",
            "DataForaDoIntervalo",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Texto, true),
                new ParametroEsperado("maximo", TipoParametro.Texto, true),
                new ParametroEsperado("formato", TipoParametro.Texto, false)
            },
            (valor, p) =>
            {
                var formato = ObterFormato(p);

                // minimo/maximo não passam pela checagem de cadastro (só parâmetro obrigatório
                // tem tipo checado ali) — se vierem numa data que não bate com "formato", trata
                // como "nunca aderente" em vez de deixar o TryParseExact explodir pra quem chama.
                if (!DateTime.TryParseExact(p.ObterTexto("minimo"), formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var minimo)
                    || !DateTime.TryParseExact(p.ObterTexto("maximo"), formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var maximo))
                {
                    return false;
                }

                return Formatos.DataEntre(valor, minimo, maximo, formato);
            },
            (nomeCampo, p) => $"'{nomeCampo}' deve ser uma data entre {p.ObterTexto("minimo")} e {p.ObterTexto("maximo")} (formato {ObterFormato(p)}).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "DataNoPassado",
            "DataNoFuturo",
            new[] { new ParametroEsperado("formato", TipoParametro.Texto, false) },
            (valor, p) => Formatos.DataNoPassado(valor, ObterFormato(p)),
            (nomeCampo, _) => $"'{nomeCampo}' não pode ser uma data futura.");
    }

    /// <summary>"formato" é opcional — cai pro padrão brasileiro se ausente ou em branco.</summary>
    private static string ObterFormato(JsonElement? parametros)
    {
        if (!parametros.TemPropriedade("formato"))
            return RegrasDataExtensions.FormatoBrasileiro;

        var formato = parametros.ObterTexto("formato");
        return string.IsNullOrWhiteSpace(formato) ? RegrasDataExtensions.FormatoBrasileiro : formato;
    }
}
