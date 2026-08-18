using LayoutValidator.Core;
using LayoutValidator.Tests.Modelos;
using Xunit;

namespace LayoutValidator.Tests;

/// <summary>
/// Cobre os dois overloads de <see cref="LayoutValidationEngine.Validar"/> que não passam por
/// arquivo — dados já em memória (ex.: retorno de consulta), sempre posicionais (sem
/// cabeçalho) e sem fachada de layout. O overload de linha delimitada ainda usa
/// <see cref="OpcoesLayout"/> — só pra reusar o mesmo <c>Delimitador</c> de uma eventual fachada
/// de arquivo do mesmo layout; <c>Cabecalho</c> é ignorado. Nenhum teste aqui depende de banco:
/// os dados são construídos direto em cada `[Fact]`, o mesmo padrão inline já usado por
/// `Validar_ArquivoVazio_NaoRetornaNadaENaoQuebra` em <see cref="LayoutValidationEngineTestes"/>.
/// </summary>
public class LayoutValidationEngineLinhasTestes
{
    private static readonly ValidadorRegistroTeste Validador = new();
    private static readonly RegistroTesteMapper Mapper = new();

    [Fact]
    public void Validar_ValoresJaSeparados_RetornaRegistroValido()
    {
        var linhas = new[]
        {
            (IReadOnlyList<string>) new[] { "Maria", "30", "01/01/1994" }
        };

        var resultados = LayoutValidationEngine.Validar(linhas, Validador, Mapper).ToList();

        var valido = Assert.IsType<RegistroValido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Equal("Maria", valido.Registro.Nome);
        Assert.Equal(30, valido.Registro.Idade);
        Assert.Equal(new DateTime(1994, 1, 1), valido.Registro.DataNascimento);
        Assert.Equal(1, valido.NumeroLinha);
    }

    [Fact]
    public void Validar_ValoresJaSeparados_ErroDeRegraViraRegistroInvalido()
    {
        var linhas = new[]
        {
            (IReadOnlyList<string>) new[] { "Maria", "trinta", "01/01/1994" }
        };

        var resultados = LayoutValidationEngine.Validar(linhas, Validador, Mapper).ToList();

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        var erro = Assert.Single(invalido.Erros);
        Assert.Equal("Idade", erro.NomeCampo);
        Assert.Equal("IdadeDeveSerInteiro", erro.NomeRegra);
    }

    [Fact]
    public void Validar_ValoresJaSeparados_ContagemErradaViraEstruturaDeColunas()
    {
        var linhas = new[]
        {
            (IReadOnlyList<string>) new[] { "Maria", "30" } // falta DataNascimento
        };

        var resultados = LayoutValidationEngine.Validar(linhas, Validador, Mapper).ToList();

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        var erro = Assert.Single(invalido.Erros);
        Assert.Equal("EstruturaDeColunas", erro.NomeRegra);
        Assert.Contains("2 coluna(s), esperado 3", erro.Mensagem);
    }

    [Fact]
    public void Validar_ValoresJaSeparados_SequenciaVazia_NaoRetornaNadaENaoQuebra()
    {
        var resultados = LayoutValidationEngine.Validar(
            Array.Empty<IReadOnlyList<string>>(), Validador, Mapper).ToList();

        Assert.Empty(resultados);
    }

    [Fact]
    public void Validar_LinhaDelimitada_RetornaRegistroValido()
    {
        var linhas = new[] { "Maria;30;01/01/1994" };

        var resultados = LayoutValidationEngine.Validar(linhas, new OpcoesLayout(), Validador, Mapper).ToList();

        var valido = Assert.IsType<RegistroValido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Equal("Maria", valido.Registro.Nome);
    }

    [Fact]
    public void Validar_LinhaDelimitada_UsaODelimitadorDeOpcoesLayout()
    {
        // Mesmo OpcoesLayout que uma fachada de arquivo do mesmo layout usaria — é assim que os
        // dois caminhos ficam consistentes sem duplicar o valor do delimitador em dois lugares.
        var linhas = new[] { "Maria,30,01/01/1994" };

        var resultados = LayoutValidationEngine.Validar(
            linhas, new OpcoesLayout { Delimitador = "," }, Validador, Mapper).ToList();

        Assert.All(resultados, r => Assert.IsType<RegistroValido<RegistroTeste>>(r));
    }

    [Fact]
    public void Validar_LinhaDelimitada_CabecalhoEmOpcoesLayoutEhIgnorado()
    {
        // Mesmo passando Cabecalho = Presente, este caminho é sempre posicional — não existe
        // conceito de cabeçalho aqui, e não deve tratar a primeira linha como cabeçalho.
        var linhas = new[] { "Maria;30;01/01/1994" };

        var resultados = LayoutValidationEngine.Validar(
            linhas, new OpcoesLayout { Cabecalho = ModoCabecalho.Presente }, Validador, Mapper).ToList();

        var valido = Assert.IsType<RegistroValido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Equal("Maria", valido.Registro.Nome);
    }

    [Fact]
    public void Validar_LinhaDelimitada_ValorEntreAspasContendoODelimitadorEhTratadoComoUmSoCampo()
    {
        // "Maria;Silva" entre aspas não deve ser quebrado em dois campos pelo ';' interno —
        // mesma robustez de escaping que o caminho de arquivo já tem via CsvHelper.
        var linhas = new[] { "\"Maria;Silva\";30;01/01/1994" };

        var resultados = LayoutValidationEngine.Validar(linhas, new OpcoesLayout(), Validador, Mapper).ToList();

        var valido = Assert.IsType<RegistroValido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Equal("Maria;Silva", valido.Registro.Nome);
    }

    [Fact]
    public void Validar_LinhaDelimitada_ContagemErradaViraEstruturaDeColunas()
    {
        var linhas = new[] { "Maria;30" };

        var resultados = LayoutValidationEngine.Validar(linhas, new OpcoesLayout(), Validador, Mapper).ToList();

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Equal("EstruturaDeColunas", Assert.Single(invalido.Erros).NomeRegra);
    }

    [Fact]
    public void Validar_LinhaDelimitada_SequenciaVazia_NaoRetornaNadaENaoQuebra()
    {
        var resultados = LayoutValidationEngine.Validar(
            Array.Empty<string>(), new OpcoesLayout(), Validador, Mapper).ToList();

        Assert.Empty(resultados);
    }
}
