using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras.Tests.Predicados;

public class FormatosTestes
{
    [Theory]
    [InlineData("12/05/1990")]
    [InlineData("29/02/2020")]  // ano bissexto
    public void DataValida_AceitaDataRealNoFormato(string valor) =>
        Assert.True(Formatos.DataValida(valor, "dd/MM/yyyy"));

    [Theory]
    [InlineData("31/02/2000")]  // dia que não existe nesse mês
    [InlineData("29/02/2021")]  // ano não bissexto
    [InlineData("1990-05-12")]  // formato errado
    [InlineData("12/5/1990")]   // sem zero à esquerda
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void DataValida_RecusaDataForaDoFormatoOuInexistente(string? valor) =>
        Assert.False(Formatos.DataValida(valor, "dd/MM/yyyy"));

    [Fact]
    public void DataEntre_RespeitaOsLimitesInclusive()
    {
        var minimo = new DateTime(2020, 1, 1);
        var maximo = new DateTime(2020, 12, 31);

        Assert.True(Formatos.DataEntre("01/01/2020", minimo, maximo, "dd/MM/yyyy"));
        Assert.True(Formatos.DataEntre("31/12/2020", minimo, maximo, "dd/MM/yyyy"));
        Assert.False(Formatos.DataEntre("31/12/2019", minimo, maximo, "dd/MM/yyyy"));
        Assert.False(Formatos.DataEntre("01/01/2021", minimo, maximo, "dd/MM/yyyy"));
    }

    [Fact]
    public void DataNoPassado_AceitaHojeERecusaAmanha()
    {
        var hoje = DateTime.Today.ToString("dd/MM/yyyy");
        var amanha = DateTime.Today.AddDays(1).ToString("dd/MM/yyyy");

        Assert.True(Formatos.DataNoPassado(hoje, "dd/MM/yyyy"));
        Assert.False(Formatos.DataNoPassado(amanha, "dd/MM/yyyy"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("42")]
    [InlineData("-7")]
    public void InteiroValido_AceitaInteiro(string valor) =>
        Assert.True(Formatos.InteiroValido(valor));

    [Theory]
    [InlineData("4,2")]
    [InlineData("4.2")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void InteiroValido_RecusaNaoInteiro(string? valor) =>
        Assert.False(Formatos.InteiroValido(valor));

    [Fact]
    public void InteiroEntre_RespeitaOsLimitesInclusive()
    {
        Assert.True(Formatos.InteiroEntre("1", 1, 60));
        Assert.True(Formatos.InteiroEntre("60", 1, 60));
        Assert.False(Formatos.InteiroEntre("0", 1, 60));
        Assert.False(Formatos.InteiroEntre("61", 1, 60));
        Assert.False(Formatos.InteiroEntre("abc", 1, 60));
    }

    [Theory]
    [InlineData("1234,56")]
    [InlineData("0,5")]
    [InlineData("42")]
    [InlineData("-7,5")]
    public void DecimalValido_AceitaPadraoBrasileiro(string valor) =>
        Assert.True(Formatos.DecimalValido(valor));

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void DecimalValido_RecusaNaoNumero(string? valor) =>
        Assert.False(Formatos.DecimalValido(valor));

    // Separador de milhar fica fora de propósito: o Mapper típico faz Replace(',', '.') e
    // parseia com InvariantCulture, onde "1.00" vale 1 — enquanto em pt-BR valeria 100.
    // Aceitar aqui deixaria passar um valor que chega diferente no banco.
    [Theory]
    [InlineData("1.234,56")]
    [InlineData("1.00")]
    public void DecimalValido_RecusaSeparadorDeMilhar(string valor) =>
        Assert.False(Formatos.DecimalValido(valor));

    [Fact]
    public void Percentual_NaoAceitaValorQueOMapperLeriaDiferente()
    {
        // "1.00" seria 100 em pt-BR (dentro do intervalo 0-100) e 1 no Mapper.
        Assert.False(Formatos.DecimalEntre("1.00", 0m, 100m));
        Assert.True(Formatos.DecimalEntre("100", 0m, 100m));
        Assert.True(Formatos.DecimalEntre("1,00", 0m, 100m));
    }

    [Theory]
    [InlineData("1234,56")]
    [InlineData("0,00")]
    public void MoedaValida_AceitaCasasDecimaisExatas(string valor) =>
        Assert.True(Formatos.MoedaValida(valor, 2));

    [Theory]
    [InlineData("1234,5")]    // uma casa decimal só
    [InlineData("1234,567")]  // três casas
    [InlineData("1234")]      // sem casa decimal
    [InlineData("1.234,56")]  // separador de milhar não é aceito aqui
    [InlineData("1234.56")]
    [InlineData(",56")]
    [InlineData("")]
    [InlineData(null)]
    public void MoedaValida_RecusaFormatoForaDoPadrao(string? valor) =>
        Assert.False(Formatos.MoedaValida(valor, 2));

    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("12a", false)]
    [InlineData("1 2", false)]
    [InlineData("-1", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SomenteDigitos_AceitaApenasDigitos(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SomenteDigitos(valor));

    [Fact]
    public void ComprimentoEntre_TrataNuloComoComprimentoZero()
    {
        Assert.True(Formatos.ComprimentoEntre("abc", 1, 3));
        Assert.True(Formatos.ComprimentoEntre("a", 1, 3));
        Assert.False(Formatos.ComprimentoEntre("abcd", 1, 3));
        Assert.False(Formatos.ComprimentoEntre(null, 1, 3));
        Assert.True(Formatos.ComprimentoEntre(null, 0, 3));
    }
}
