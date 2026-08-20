using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeDocumentoCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeDocumentoCatalogo.Construir().ToDictionary(r => r.Chave);

    [Theory]
    [InlineData("11144477735", true)]  // CPF válido conhecido
    [InlineData("11111111111", false)] // dígitos repetidos
    [InlineData("12345678900", false)] // dígito verificador errado
    [InlineData("", true)]
    public void Cpf_ValidaDigitoVerificadorEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cpf"].Avaliar(valor, null));

    [Theory]
    [InlineData("11222333000181", true)] // CNPJ válido conhecido
    [InlineData("11111111000111", false)]
    [InlineData("", true)]
    public void Cnpj_ValidaDigitoVerificadorEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cnpj"].Avaliar(valor, null));

    [Theory]
    [InlineData("11144477735", true)]      // CPF
    [InlineData("11222333000181", true)]   // CNPJ
    [InlineData("123", false)]
    public void CpfOuCnpj_AceitaQualquerUmDosDois(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["CpfOuCnpj"].Avaliar(valor, null));

    [Theory]
    [InlineData("01310-100", true)]
    [InlineData("01310100", true)]
    [InlineData("1310-100", false)]
    public void Cep_AceitaComOuSemHifen(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cep"].Avaliar(valor, null));

    [Theory]
    [InlineData("SP", true)]
    [InlineData("sp", true)]
    [InlineData("CC", false)]
    public void Uf_AceitaSiglaRealIgnorandoCaixa(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Uf"].Avaliar(valor, null));

    [Theory]
    [InlineData("(11) 98888-7777", true)]
    [InlineData("11988887777", true)]
    [InlineData("998887777", false)]
    public void Telefone_AceitaFormatosConhecidos(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Telefone"].Avaliar(valor, null));

    [Theory]
    [InlineData("02650306461", true)] // CNH válida conhecida
    [InlineData("00000000000", false)]
    public void Cnh_ValidaDigitoVerificador(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cnh"].Avaliar(valor, null));

    [Theory]
    [InlineData("12045678905", true)] // PIS/PASEP válido (dígito verificador conferido à mão contra o algoritmo)
    [InlineData("00000000000", false)]
    public void PisPasep_ValidaDigitoVerificador(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["PisPasep"].Avaliar(valor, null));
}
