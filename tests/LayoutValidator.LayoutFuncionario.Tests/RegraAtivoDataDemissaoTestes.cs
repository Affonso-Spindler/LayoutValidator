namespace LayoutValidator.LayoutFuncionario.Tests;

/// <summary>
/// Regra entre campos do Funcionario: o registro só é válido com Ativo = N e DataDemissao
/// preenchida, ou Ativo = S e DataDemissao vazia. Cada campo sozinho não enxerga o erro —
/// só a combinação.
/// </summary>
public class RegraAtivoDataDemissaoTestes
{
    private const string DataValida = "10/10/2022";
    private const string DataInvalida = "31/02/2000";

    private static readonly FuncionarioValidador Validador = new();

    private static FuncionarioRaw Funcionario(string ativo, string dataDemissao) => new()
    {
        MatriculaId = "1",
        Nome = "Diego Costa",
        Cpf = "25332508565",
        Rg = "398073043",
        DataNascimento = "06/07/1965",
        Email = "diego.costa1@empresa.com.br",
        Telefone = "(24) 90901-7345",
        Cargo = "Estagiário",
        Departamento = "Operações",
        Salario = "19461,01",
        DataAdmissao = "16/09/2010",
        DataDemissao = dataDemissao,
        Ativo = ativo,
        Cep = "13973-739",
        Endereco = "Avenida Brasil",
        NumeroEndereco = "9061",
        Bairro = "Boa Vista",
        Cidade = "Rio de Janeiro",
        Uf = "SP",
        CargaHoraria = "30"
    };

    [Theory]
    [InlineData("S", "")]
    [InlineData("N", DataValida)]
    [InlineData("s", "")]
    [InlineData("n", DataValida)]
    public void CombinacaoCoerente_Passa(string ativo, string dataDemissao)
    {
        Assert.Empty(Validador.Validate(Funcionario(ativo, dataDemissao)).Errors);
    }

    // Minúsculas também disparam: ValorEm("S", "N") aceita "s" e "n", então a regra entre
    // campos precisa enxergar os dois do mesmo jeito.
    [Theory]
    [InlineData("S", DataValida, "DataDemissaoEmFuncionarioAtivo")]
    [InlineData("S", DataInvalida, "DataDemissaoEmFuncionarioAtivo")]
    [InlineData("N", "", "CampoObrigatorio")]
    [InlineData("N", DataInvalida, "DataInvalida")]
    [InlineData("s", DataValida, "DataDemissaoEmFuncionarioAtivo")]
    [InlineData("n", "", "CampoObrigatorio")]
    public void CombinacaoIncoerente_ReprovaNaDataDemissao(string ativo, string dataDemissao, string codigoEsperado)
    {
        var erro = Assert.Single(Validador.Validate(Funcionario(ativo, dataDemissao)).Errors);

        Assert.Equal(nameof(FuncionarioRaw.DataDemissao), erro.PropertyName);
        Assert.Equal(codigoEsperado, erro.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(DataValida)]
    public void AtivoForaDoDominio_ReprovaSoNoAtivo(string dataDemissao)
    {
        // Sem S nem N, nenhuma das condições dispara: o erro é do Ativo, não da combinação.
        var erro = Assert.Single(Validador.Validate(Funcionario("X", dataDemissao)).Errors);

        Assert.Equal(nameof(FuncionarioRaw.Ativo), erro.PropertyName);
    }
}
