using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeDataCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeDataCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("31/12/2024", true)]
    [InlineData("29/02/2024", true)]
    [InlineData("29/02/2023", false)]
    [InlineData("31/04/2024", false)]
    [InlineData("2024-12-31", false)]
    [InlineData("", true)]
    public void Data_UsaFormatoBrasileiroPorPadraoEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Data"].Avaliar(valor, null));

    [Fact]
    public void Data_AceitaFormatoCustomizadoViaParametro()
    {
        var parametros = Parametros("""{"formato":"yyyy-MM-dd"}""");

        Assert.True(Regras["Data"].Avaliar("2024-12-31", parametros));
        Assert.False(Regras["Data"].Avaliar("31/12/2024", parametros));
    }

    [Theory]
    [InlineData("01/01/2024", false)]
    [InlineData("15/06/2024", true)]
    [InlineData("31/12/2024", true)]
    [InlineData("01/01/2025", false)]
    [InlineData("", true)]
    public void DataEntre_RespeitaLimitesInclusiveEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":"01/02/2024","maximo":"31/12/2024"}""");
        Assert.Equal(esperado, Regras["DataEntre"].Avaliar(valor, parametros));
    }

    [Fact]
    public void DataEntre_TrataLimiteQueNaoBateComOFormatoComoNaoAderente()
    {
        var parametros = Parametros("""{"minimo":"data-invalida","maximo":"31/12/2024"}""");
        Assert.False(Regras["DataEntre"].Avaliar("15/06/2024", parametros));
    }

    [Fact]
    public void DataNoPassado_AceitaHojeEPassadoMasRejeitaFuturo()
    {
        const string formato = "dd/MM/yyyy";
        var hoje = DateTime.Today.ToString(formato);
        var ontem = DateTime.Today.AddDays(-1).ToString(formato);
        var amanha = DateTime.Today.AddDays(1).ToString(formato);

        Assert.True(Regras["DataNoPassado"].Avaliar(hoje, null));
        Assert.True(Regras["DataNoPassado"].Avaliar(ontem, null));
        Assert.False(Regras["DataNoPassado"].Avaliar(amanha, null));
        Assert.True(Regras["DataNoPassado"].Avaliar("", null));
    }
}
