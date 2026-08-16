using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras.Tests.Predicados;

public class UnidadesFederativasTestes
{
    [Theory]
    [InlineData("AC")] [InlineData("AL")] [InlineData("AP")] [InlineData("AM")]
    [InlineData("BA")] [InlineData("CE")] [InlineData("DF")] [InlineData("ES")]
    [InlineData("GO")] [InlineData("MA")] [InlineData("MT")] [InlineData("MS")]
    [InlineData("MG")] [InlineData("PA")] [InlineData("PB")] [InlineData("PR")]
    [InlineData("PE")] [InlineData("PI")] [InlineData("RJ")] [InlineData("RN")]
    [InlineData("RS")] [InlineData("RO")] [InlineData("RR")] [InlineData("SC")]
    [InlineData("SP")] [InlineData("SE")] [InlineData("TO")]
    public void Valida_AceitaAsSiglasReais(string sigla) =>
        Assert.True(UnidadesFederativas.Valida(sigla));

    [Fact]
    public void Todas_TemAsVinteESeteUnidades() =>
        Assert.Equal(27, UnidadesFederativas.Todas.Count);

    [Theory]
    [InlineData("CC")]   // não é estado nenhum
    [InlineData("XX")]
    [InlineData("BR")]
    [InlineData("S")]    // curto demais
    [InlineData("SPP")]  // longo demais
    [InlineData("")]
    [InlineData(null)]
    public void Valida_RecusaSiglaInexistente(string? valor) =>
        Assert.False(UnidadesFederativas.Valida(valor));

    [Theory]
    [InlineData("sp")]
    [InlineData("Sp")]
    [InlineData("sP")]
    public void Valida_IgnoraCaixa(string valor) =>
        Assert.True(UnidadesFederativas.Valida(valor));

    [Theory]
    [InlineData(" SP")]
    [InlineData("SP ")]
    public void Valida_RecusaEspacoSobrando(string valor) =>
        Assert.False(UnidadesFederativas.Valida(valor));
}
