using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class AvaliadorDeCampoTestes
{
    private readonly ICatalogoDeRegras _catalogo = new CatalogoDeRegras();

    [Fact]
    public void Avaliar_RetornaNuloQuandoTodasAsRegrasPassam()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Cpf",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
        };

        Assert.Null(AvaliadorDeCampo.Avaliar(campo, "11144477735", _catalogo));
    }

    [Fact]
    public void Avaliar_RetornaErroDaRegraQueFalhou()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Cpf",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
        };

        var erro = AvaliadorDeCampo.Avaliar(campo, "12345678900", _catalogo);

        Assert.NotNull(erro);
        Assert.Equal("Cpf", erro!.Campo);
        Assert.Equal("CpfInvalido", erro.Regra);
    }

    [Fact]
    public void Avaliar_ParaNaPrimeiraRegraQueFalha_CascadeStop()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Idade",
            Ordem = 0,
            Regras =
            {
                new RegraCampoCadastrada { ChaveRegra = "Inteiro", Ordem = 0 },
                new RegraCampoCadastrada
                {
                    ChaveRegra = "InteiroEntre",
                    ParametrosJson = """{"minimo":18,"maximo":60}""",
                    Ordem = 1
                }
            }
        };

        // "abc" falha em Inteiro (primeira regra) e também falharia em InteiroEntre (segunda) —
        // o teste só prova cascade-stop porque o erro retornado é o de Inteiro, não o de
        // InteiroEntre: confirma que a segunda regra nunca chegou a ser avaliada.
        var erro = AvaliadorDeCampo.Avaliar(campo, "abc", _catalogo);

        Assert.NotNull(erro);
        Assert.Equal("InteiroInvalido", erro!.Regra);
    }

    [Fact]
    public void Avaliar_CampoOpcionalVazioPassaSemObrigatorio()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Observacao",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "ComprimentoMaximo", ParametrosJson = """{"maximo":100}""", Ordem = 0 } }
        };

        Assert.Null(AvaliadorDeCampo.Avaliar(campo, "", _catalogo));
    }
}
