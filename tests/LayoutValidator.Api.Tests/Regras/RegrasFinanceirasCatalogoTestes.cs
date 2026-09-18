using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasFinanceirasCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasFinanceirasCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("1234,56", true)]
    [InlineData("1234,5", false)]   // casas decimais a menos
    [InlineData("1234", false)]     // sem casa decimal
    [InlineData("1.234,56", false)] // separador de milhar não entra
    [InlineData("", true)]
    public void Moeda_UsaDuasCasasQuandoOParametroNaoVem(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Moeda"].Avaliar(valor, null));

    [Fact]
    public void Moeda_RespeitaCasasDecimaisDoParametro()
    {
        var tresCasas = Parametros("""{"casasDecimais":3}""");

        Assert.True(Regras["Moeda"].Avaliar("1234,567", tresCasas));
        Assert.False(Regras["Moeda"].Avaliar("1234,56", tresCasas));
    }

    [Fact]
    public void Moeda_CaiProPadraoQuandoOParametroVemNuloOuComTipoErrado()
    {
        // A tela manda a chave mesmo com o campo em branco — vira null no JSON. Sem tratar,
        // isso seria uma exceção no meio da validação, não um "usa o padrão".
        foreach (var json in new[] { """{"casasDecimais":null}""", """{"casasDecimais":"duas"}""", "{}" })
            Assert.True(Regras["Moeda"].Avaliar("1234,56", Parametros(json)), $"falhou para {json}");
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("100", true)]
    [InlineData("50,5", true)]
    [InlineData("101", false)]
    [InlineData("-1", false)]
    [InlineData("", true)]
    public void Percentual_AceitaDeZeroACemEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Percentual"].Avaliar(valor, null));

    [Theory]
    [InlineData("4111111111111111", true)]
    [InlineData("4111111111111112", false)] // dígito verificador de Luhn errado
    [InlineData("abc", false)]
    [InlineData("", true)]
    public void CartaoDeCredito_ValidaPorLuhnEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["CartaoDeCredito"].Avaliar(valor, null));
}
