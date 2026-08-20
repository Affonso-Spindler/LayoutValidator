using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasNumericasCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasNumericasCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("42", true)]
    [InlineData("-7", true)]
    [InlineData("4,2", false)]
    [InlineData("", true)]
    public void Inteiro_AceitaInteiroOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Inteiro"].Avaliar(valor, null));

    [Theory]
    [InlineData("1", false)]
    [InlineData("18", true)]
    [InlineData("60", true)]
    [InlineData("61", false)]
    [InlineData("", true)]
    public void InteiroEntre_RespeitaLimitesInclusiveEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":18,"maximo":60}""");
        Assert.Equal(esperado, Regras["InteiroEntre"].Avaliar(valor, parametros));
    }

    [Theory]
    [InlineData("1234,56", true)]
    [InlineData("1.234,56", false)]
    [InlineData("", true)]
    public void Decimal_AceitaFormatoBrasileiroOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Decimal"].Avaliar(valor, null));

    [Theory]
    [InlineData("0,00", false)]
    [InlineData("10,50", true)]
    [InlineData("100,00", true)]
    [InlineData("100,01", false)]
    public void DecimalEntre_RespeitaLimitesInclusive(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":10.50,"maximo":100.00}""");
        Assert.Equal(esperado, Regras["DecimalEntre"].Avaliar(valor, parametros));
    }
}
