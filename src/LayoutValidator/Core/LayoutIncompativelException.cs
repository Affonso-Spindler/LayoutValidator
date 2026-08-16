namespace LayoutValidator.Core;

/// <summary>
/// O arquivo não é do formato que o layout declara — quase sempre delimitador errado.
///
/// É exceção, e não <see cref="RegistroInvalido{T}"/>, porque o arquivo inteiro é
/// inaproveitável: emitir um registro inválido por linha só produziria ruído. Como
/// <c>Validar</c> é um iterador preguiçoso, ela sobe na primeira iteração do <c>foreach</c>,
/// não na chamada.
/// </summary>
public sealed class LayoutIncompativelException : Exception
{
    private const int TamanhoMaximoDoTrecho = 120;

    private LayoutIncompativelException(string mensagem, string delimitadorEsperado, IReadOnlyList<string> primeiraLinha)
        : base(mensagem)
    {
        DelimitadorEsperado = delimitadorEsperado;
        PrimeiraLinha = primeiraLinha;
    }

    public string DelimitadorEsperado { get; }

    /// <summary>A linha que o parser leu — cabeçalho ou primeiro registro, conforme o modo.</summary>
    public IReadOnlyList<string> PrimeiraLinha { get; }

    internal static LayoutIncompativelException NenhumaColunaCasou(
        string[] cabecalho,
        string delimitador,
        string nomeDoLayout) =>
        new(
            $"Nenhuma coluna do cabeçalho casou com o layout '{nomeDoLayout}'. " +
            $"Delimitador esperado: '{delimitador}'. " +
            $"Cabeçalho lido em {cabecalho.Length} coluna(s): {Resumir(cabecalho)}. " +
            "Confira se o arquivo usa outro delimitador.",
            delimitador,
            cabecalho);

    internal static LayoutIncompativelException ColunaUnicaInesperada(
        string[] primeiraLinha,
        string delimitador,
        string nomeDoLayout,
        int colunasEsperadas) =>
        new(
            $"A primeira linha veio com 1 coluna, mas o layout '{nomeDoLayout}' espera {colunasEsperadas}. " +
            $"Delimitador esperado: '{delimitador}'. " +
            $"Linha lida: {Resumir(primeiraLinha)}. " +
            "Confira se o arquivo usa outro delimitador.",
            delimitador,
            primeiraLinha);

    private static string Resumir(string[] campos)
    {
        var texto = string.Join(" | ", campos);

        if (texto.Length > TamanhoMaximoDoTrecho)
            texto = texto[..TamanhoMaximoDoTrecho] + "...";

        return $"'{texto}'";
    }
}
