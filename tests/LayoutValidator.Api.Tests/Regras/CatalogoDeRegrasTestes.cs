using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class CatalogoDeRegrasTestes
{
    private static readonly string[] ChavesEsperadas =
    {
        "Obrigatorio", "ComprimentoEntre", "ComprimentoMaximo", "ComprimentoExato",
        "SomenteDigitos", "ValorEm", "Formato", "Inteiro", "InteiroEntre", "Decimal",
        "DecimalEntre", "Cpf", "Cnpj", "CpfOuCnpj", "Cep", "Uf", "Telefone", "Cnh", "PisPasep"
    };

    [Fact]
    public void Todas_ContemExatamenteAs19ChavesDaV1()
    {
        var catalogo = new CatalogoDeRegras();
        var chaves = catalogo.Todas.Select(r => r.Chave).ToArray();

        Assert.Equal(ChavesEsperadas.Length, chaves.Length);
        foreach (var chave in ChavesEsperadas)
            Assert.Contains(chave, chaves);
    }

    [Fact]
    public void Existe_EObter_SaoCaseInsensitive()
    {
        var catalogo = new CatalogoDeRegras();

        Assert.True(catalogo.Existe("cpf"));
        Assert.Equal("Cpf", catalogo.Obter("CPF").Chave);
    }

    [Fact]
    public void Obter_LancaParaChaveInexistente()
    {
        var catalogo = new CatalogoDeRegras();

        Assert.Throws<InvalidOperationException>(() => catalogo.Obter("NaoExiste"));
    }
}
