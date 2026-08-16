using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras.Tests.Predicados;

public class DocumentosTestes
{
    // CPFs e CNPJs válidos amplamente publicados como vetor de teste — conferidos dígito a
    // dígito contra o módulo 11 antes de entrar aqui.
    [Theory]
    [InlineData("11144477735")]
    [InlineData("52998224725")]
    public void CpfValido_AceitaCpfComDigitoVerificadorCorreto(string valor) =>
        Assert.True(Documentos.CpfValido(valor));

    [Theory]
    [InlineData("11144477736")]  // último dígito trocado
    [InlineData("11144477725")]  // penúltimo dígito trocado
    [InlineData("52998224720")]
    public void CpfValido_RecusaDigitoVerificadorErrado(string valor) =>
        Assert.False(Documentos.CpfValido(valor));

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void CpfValido_RecusaSequenciaDeDigitoRepetido(string valor) =>
        Assert.False(Documentos.CpfValido(valor));

    [Theory]
    [InlineData("1114447773")]        // 10 dígitos
    [InlineData("111444777351")]      // 12 dígitos
    [InlineData("111.444.777-35")]    // com máscara
    [InlineData("1114447773a")]
    [InlineData("")]
    [InlineData(null)]
    public void CpfValido_RecusaEstruturaForaDoPadrao(string? valor) =>
        Assert.False(Documentos.CpfValido(valor));

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("04252011000110")]
    public void CnpjValido_AceitaCnpjComDigitoVerificadorCorreto(string valor) =>
        Assert.True(Documentos.CnpjValido(valor));

    [Theory]
    [InlineData("11222333000182")]
    [InlineData("11222333000191")]
    [InlineData("00000000000000")]
    [InlineData("1122233300018")]
    [InlineData("11.222.333/0001-81")]
    [InlineData("")]
    [InlineData(null)]
    public void CnpjValido_RecusaCnpjInvalido(string? valor) =>
        Assert.False(Documentos.CnpjValido(valor));

    [Theory]
    [InlineData("11144477735")]
    [InlineData("11222333000181")]
    public void CpfOuCnpjValido_AceitaOsDoisFormatos(string valor) =>
        Assert.True(Documentos.CpfOuCnpjValido(valor));

    [Fact]
    public void CpfOuCnpjValido_RecusaValorQueNaoEhNemUmNemOutro() =>
        Assert.False(Documentos.CpfOuCnpjValido("123456789012"));

    // PIS e CNH não têm vetor de teste publicado tão consagrado quanto CPF/CNPJ. Os valores
    // abaixo foram derivados do algoritmo padrão e conferidos na mão; o par de casos cobre
    // os dois ramos do cálculo (com e sem o desconto de 2 no segundo dígito da CNH).
    [Fact]
    public void PisPasepValido_AceitaValorComDigitoCorreto() =>
        Assert.True(Documentos.PisPasepValido("12345678900"));

    [Theory]
    [InlineData("12345678901")]
    [InlineData("00000000000")]
    [InlineData("123456789")]
    [InlineData("")]
    [InlineData(null)]
    public void PisPasepValido_RecusaValorInvalido(string? valor) =>
        Assert.False(Documentos.PisPasepValido(valor));

    [Theory]
    [InlineData("12345678900")]  // caminho sem desconto
    [InlineData("98765432109")]  // caminho com o desconto de 2
    public void CnhValida_AceitaValorComDigitosCorretos(string valor) =>
        Assert.True(Documentos.CnhValida(valor));

    [Theory]
    [InlineData("12345678901")]
    [InlineData("98765432100")]
    [InlineData("11111111111")]
    [InlineData("1234567890")]
    [InlineData("")]
    [InlineData(null)]
    public void CnhValida_RecusaValorInvalido(string? valor) =>
        Assert.False(Documentos.CnhValida(valor));

    [Theory]
    [InlineData("4111111111111111")]  // número de teste Visa
    [InlineData("5500005555555559")]
    public void LuhnValido_AceitaNumeroComDigitoDeControleCorreto(string valor) =>
        Assert.True(Documentos.LuhnValido(valor));

    [Theory]
    [InlineData("4111111111111112")]
    [InlineData("411111111111")]        // 12 dígitos, curto demais
    [InlineData("41111111111111111111")] // 20 dígitos, longo demais
    [InlineData("4111-1111-1111-1111")]
    [InlineData("")]
    [InlineData(null)]
    public void LuhnValido_RecusaNumeroInvalido(string? valor) =>
        Assert.False(Documentos.LuhnValido(valor));
}
