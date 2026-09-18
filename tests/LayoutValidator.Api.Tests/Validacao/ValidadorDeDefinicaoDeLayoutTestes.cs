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

    [Fact]
    public void Validar_AceitaRegraDeDataSemFormatoInformado()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Nascimento", new[] { new RegraCampoRequest("Data", null) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }

    [Theory]
    [InlineData("22222222222")] // sem nenhum especificador de data real (y, M ou d)
    [InlineData("")]
    public void Validar_RejeitaFormatoDeDataSemEspecificadores(string formato)
    {
        var parametros = JsonDocument.Parse($$"""{"formato":"{{formato}}"}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Nascimento", new[] { new RegraCampoRequest("Data", parametros) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        if (string.IsNullOrEmpty(formato))
            Assert.Empty(erros); // formato em branco é "não informado" — usa o padrão, não é erro
        else
            Assert.Contains(erros, e => e.Contains("Nascimento") && e.Contains("especificador"));
    }

    [Theory]
    [InlineData("yyyy-MM-dd")]
    [InlineData("dd/MM/yyyy HH:mm:ss")]
    [InlineData("HH:mm")] // coluna só de hora é cenário legítimo de arquivo
    public void Validar_AceitaFormatoDeDataOuHoraValido(string formato)
    {
        var parametros = JsonDocument.Parse($$"""{"formato":"{{formato}}"}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Nascimento", new[] { new RegraCampoRequest("Data", parametros) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }

    [Fact]
    public void Validar_RejeitaCasasDecimaisNegativasEmMoeda()
    {
        var parametros = JsonDocument.Parse("""{"casasDecimais":-1}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Valor", new[] { new RegraCampoRequest("Moeda", parametros) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("Valor") && e.Contains("casasDecimais"));
    }

    [Fact]
    public void Validar_RejeitaDataEntreComMinimoOuMaximoForaDoFormato()
    {
        var parametros = JsonDocument.Parse("""{"minimo":"data-invalida","maximo":"31/12/2024"}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Nascimento", new[] { new RegraCampoRequest("DataEntre", parametros) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("minimo"));
        Assert.DoesNotContain(erros, e => e.Contains("'maximo'"));
    }
}
