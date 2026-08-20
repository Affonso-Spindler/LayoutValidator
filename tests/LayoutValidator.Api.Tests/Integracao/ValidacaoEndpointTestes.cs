using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class ValidacaoEndpointTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public ValidacaoEndpointTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    private async Task CadastrarLayoutPessoaAsync(string codigo)
    {
        var requisicao = new LayoutRequest(codigo, "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", new[] { new RegraCampoRequest("Obrigatorio", null) })
        });

        await _cliente.PostAsJsonAsync("/layouts", requisicao);
    }

    [Fact]
    public async Task Validar_LinhaAderenteRetornaAderenteTrueSemErros()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA1");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA1/validar", new ValidarRequest("11144477735;João"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.True(corpo!.Aderente);
        Assert.Empty(corpo.Erros);
    }

    [Fact]
    public async Task Validar_CpfInvalidoRetornaErroDoCampo()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA2");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA2/validar", new ValidarRequest("12345678900;João"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Single(corpo.Erros);
        Assert.Equal("Cpf", corpo.Erros[0].Campo);
        Assert.Equal("CpfInvalido", corpo.Erros[0].Regra);
    }

    [Fact]
    public async Task Validar_ContagemDeColunasErradaRetornaEstruturaDeColunas()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA3");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA3/validar", new ValidarRequest("11144477735;João;Extra"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Equal("EstruturaDeColunas", corpo.Erros[0].Regra);
    }

    [Fact]
    public async Task Validar_LayoutInexistenteRetorna404()
    {
        var resposta = await _cliente.PostAsJsonAsync("/layouts/NAOEXISTE/validar", new ValidarRequest("qualquer;coisa"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Validar_CorpoSemLinhaRetorna400()
    {
        // "linha" ausente do JSON — sem a guarda, DivisorDeLinha.Dividir(null, ...) explodia
        // com ArgumentNullException dentro de new StringReader(null) (500) em vez de 400.
        await CadastrarLayoutPessoaAsync("VALPESSOA5");

        var conteudo = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resposta = await _cliente.PostAsync("/layouts/VALPESSOA5/validar", conteudo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Validar_CampoObrigatorioVazioRetornaErro()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA4");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA4/validar", new ValidarRequest("11144477735;"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Equal("Nome", corpo.Erros[0].Campo);
        Assert.Equal("CampoObrigatorio", corpo.Erros[0].Regra);
    }
}
