using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class LayoutsEndpointsTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public LayoutsEndpointsTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    private static LayoutRequest LayoutPessoaValido(string codigo) => new(
        codigo, "Pessoa", ";",
        new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", new[] { new RegraCampoRequest("Obrigatorio", null) })
        });

    [Fact]
    public async Task Post_CriaLayoutERetorna201ComOCodigoNaLocation()
    {
        var resposta = await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA1"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("/layouts/PESSOA1", resposta.Headers.Location?.OriginalString);

        var corpo = await resposta.Content.ReadFromJsonAsync<LayoutResponse>();
        Assert.Equal("PESSOA1", corpo!.Codigo);
        Assert.Equal(2, corpo.Campos.Count);
    }

    [Fact]
    public async Task Post_RejeitaCodigoDuplicadoCom409()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA2"));

        var resposta = await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA2"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_RejeitaLayoutComParametroFaltandoCom400()
    {
        var requisicao = new LayoutRequest("PESSOA3", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", null) })
        });

        var resposta = await _cliente.PostAsJsonAsync("/layouts", requisicao);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_ObtemLayoutPeloCodigo()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA4"));

        var resposta = await _cliente.GetAsync("/layouts/PESSOA4");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<LayoutResponse>();
        Assert.Equal("PESSOA4", corpo!.Codigo);
    }

    [Fact]
    public async Task Get_RetornaNotFoundParaCodigoInexistente()
    {
        var resposta = await _cliente.GetAsync("/layouts/NAOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetLista_RetornaOsLayoutsCadastrados()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA5"));

        var resposta = await _cliente.GetAsync("/layouts");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<List<LayoutResponse>>();
        Assert.Contains(corpo!, l => l.Codigo == "PESSOA5");
    }
}
