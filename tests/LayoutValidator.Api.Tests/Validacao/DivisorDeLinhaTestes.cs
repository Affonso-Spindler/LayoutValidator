using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class DivisorDeLinhaTestes
{
    [Fact]
    public void Dividir_QuebraPeloDelimitadorInformado()
    {
        var campos = DivisorDeLinha.Dividir("12345678901;João;30", ";");

        Assert.Equal(new[] { "12345678901", "João", "30" }, campos);
    }

    [Fact]
    public void Dividir_RespeitaAspasAoRedorDeCampoComDelimitadorDentro()
    {
        var campos = DivisorDeLinha.Dividir("\"Rua A; 123\";São Paulo", ";");

        Assert.Equal(new[] { "Rua A; 123", "São Paulo" }, campos);
    }

    [Fact]
    public void Dividir_AceitaDelimitadorDiferenteDePontoEVirgula()
    {
        var campos = DivisorDeLinha.Dividir("a|b|c", "|");

        Assert.Equal(new[] { "a", "b", "c" }, campos);
    }

    [Fact]
    public void Dividir_LinhaVaziaRetornaArrayVazio()
    {
        var campos = DivisorDeLinha.Dividir("", ";");

        Assert.Empty(campos);
    }
}
