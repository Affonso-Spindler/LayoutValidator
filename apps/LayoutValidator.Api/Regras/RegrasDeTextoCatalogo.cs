using System.Text.RegularExpressions;
using LayoutValidator.Regras.Predicados;

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
            (valor, p) => Formatos.ComprimentoEntre(valor, (int)p.ObterInteiro("minimo"), (int)p.ObterInteiro("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter entre {p.ObterInteiro("minimo")} e {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoMaximo",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("maximo", TipoParametro.Inteiro, true) },
            (valor, p) => Formatos.ComprimentoEntre(valor, 0, (int)p.ObterInteiro("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter no máximo {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoExato",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("comprimento", TipoParametro.Inteiro, true) },
            (valor, p) =>
            {
                var comprimento = (int)p.ObterInteiro("comprimento");
                return Formatos.ComprimentoEntre(valor, comprimento, comprimento);
            },
            (nomeCampo, p) => $"'{nomeCampo}' deve ter exatamente {p.ObterInteiro("comprimento")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "SomenteDigitos",
            "SomenteDigitosInvalido",
            Array.Empty<ParametroEsperado>(),
            // Reusa o predicado de LayoutValidator.Regras (só dígitos ASCII '0'-'9') em vez de
            // char.IsDigit, que aceita qualquer dígito Unicode (ex.: arábico-índico, fullwidth) —
            // sem isso, essa chave dava respostas diferentes dependendo de qual catálogo avaliava.
            (valor, _) => Formatos.SomenteDigitos(valor),
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
            // A expressão regular vem de dado persistido no cadastro, não da requisição —
            // um padrão catastrófico (ex.: "^(a+)+$") ficaria preso indefinidamente sem esse
            // timeout, travando a thread da requisição sem chance de cancelamento (ReDoS).
            // Timeout estourado é tratado como "não bate com o padrão", não como erro 500.
            Avaliar = (valor, p) =>
            {
                if (string.IsNullOrWhiteSpace(valor))
                    return true;

                try
                {
                    return Regex.IsMatch(valor, p.ObterTexto("expressaoRegular"), RegexOptions.None, TimeSpan.FromMilliseconds(200));
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
            },
            ObterCodigoErro = p => p.ObterTexto("codigoErro"),
            MontarMensagem = (nomeCampo, p) => p.ObterTexto("mensagem").Replace("{PropertyName}", nomeCampo)
        };
    }
}
