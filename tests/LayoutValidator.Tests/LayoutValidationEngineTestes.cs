using LayoutValidator.Core;
using LayoutValidator.Tests.Modelos;
using Xunit;

namespace LayoutValidator.Tests;

public class LayoutValidationEngineTestes
{
    private static readonly ValidadorRegistroTeste Validador = new();
    private static readonly RegistroTesteMapper Mapper = new();

    private static List<ResultadoValidacaoRegistro<RegistroTeste>> ValidarFixture(
        string nomeArquivo,
        OpcoesLayout? opcoes = null)
    {
        using var leitor = new StreamReader(Path.Combine("Fixtures", nomeArquivo));
        return LayoutValidationEngine.Validar(leitor, opcoes ?? new OpcoesLayout(), Validador, Mapper).ToList();
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

    [Fact]
    public void OpcoesLayout_NasceComPontoEVirgulaECabecalhoPresente()
    {
        var opcoes = new OpcoesLayout();

        Assert.Equal(";", opcoes.Delimitador);
        Assert.Equal(";", OpcoesLayout.DelimitadorPadrao);
        Assert.Equal(ModoCabecalho.Presente, opcoes.Cabecalho);
    }

    [Fact]
    public void Validar_ComDelimitadorTrocado_LeArquivoDeVirgula()
    {
        var resultados = ValidarFixture("delimitador_errado.csv", new OpcoesLayout { Delimitador = "," });

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, r => Assert.IsType<RegistroValido<RegistroTeste>>(r));
    }

    [Fact]
    public void Validar_SemCabecalho_CasaColunaPorPosicaoNaOrdemDeDeclaracao()
    {
        // RegistroRawTeste declara Nome, Idade, DataNascimento — e a fixture vem nessa ordem,
        // sem linha de cabeçalho. Este teste é o que sustenta a decisão de não exigir
        // [Index(n)]: se o CsvHelper deixar de casar por ordem de declaração, quebra aqui.
        var resultados = ValidarFixture("sem_cabecalho.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.Ausente });

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, r => Assert.IsType<RegistroValido<RegistroTeste>>(r));

        var primeiro = Assert.IsType<RegistroValido<RegistroTeste>>(resultados[0]);
        Assert.Equal("Maria", primeiro.Registro.Nome);
        Assert.Equal(30, primeiro.Registro.Idade);
        Assert.Equal(new DateTime(1994, 1, 1), primeiro.Registro.DataNascimento);
    }

    [Fact]
    public void Validar_CabecalhoPresenteIgnorado_DescartaAPrimeiraLinhaECasaPorPosicao()
    {
        // O cabeçalho é COL_A;COL_B;COL_C — não bate com nome nenhum de propriedade.
        var resultados = ValidarFixture("cabecalho_ignorado.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.PresenteIgnorado });

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, r => Assert.IsType<RegistroValido<RegistroTeste>>(r));

        var primeiro = Assert.IsType<RegistroValido<RegistroTeste>>(resultados[0]);
        Assert.Equal("Maria", primeiro.Registro.Nome);
    }

    [Fact]
    public void Validar_LinhaDeCabecalho_SoContaComoRegistroNoModoAusente()
    {
        // Mesmo arquivo nos três modos. valido.csv tem 3 linhas físicas: 1 de cabeçalho e 2
        // de dados. Presente e PresenteIgnorado consomem a primeira linha — ela não passa
        // pelo validador nem entra no total. Ausente trata ela como dado.
        var presente = ValidarFixture("valido.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.Presente });
        var ignorado = ValidarFixture("valido.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.PresenteIgnorado });
        var ausente = ValidarFixture("valido.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.Ausente });

        Assert.Equal(2, presente.Count);
        Assert.Equal(2, ignorado.Count);
        Assert.Equal(3, ausente.Count);

        // No modo Ausente a linha de cabeçalho vira um registro — e reprova, porque
        // "Idade" não é inteiro e "DataNascimento" não é data.
        var cabecalhoComoRegistro = Assert.IsType<RegistroInvalido<RegistroTeste>>(ausente[0]);
        Assert.Contains(cabecalhoComoRegistro.Erros, erro => erro.ValorRaw == "Idade");
    }

    [Fact]
    public void Validar_ArquivoComDelimitadorDiferenteDoDeclarado_FalhaComMensagemUtil()
    {
        // Arquivo de vírgula lido com o padrão ';': o cabeçalho inteiro vira uma coluna só,
        // então a contagem de colunas bate (1 == 1) e o problema passaria batido até o
        // GetRecord estourar uma ReaderException crua.
        var excecao = Assert.Throws<LayoutIncompativelException>(() => ValidarFixture("delimitador_errado.csv"));

        Assert.Equal(";", excecao.DelimitadorEsperado);
        Assert.Single(excecao.PrimeiraLinha);
        Assert.Contains("RegistroRawTeste", excecao.Message);
        Assert.Contains("';'", excecao.Message);
    }

    [Fact]
    public void Validar_SemCabecalhoComDelimitadorErrado_FalhaComMensagemUtil()
    {
        var excecao = Assert.Throws<LayoutIncompativelException>(
            () => ValidarFixture("delimitador_errado.csv", new OpcoesLayout { Cabecalho = ModoCabecalho.Ausente }));

        Assert.Equal(";", excecao.DelimitadorEsperado);
        Assert.Contains("1 coluna", excecao.Message);
        Assert.Contains("espera 3", excecao.Message);
    }

    [Fact]
    public void Validar_CabecalhoQueCasaSoEmParte_NaoFalhaEViraErroDeValidacao()
    {
        // Nome e Idade casam, Sobrenome não existe no layout e DataNascimento não veio.
        // Casamento parcial pode ser arquivo com coluna faltando — é erro de validação,
        // não incompatibilidade de formato.
        var resultados = ValidarFixture("cabecalho_parcial.csv");

        var invalido = Assert.IsType<RegistroInvalido<RegistroTeste>>(Assert.Single(resultados));
        Assert.Contains(invalido.Erros, erro => erro.NomeCampo == "DataNascimento");
    }

    [Fact]
    public void Validar_ArquivoVazio_NaoRetornaNadaENaoQuebra()
    {
        using var leitor = new StringReader(string.Empty);

        var resultados = LayoutValidationEngine.Validar(leitor, new OpcoesLayout(), Validador, Mapper).ToList();

        Assert.Empty(resultados);
    }
}
