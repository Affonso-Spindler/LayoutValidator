using System.Globalization;
using CsvHelper.Configuration;
using LayoutValidator.Core;
using LayoutValidator.Tests.Modelos;
using Xunit;

namespace LayoutValidator.Tests;

public class LayoutValidationEngineTestes
{
    private static readonly ValidadorRegistroTeste Validador = new();
    private static readonly RegistroTesteMapper Mapper = new();

    private static List<ResultadoValidacaoRegistro<RegistroTeste>> ValidarFixture(string nomeArquivo)
    {
        using var leitor = new StreamReader(Path.Combine("Fixtures", nomeArquivo));
        var configuracaoCsv = new CsvConfiguration(CultureInfo.InvariantCulture);
        return LayoutValidationEngine.Validar(leitor, configuracaoCsv, Validador, Mapper).ToList();
    }

    [Fact]
    public void Validar_ArquivoValido_RetornaTodosRegistrosValidos()
    {
        var resultados = ValidarFixture("valido.csv");

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, r => Assert.IsType<RegistroValido<RegistroTeste>>(r));
    }

    [Fact]
    public void Validar_CampoInteiroInvalido_RetornaRegistroInvalidoComErroDeRegra()
    {
        var resultados = ValidarFixture("invalido_inteiro.csv");

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        var erro = Assert.Single(invalido.Erros);
        Assert.Equal("Idade", erro.NomeCampo);
        Assert.Equal("IdadeDeveSerInteiro", erro.NomeRegra);
        Assert.Equal("trinta", erro.ValorRaw);
    }

    [Fact]
    public void Validar_CampoDataInvalido_RetornaRegistroInvalidoComErroDeRegra()
    {
        var resultados = ValidarFixture("invalido_data.csv");

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        var erro = Assert.Single(invalido.Erros);
        Assert.Equal("DataNascimento", erro.NomeCampo);
        Assert.Equal("DataNascimentoFormatoInvalido", erro.NomeRegra);
    }

    [Fact]
    public void Validar_LinhaComColunasFaltando_NaoInterrompeLeituraEGeraErroEstrutural()
    {
        var resultados = ValidarFixture("linhas_malformadas.csv");

        Assert.Equal(2, resultados.Count);

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(resultados[0]);
        var erro = Assert.Single(invalido.Erros);
        Assert.Equal("EstruturaDeColunas", erro.NomeRegra);

        var valido = Assert.IsType<RegistroValido<RegistroTeste>>(resultados[1]);
        Assert.Equal("Joao", valido.Registro.Nome);
    }
}
