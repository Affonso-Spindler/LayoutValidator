using CsvHelper;
using LayoutValidator.Core;

namespace LayoutValidator.Api.Validacao;

public static class DivisorDeLinha
{
    public static string[] Dividir(string linha, string delimitador)
    {
        var opcoes = new OpcoesLayout { Delimitador = delimitador, Cabecalho = ModoCabecalho.Ausente };
        var configuracao = opcoes.ParaConfiguracaoCsv();

        using var leitor = new StringReader(linha);
        using var parser = new CsvParser(leitor, configuracao);

        return parser.Read() ? (parser.Record ?? Array.Empty<string>()) : Array.Empty<string>();
    }
}
