using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeTextoCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeTextoCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("abc", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Obrigatorio_ReprovaVazioAceitaResto(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Obrigatorio"].Avaliar(valor, null));

    [Theory]
    [InlineData("", true)]       // regra de formato nunca reprova vazio
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    [InlineData("abcde", true)]
    [InlineData("abcdef", false)]
    public void ComprimentoEntre_RespeitaLimitesEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":3,"maximo":5}""");
        Assert.Equal(esperado, Regras["ComprimentoEntre"].Avaliar(valor, parametros));
    }

    [Fact]
    public void ComprimentoEntre_MontaMensagemComOsParametros()
    {
        var parametros = Parametros("""{"minimo":3,"maximo":5}""");
        Assert.Equal("'Nome' deve ter entre 3 e 5 caracteres.", Regras["ComprimentoEntre"].MontarMensagem("Nome", parametros));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("12a", false)]
    [InlineData("", true)]
    public void SomenteDigitos_AceitaSoDigitosOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["SomenteDigitos"].Avaliar(valor, null));

    [Theory]
    [InlineData("S", true)]
    [InlineData("n", true)]
    [InlineData("X", false)]
    [InlineData("", true)]
    public void ValorEm_AceitaDominioIgnorandoCaixaEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"valores":["S","N"]}""");
        Assert.Equal(esperado, Regras["ValorEm"].Avaliar(valor, parametros));
    }

    [Fact]
    public void Formato_UsaRegexECodigoErroEMensagemDosParametros()
    {
        var parametros = Parametros("""{"expressaoRegular":"^[0-9]{3}$","codigoErro":"CodigoInvalido","mensagem":"'{PropertyName}' precisa de 3 dígitos."}""");
        var regra = Regras["Formato"];

        Assert.True(regra.Avaliar("123", parametros));
        Assert.False(regra.Avaliar("12", parametros));
        Assert.True(regra.Avaliar("", parametros));
        Assert.Equal("CodigoInvalido", regra.ObterCodigoErro(parametros));
        Assert.Equal("'Codigo' precisa de 3 dígitos.", regra.MontarMensagem("Codigo", parametros));
    }
}
