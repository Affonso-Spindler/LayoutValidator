using System.Reflection;
using FluentValidation;

namespace LayoutValidator.Regras.Tests.Extensions;

/// <summary>
/// O contrato que toda regra do catálogo tem que respeitar: <b>formato não reprova vazio</b>,
/// só <c>Obrigatorio()</c> reprova; e cada regra carrega um código de erro estável, que é o
/// que o <c>ResumoValidacaoLayout</c> agrupa em <c>ErrosPorRegra</c>.
/// </summary>
public class ContratoDoCatalogoTestes
{
    private static readonly (string Nome, Action<IRuleBuilder<RegistroTeste, string>> Configurar)[] RegrasDeFormato =
    {
        ("ComprimentoEntre", regra => regra.ComprimentoEntre(3, 5)),
        ("ComprimentoMaximo", regra => regra.ComprimentoMaximo(5)),
        ("ComprimentoExato", regra => regra.ComprimentoExato(5)),
        ("SomenteDigitos", regra => regra.SomenteDigitos()),
        ("ValorEm", regra => regra.ValorEm("S", "N")),
        ("Formato", regra => regra.Formato(@"^\d+$", "MeuCodigo", "Minha mensagem.")),
        ("Inteiro", regra => regra.Inteiro()),
        ("InteiroPositivo", regra => regra.InteiroPositivo()),
        ("InteiroNaoNegativo", regra => regra.InteiroNaoNegativo()),
        ("InteiroEntre", regra => regra.InteiroEntre(1, 60)),
        ("Decimal", regra => regra.Decimal()),
        ("DecimalPositivo", regra => regra.DecimalPositivo()),
        ("DecimalEntre", regra => regra.DecimalEntre(0, 100)),
        ("Data", regra => regra.Data()),
        ("DataEntre", regra => regra.DataEntre(new DateTime(2000, 1, 1), new DateTime(2030, 12, 31))),
        ("DataNoPassado", regra => regra.DataNoPassado()),
        ("Moeda", regra => regra.Moeda()),
        ("Percentual", regra => regra.Percentual()),
        ("CartaoDeCredito", regra => regra.CartaoDeCredito()),
        ("Email", regra => regra.Email()),
        ("Cpf", regra => regra.Cpf()),
        ("Cnpj", regra => regra.Cnpj()),
        ("CpfOuCnpj", regra => regra.CpfOuCnpj()),
        ("Cep", regra => regra.Cep()),
        ("Uf", regra => regra.Uf()),
        ("Telefone", regra => regra.Telefone()),
        ("Cnh", regra => regra.Cnh()),
        ("PisPasep", regra => regra.PisPasep())
    };

    [Fact]
    public void RegraDeFormato_NuncaReprovaValorVazio()
    {
        foreach (var (nome, configurar) in RegrasDeFormato)
        {
            foreach (var vazio in new[] { "", "   ", "\t" })
            {
                var erros = Exercitar.Regra(configurar, vazio);

                Assert.True(
                    erros.Count == 0,
                    $"A regra {nome} reprovou o valor vazio \"{vazio}\" — regra de formato deve ignorar vazio.");
            }
        }
    }

