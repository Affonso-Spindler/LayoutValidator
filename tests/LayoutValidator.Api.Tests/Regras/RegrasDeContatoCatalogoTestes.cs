using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeContatoCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeContatoCatalogo.Construir().ToDictionary(r => r.Chave);

    [Theory]
    [InlineData("maria@empresa.com.br", true)]
    [InlineData("maria.silva+tag@empresa.com", true)]
    [InlineData("sem-arroba", false)]
    [InlineData("maria@sem-ponto", false)]
    [InlineData("maria @empresa.com", false)] // espaço reprova
    [InlineData("", true)]
    public void Email_ValidaFormatoEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Email"].Avaliar(valor, null));
}
