using System.Text.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Contratos;

public class MapeadorDeLayoutTestes
{
    [Fact]
    public void ParaEntidade_DerivaOrdemDaPosicaoNoArray()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", Array.Empty<RegraCampoRequest>())
        });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        Assert.Equal(0, layout.Campos[0].Ordem);
        Assert.Equal(1, layout.Campos[1].Ordem);
        Assert.Equal(0, layout.Campos[0].Regras[0].Ordem);
        Assert.Equal(1, layout.Campos[0].Regras[1].Ordem);
    }

    [Fact]
    public void ParaEntidade_SerializaParametrosJsonComoTextoCru()
    {
        var parametros = JsonDocument.Parse("""{"minimo":1,"maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        Assert.Equal("""{"minimo":1,"maximo":60}""", layout.Campos[0].Regras[0].ParametrosJson);
    }

    [Fact]
    public void ParaResposta_OrdenaCamposERegrasPelaOrdemCadastrada()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null) }),
            new CampoRequest("Nome", Array.Empty<RegraCampoRequest>())
        });
        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        var resposta = MapeadorDeLayout.ParaResposta(layout);

        Assert.Equal("Cpf", resposta.Campos[0].Nome);
        Assert.Equal("Nome", resposta.Campos[1].Nome);
        Assert.Equal("Obrigatorio", resposta.Campos[0].Regras[0].ChaveRegra);
    }
}
