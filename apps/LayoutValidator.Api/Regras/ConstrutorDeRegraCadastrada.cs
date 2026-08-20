using System.Text.Json;

namespace LayoutValidator.Api.Regras;

/// <summary>
/// Equivalente dinâmico de LayoutValidator.Regras.ConstrutorRegra: costura um predicado com
/// código de erro e mensagem, aplicando o mesmo contrato do catálogo estático — regra de
/// formato nunca reprova valor vazio. Obrigatoriedade é a única exceção e não passa por aqui.
/// </summary>
internal static class ConstrutorDeRegraCadastrada
{
    public static RegraCadastrada DeFormato(
        string chave,
        string codigoErro,
        IReadOnlyList<ParametroEsperado> parametrosEsperados,
        Func<string, JsonElement?, bool> predicado,
        Func<string, JsonElement?, string> montarMensagem) =>
        new()
        {
            Chave = chave,
            ParametrosEsperados = parametrosEsperados,
            Avaliar = (valor, parametros) => string.IsNullOrWhiteSpace(valor) || predicado(valor, parametros),
            ObterCodigoErro = _ => codigoErro,
            MontarMensagem = montarMensagem
        };
}
