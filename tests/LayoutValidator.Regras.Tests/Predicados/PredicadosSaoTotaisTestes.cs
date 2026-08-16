using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras.Tests.Predicados;

/// <summary>
/// Nenhum predicado pode lançar exceção, seja qual for a entrada.
///
/// Isso não é preciosismo: o motor de validação é um iterador preguiçoso que percorre o
/// arquivo linha a linha. Uma exceção dentro de uma regra não reprova a linha — ela sobe
/// pelo <c>foreach</c> de quem consome e aborta a leitura do arquivo inteiro. Uma célula
/// suja no meio de um arquivo de milhões de linhas derrubaria a carga toda.
/// </summary>
public class PredicadosSaoTotaisTestes
{
    public static TheoryData<string?> EntradasHostis() => new()
    {
        null,
        "",
        " ",
        "abc",
        "-",
        ",",
        "0,",
        ",0",
        "-1",
        "999999999999999999999999999999999",  // estoura long e decimal
        "\t\n\r",
        "ção",
        "😀",
        "\0",
        "1e10",
        "0x1F",
        new string('9', 5000)
    };

    [Theory]
    [MemberData(nameof(EntradasHostis))]
    public void NenhumPredicadoLanca(string? valor)
    {
        // Cada chamada é um caminho independente; o teste passa se nenhuma lançar.
        _ = Documentos.CpfValido(valor);
        _ = Documentos.CnpjValido(valor);
        _ = Documentos.CpfOuCnpjValido(valor);
        _ = Documentos.PisPasepValido(valor);
        _ = Documentos.CnhValida(valor);
        _ = Documentos.LuhnValido(valor);

        _ = Formatos.DataValida(valor, "dd/MM/yyyy");
        _ = Formatos.DataEntre(valor, DateTime.MinValue, DateTime.MaxValue, "dd/MM/yyyy");
        _ = Formatos.DataNoPassado(valor, "dd/MM/yyyy");
        _ = Formatos.InteiroValido(valor);
        _ = Formatos.InteiroEntre(valor, 0, 10);
        _ = Formatos.DecimalValido(valor);
        _ = Formatos.DecimalEntre(valor, 0, 100);
        _ = Formatos.MoedaValida(valor, 2);
        _ = Formatos.SomenteDigitos(valor);
        _ = Formatos.ComprimentoEntre(valor, 1, 10);

        _ = UnidadesFederativas.Valida(valor);
    }

    [Fact]
    public void MoedaValida_NaoLancaComCasasDecimaisAbsurdas()
    {
        Assert.False(Formatos.MoedaValida("1234,56", -1));
        Assert.False(Formatos.MoedaValida("1234,56", int.MaxValue));
    }
}
