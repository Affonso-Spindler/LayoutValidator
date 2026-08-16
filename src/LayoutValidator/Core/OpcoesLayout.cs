using System.Globalization;
using CsvHelper.Configuration;

namespace LayoutValidator.Core;

/// <summary>
/// O formato do arquivo que um layout descreve — delimitador e tratamento da primeira linha.
///
/// Pertence ao <b>layout</b>, não a quem consome: é característica do arquivo, igual à lista
/// de colunas. Quem consome só chama <c>Validar(leitor)</c>.
/// </summary>
public sealed class OpcoesLayout
{
    /// <summary>
    /// Ponto e vírgula. Arquivo brasileiro usa vírgula como separador decimal, então vírgula
    /// delimitadora colidiria com todo campo de valor e obrigaria o arquivo a vir com aspas.
    /// </summary>
    public const string DelimitadorPadrao = ";";

    public string Delimitador { get; set; } = DelimitadorPadrao;

    public ModoCabecalho Cabecalho { get; set; } = ModoCabecalho.Presente;

    /// <summary>
    /// Não existe detecção automática de delimitador de propósito: se o layout é o contrato
    /// do formato, a ferramenta adivinhar contradiz isso — e adivinhar errado num arquivo de
    /// milhões de linhas produz um resultado plausível e errado, pior que uma falha.
    /// </summary>
    public CsvConfiguration ParaConfiguracaoCsv() => new(CultureInfo.InvariantCulture)
    {
        Delimiter = Delimitador,
        HasHeaderRecord = Cabecalho == ModoCabecalho.Presente,

        // Dado ruim ou campo ausente não deve abortar o streaming: cada linha problemática
        // vira um RegistroInvalido.
        BadDataFound = null,
        MissingFieldFound = null
    };
}
