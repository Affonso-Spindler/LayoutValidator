using System.Text.Json;
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class ValidadorDeDefinicaoDeLayoutTestes
{
    private readonly ICatalogoDeRegras _catalogo = new CatalogoDeRegras();

    [Fact]
    public void Validar_SemErrosParaLayoutBemFormado()
    {
        var parametros = JsonDocument.Parse("""{"minimo":18,"maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }

    [Theory]
    [InlineData("FUNC 2024")]                    // espaço
    [InlineData("FUNC-2024")]                     // hífen
    [InlineData("FUNC/2024")]                     // vira segmento de URL — não pode ter barra
    [InlineData("CODIGOMUITOGRANDEDEMAISPARASER")] // mais de 20 caracteres
    [InlineData("")]
    public void Validar_RejeitaCodigoForaDoFormato(string codigo)
    {
        var requisicao = new LayoutRequest(codigo, "Pessoa", ";", Array.Empty<CampoRequest>());

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("inválido"));
    }

    [Fact]
    public void Validar_RejeitaChaveDeRegraInexistente()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("NaoExiste", null) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("NaoExiste") && e.Contains("não existe no catálogo"));
    }

    [Fact]
    public void Validar_RejeitaParametroObrigatorioFaltando()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", null) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("minimo"));
        Assert.Contains(erros, e => e.Contains("maximo"));
    }

    [Fact]
    public void Validar_RejeitaParametroComTipoErrado()
    {
        var parametros = JsonDocument.Parse("""{"minimo":"dezoito","maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("minimo"));
    }

    [Fact]
    public void Validar_RegraSemParametrosObrigatoriosNuncaGeraErro()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }
}
