using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class RegrasEndpointTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public RegrasEndpointTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Get_ListaAs19RegrasDoCatalogo()
    {
        var resposta = await _cliente.GetAsync("/regras");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<List<RegraDisponivelResponse>>();
        Assert.Equal(19, corpo!.Count);
    }

    [Fact]
    public async Task Get_DescreveOsParametrosEsperadosDeInteiroEntre()
    {
        var resposta = await _cliente.GetAsync("/regras");
        var corpo = await resposta.Content.ReadFromJsonAsync<List<RegraDisponivelResponse>>();

        var inteiroEntre = corpo!.Single(r => r.Chave == "InteiroEntre");

        Assert.Equal(2, inteiroEntre.ParametrosEsperados.Count);
        Assert.Contains(inteiroEntre.ParametrosEsperados, p => p.Nome == "minimo" && p.Obrigatorio);
        Assert.Contains(inteiroEntre.ParametrosEsperados, p => p.Nome == "maximo" && p.Obrigatorio);
    }
}
