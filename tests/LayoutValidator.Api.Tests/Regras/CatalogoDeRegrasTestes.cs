using System.Reflection;
using LayoutValidator.Api.Regras;
using LayoutValidator.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class CatalogoDeRegrasTestes
{
    private static readonly string[] ChavesEsperadas =
    {
        "Obrigatorio", "ComprimentoEntre", "ComprimentoMaximo", "ComprimentoExato",
        "ComprimentoMinimo", "SomenteDigitos", "SomenteLetras", "SemAcento", "SomenteMaiusculas",
        "SomenteMinusculas", "SemEspacoNasBordas", "ValorEm", "Formato", "Inteiro", "InteiroPositivo",
        "InteiroNaoNegativo", "InteiroEntre", "Decimal", "DecimalPositivo", "DecimalEntre", "Cpf",
        "Cnpj", "CpfOuCnpj", "Cep", "Uf", "Telefone", "Cnh", "PisPasep", "Data", "DataEntre",
        "DataNoPassado", "Moeda", "Percentual", "CartaoDeCredito", "Email"
    };

    [Fact]
    public void Todas_ContemExatamenteAs35ChavesDaV1()
    {
        var catalogo = new CatalogoDeRegras();
        var chaves = catalogo.Todas.Select(r => r.Chave).ToArray();

        Assert.Equal(ChavesEsperadas.Length, chaves.Length);
        foreach (var chave in ChavesEsperadas)
            Assert.Contains(chave, chaves);
    }

    /// <summary>
    /// Os dois caminhos (layout como código e layout cadastrado) prometem o mesmo catálogo —
    /// é o que a wiki diz e o que o usuário da tela espera. Já aconteceu de a promessa ficar
    /// pra trás sem ninguém ver: 7 regras existiam só no código. Esta trava fecha isso.
    /// </summary>
    [Fact]
    public void CatalogoCadastravel_TemParidadeComAsExtensoesDeCodigo()
    {
        var regrasDoCodigo = new[]
            {
                typeof(RegrasTextoExtensions),
                typeof(RegrasNumericasExtensions),
                typeof(RegrasDataExtensions),
                typeof(RegrasFinanceirasExtensions),
                typeof(RegrasContatoExtensions),
                typeof(RegrasBrasilExtensions)
            }
            .SelectMany(tipo => tipo.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(metodo => metodo.Name)
            .Distinct()
            .ToList();

        var cadastraveis = new CatalogoDeRegras().Todas.Select(r => r.Chave).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var faltando = regrasDoCodigo.Where(nome => !cadastraveis.Contains(nome)).ToList();

        Assert.True(faltando.Count == 0, $"Regras que existem no código mas não dá pra cadastrar: {string.Join(", ", faltando)}");
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
