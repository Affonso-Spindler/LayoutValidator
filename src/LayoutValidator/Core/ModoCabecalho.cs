namespace LayoutValidator.Core;

/// <summary>
/// Como o arquivo do layout trata a primeira linha.
///
/// "Tem cabeçalho" e "ignora a primeira linha" são conceitos diferentes: o primeiro decide
/// <b>como as colunas casam</b> com as propriedades (por nome ou por posição), o segundo só
/// descarta uma linha. Por isso três estados nomeados em vez de um booleano.
/// </summary>
public enum ModoCabecalho
{
    /// <summary>
    /// A primeira linha é cabeçalho e é usada pra casar coluna com propriedade <b>pelo nome</b>.
    /// Ela é consumida antes do laço: não vira registro e não entra no total.
    /// </summary>
    Presente,

    /// <summary>
    /// Não há cabeçalho — a primeira linha já é dado e conta no total. O casamento é
    /// <b>por posição</b>, então a ordem das propriedades do Raw Model é o contrato.
    /// </summary>
    Ausente,

    /// <summary>
    /// Há uma linha de cabeçalho, mas ela é descartada e o casamento é <b>por posição</b>.
    /// Para o arquivo cujo cabeçalho não bate com os nomes das propriedades.
    /// </summary>
    PresenteIgnorado
}