    [Fact]
    public void TodasAsRegrasDeFormato_EstaoCobertasPorEsteContrato()
    {
        // Trava simples contra esquecer de registrar uma regra nova na tabela acima.
        var metodosDeExtensao = new[]
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
            .Where(nome => nome != nameof(RegrasTextoExtensions.Obrigatorio))
            .Distinct()
            .ToList();

        var cobertas = RegrasDeFormato.Select(regra => regra.Nome).ToHashSet();
        var descobertas = metodosDeExtensao.Where(nome => !cobertas.Contains(nome)).ToList();

        Assert.True(descobertas.Count == 0, $"Regras sem cobertura de contrato: {string.Join(", ", descobertas)}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Obrigatorio_EhAUnicaRegraQueReprovaVazio(string vazio) =>
        Assert.Equal("CampoObrigatorio", Exercitar.CodigoDoUnicoErro(regra => regra.Obrigatorio(), vazio));

    [Fact]
    public void ObrigatorioMaisFormato_EmValorVazio_ProduzUmErroSo()
    {
        var erros = Exercitar.Regra(regra => regra.Obrigatorio().Cpf(), "");

        Assert.Single(erros);
        Assert.Equal("CampoObrigatorio", erros[0].ErrorCode);
    }

    [Fact]
    public void ObrigatorioMaisFormato_EmValorPreenchidoEInvalido_AcusaOFormato()
    {
        var erros = Exercitar.Regra(regra => regra.Obrigatorio().Cpf(), "12345678900");

        Assert.Single(erros);
        Assert.Equal("CpfInvalido", erros[0].ErrorCode);
    }

    [Theory]
    [InlineData("ComprimentoInvalido", "ab")]
    public void ComprimentoEntre_UsaCodigoEstavel(string codigo, string valor) =>
        Assert.Equal(codigo, Exercitar.CodigoDoUnicoErro(regra => regra.ComprimentoEntre(3, 5), valor));

    [Fact]
    public void CadaRegra_CarregaSeuProprioCodigoDeErro()
    {
        var esperados = new (string Codigo, Action<IRuleBuilder<RegistroTeste, string>> Configurar, string ValorInvalido)[]
        {
            ("SomenteDigitosInvalido", regra => regra.SomenteDigitos(), "12a"),
            ("ValorForaDoDominio", regra => regra.ValorEm("S", "N"), "X"),
            ("MeuCodigo", regra => regra.Formato(@"^\d+$", "MeuCodigo", "Minha mensagem."), "abc"),
            ("InteiroInvalido", regra => regra.Inteiro(), "abc"),
            ("InteiroPositivoInvalido", regra => regra.InteiroPositivo(), "0"),
            ("InteiroNaoNegativoInvalido", regra => regra.InteiroNaoNegativo(), "-1"),
            ("InteiroForaDoIntervalo", regra => regra.InteiroEntre(1, 60), "61"),
            ("DecimalInvalido", regra => regra.Decimal(), "abc"),
            ("DecimalPositivoInvalido", regra => regra.DecimalPositivo(), "0"),
            ("DecimalForaDoIntervalo", regra => regra.DecimalEntre(0, 100), "101"),
            ("DataInvalida", regra => regra.Data(), "31/02/2000"),
            ("DataForaDoIntervalo", regra => regra.DataEntre(new DateTime(2000, 1, 1), new DateTime(2020, 12, 31)), "01/01/2021"),
            ("DataNoFuturo", regra => regra.DataNoPassado(), DateTime.Today.AddDays(1).ToString("dd/MM/yyyy")),
            ("MoedaInvalida", regra => regra.Moeda(), "1234,5"),
            ("PercentualInvalido", regra => regra.Percentual(), "101"),
            ("CartaoDeCreditoInvalido", regra => regra.CartaoDeCredito(), "4111111111111112"),
            ("EmailInvalido", regra => regra.Email(), "sem-arroba"),
            ("CpfInvalido", regra => regra.Cpf(), "12345678900"),
            ("CnpjInvalido", regra => regra.Cnpj(), "11222333000182"),
            ("CpfOuCnpjInvalido", regra => regra.CpfOuCnpj(), "123456789012"),
            ("CepInvalido", regra => regra.Cep(), "1234-567"),
            ("UfInvalida", regra => regra.Uf(), "CC"),
            ("TelefoneInvalido", regra => regra.Telefone(), "11 98765-4321"),
            ("CnhInvalida", regra => regra.Cnh(), "12345678901"),
            ("PisPasepInvalido", regra => regra.PisPasep(), "12345678901")
        };

        foreach (var (codigo, configurar, valorInvalido) in esperados)
        {
            var obtido = Exercitar.CodigoDoUnicoErro(configurar, valorInvalido);
            Assert.True(obtido == codigo, $"Esperava o código {codigo} para o valor \"{valorInvalido}\", veio {obtido ?? "(nenhum erro ou mais de um)"}.");
        }
    }

    [Fact]
    public void Uf_ReprovaCcEAceitaSp()
    {
        Assert.Empty(Exercitar.Regra(regra => regra.Uf(), "SP"));
        Assert.Single(Exercitar.Regra(regra => regra.Uf(), "CC"));
    }
}
