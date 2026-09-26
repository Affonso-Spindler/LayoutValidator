namespace LayoutValidator.LayoutFuncionario.Tests;

/// <summary>
/// Quem aceita, converte: <c>ValorEm("S", "N")</c> e <c>Uf()</c> aceitam minúsculas, então o
/// mapper tem que tratar "s" igual a "S" — senão um valor aprovado na validação chega errado
/// no Model final, sem nenhum erro no relatório.
/// </summary>
public class FuncionarioMapperTestes
{
    private static readonly FuncionarioMapper Mapper = new();

    private static FuncionarioRaw Funcionario(string ativo = "S", string uf = "SP") => new()
    {
        MatriculaId = "1",
        DataNascimento = "06/07/1965",
        Salario = "19461,01",
        DataAdmissao = "16/09/2010",
        Ativo = ativo,
        NumeroEndereco = "9061",
        Uf = uf,
        CargaHoraria = "30"
    };

    [Theory]
    [InlineData("S", true)]
    [InlineData("s", true)]
    [InlineData("N", false)]
    [InlineData("n", false)]
    public void Ativo_IgnoraMaiusculasEMinusculas(string ativo, bool esperado)
    {
        Assert.Equal(esperado, Mapper.Map(Funcionario(ativo: ativo)).Ativo);
    }

    [Theory]
    [InlineData("SP")]
    [InlineData("sp")]
    [InlineData("Sp")]
    public void Uf_GravaEmMaiusculo(string uf)
    {
        Assert.Equal("SP", Mapper.Map(Funcionario(uf: uf)).Uf);
    }
}
