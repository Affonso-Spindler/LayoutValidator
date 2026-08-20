using System.Text.RegularExpressions;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasTextoExtensions.</summary>
internal static class RegrasDeTextoCatalogo
{
    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return new RegraCadastrada
        {
            Chave = "Obrigatorio",
            ParametrosEsperados = Array.Empty<ParametroEsperado>(),
            Avaliar = (valor, _) => !string.IsNullOrWhiteSpace(valor),
            ObterCodigoErro = _ => "CampoObrigatorio",
            MontarMensagem = (nomeCampo, _) => $"'{nomeCampo}' é obrigatório."
        };

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoEntre",
            "ComprimentoInvalido",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Inteiro, true),
                new ParametroEsperado("maximo", TipoParametro.Inteiro, true)
            },
            (valor, p) => (valor?.Length ?? 0) >= p.ObterInteiro("minimo") && (valor?.Length ?? 0) <= p.ObterInteiro("maximo"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter entre {p.ObterInteiro("minimo")} e {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoMaximo",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("maximo", TipoParametro.Inteiro, true) },
            (valor, p) => (valor?.Length ?? 0) <= p.ObterInteiro("maximo"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter no máximo {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoExato",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("comprimento", TipoParametro.Inteiro, true) },
            (valor, p) => (valor?.Length ?? 0) == p.ObterInteiro("comprimento"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter exatamente {p.ObterInteiro("comprimento")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "SomenteDigitos",
            "SomenteDigitosInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => valor.Length > 0 && valor.All(char.IsDigit),
            (nomeCampo, _) => $"'{nomeCampo}' deve conter somente dígitos.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ValorEm",
            "ValorForaDoDominio",
            new[] { new ParametroEsperado("valores", TipoParametro.ListaDeTexto, true) },
            (valor, p) => p.ObterListaDeTexto("valores").Contains(valor, StringComparer.OrdinalIgnoreCase),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um destes valores: {string.Join(", ", p.ObterListaDeTexto("valores"))}.");

        yield return new RegraCadastrada
        {
            Chave = "Formato",
            ParametrosEsperados = new[]
            {
                new ParametroEsperado("expressaoRegular", TipoParametro.Texto, true),
                new ParametroEsperado("codigoErro", TipoParametro.Texto, true),
                new ParametroEsperado("mensagem", TipoParametro.Texto, true)
            },
            Avaliar = (valor, p) => string.IsNullOrWhiteSpace(valor)
                || Regex.IsMatch(valor, p.ObterTexto("expressaoRegular")),
            ObterCodigoErro = p => p.ObterTexto("codigoErro"),
            MontarMensagem = (nomeCampo, p) => p.ObterTexto("mensagem").Replace("{PropertyName}", nomeCampo)
        };
    }
}
