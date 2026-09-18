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

    [Theory]
    [InlineData("Maria", true)]
    [InlineData("Maria Silva", true)]     // espaço — nome composto
    [InlineData("João", true)]            // acentuação conta como letra
    [InlineData("A D'Marcas", true)]      // apóstrofo — sobrenome legítimo
    [InlineData("Maria-Clara", true)]     // hífen — idem
    [InlineData("Maria2", false)]
    [InlineData("Maria_Silva", false)]    // underscore não é pontuação de nome
    [InlineData("Maria@Silva", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SomenteLetras_AceitaLetrasUnicodeEPontuacaoDeNome(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SomenteLetras(valor));

    [Theory]
    [InlineData("Jose", true)]
    [InlineData("JOSE DA SILVA 123", true)] // dígito e espaço não são acento
    [InlineData("José", false)]
    [InlineData("ação", false)]
    [InlineData("Ç", false)]                // cedilha conta
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SemAcento_ReprovaLetraAcentuadaOuCedilha(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SemAcento(valor));

    [Fact]
    public void SemAcento_TrataAcentoJaDecompostoIgualAoComposto()
    {
        // "José" com o acento como caractere de combinação separado (NFD) é o mesmo texto
        // pro usuário — tem que reprovar igual à forma composta (NFC).
        Assert.False(Formatos.SemAcento("José"));
        Assert.False(Formatos.SemAcento("José"));
    }

    [Theory]
    [InlineData("JOSE", true)]
    [InlineData("NF-123", true)]  // dígito e pontuação não interferem
    [InlineData("JOSé", false)]
    [InlineData("jose", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SomenteMaiusculas_ReprovaQualquerMinuscula(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SomenteMaiusculas(valor));

    [Theory]
    [InlineData("jose", true)]
    [InlineData("nf-123", true)]
    [InlineData("joSe", false)]
    [InlineData("JOSE", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SomenteMinusculas_ReprovaQualquerMaiuscula(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SomenteMinusculas(valor));

    [Theory]
    [InlineData("Maria", true)]
    [InlineData("Maria Silva", true)]   // espaço no meio é problema de outra regra
    [InlineData("A ", false)]
    [InlineData(" A", false)]
    [InlineData("Maria\t", false)]      // tabulação também é espaço em branco
    [InlineData("Maria\r", false)]      // \r que sobra na última coluna de arquivo CRLF
    [InlineData("   ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SemEspacoNasBordas_ReprovaEspacoNoComecoOuNoFim(string? valor, bool esperado) =>
        Assert.Equal(esperado, Formatos.SemEspacoNasBordas(valor));

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
