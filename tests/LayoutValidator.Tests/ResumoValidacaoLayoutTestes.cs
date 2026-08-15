using LayoutValidator.Core;
using Xunit;

namespace LayoutValidator.Tests;

public class ResumoValidacaoLayoutTestes
{
    [Fact]
    public void Registrar_MisturaDeValidosEInvalidos_ContabilizaCorretamente()
    {
        var resumo = new ResumoValidacaoLayout();

        resumo.Registrar(new RegistroValido<string> { NumeroLinha = 2, Registro = "ok" });
        resumo.Registrar(new RegistroValido<string> { NumeroLinha = 3, Registro = "ok" });
        resumo.Registrar(new RegistroInvalido<string>
        {
            NumeroLinha = 4,
            ValoresRaw = new Dictionary<string, string>(),
            Erros = new[]
            {
                new ErroValidacaoLayout
                {
                    NumeroLinha = 4,
                    NomeCampo = "Idade",
                    ValorRaw = "abc",
                    NomeRegra = "IdadeDeveSerInteiro",
                    Mensagem = "Idade deve ser um número inteiro."
                }
            }
        });

        Assert.Equal(3, resumo.TotalRegistros);
        Assert.Equal(2, resumo.RegistrosValidos);
        Assert.Equal(1, resumo.RegistrosInvalidos);
        Assert.Equal(1, resumo.ErrosPorRegra["IdadeDeveSerInteiro"]);
        Assert.Equal(1, resumo.ErrosPorCampo["Idade"]);
    }
}
