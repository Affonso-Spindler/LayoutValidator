using System.Diagnostics;
using System.Globalization;
using LayoutValidator.Core;
using LayoutValidator.LayoutFuncionario;
using LayoutValidator.Reporting;

namespace LayoutValidator.TesteApp;

public partial class TelaPrincipal : Form
{
    private readonly IValidadorLayout<Funcionario> _validadorLayout;
    private readonly List<ErroValidacaoLayout> _errosParaExibir = new();

    private string? _caminhoArquivoSelecionado;
    private string? _caminhoRelatorioGerado;

    public TelaPrincipal(IValidadorLayout<Funcionario> validadorLayout)
    {
        _validadorLayout = validadorLayout;
        InitializeComponent();
    }

    private void BotaoSelecionarArquivo_Click(object? sender, EventArgs e)
    {
        using var dialogo = new OpenFileDialog
        {
            Filter = "Arquivos de dados (*.csv;*.txt)|*.csv;*.txt|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo para validar"
        };

        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        _caminhoArquivoSelecionado = dialogo.FileName;
        rotuloArquivoSelecionado.Text = _caminhoArquivoSelecionado;
        botaoValidar.Enabled = true;
        botaoAbrirRelatorio.Enabled = false;
    }

    private async void BotaoValidar_Click(object? sender, EventArgs e)
    {
        if (_caminhoArquivoSelecionado is null)
            return;

        botaoSelecionarArquivo.Enabled = false;
        botaoValidar.Enabled = false;
        botaoAbrirRelatorio.Enabled = false;
        barraProgresso.Visible = true;
        _errosParaExibir.Clear();
        listaErros.VirtualListSize = 0;
        rotuloTotal.Text = "-";
        rotuloValidos.Text = "-";
        rotuloInvalidos.Text = "-";
        rotuloTempo.Text = "-";
        rotuloContagemErros.Text = "";

        var progresso = new Progress<int>(total => rotuloStatus.Text = $"Processando... {total:N0} registros lidos");
        var caminhoRelatorio = Path.Combine(
            Path.GetDirectoryName(_caminhoArquivoSelecionado)!,
            Path.GetFileNameWithoutExtension(_caminhoArquivoSelecionado) + "_erros.csv");

        try
        {
            var resumo = await Task.Run(() => Validar(_caminhoArquivoSelecionado, caminhoRelatorio, progresso));

            rotuloTotal.Text = resumo.Resumo.TotalRegistros.ToString("N0", CultureInfo.InvariantCulture);
            rotuloValidos.Text = resumo.Resumo.RegistrosValidos.ToString("N0", CultureInfo.InvariantCulture);
            rotuloInvalidos.Text = resumo.Resumo.RegistrosInvalidos.ToString("N0", CultureInfo.InvariantCulture);
            rotuloTempo.Text = $"{resumo.TempoDecorrido.TotalSeconds:N1} s";
            rotuloStatus.Text = "Concluído.";
            rotuloContagemErros.Text = _errosParaExibir.Count == resumo.Resumo.RegistrosInvalidos
                ? $"Exibindo todos os {_errosParaExibir.Count:N0} erro(s) encontrado(s)."
                : $"Exibindo {_errosParaExibir.Count:N0} erro(s) de {resumo.Resumo.RegistrosInvalidos:N0} registro(s) inválido(s).";

            listaErros.VirtualListSize = _errosParaExibir.Count;
            listaErros.Invalidate();

            _caminhoRelatorioGerado = caminhoRelatorio;
            botaoAbrirRelatorio.Enabled = true;
        }
        catch (Exception ex)
        {
            rotuloStatus.Text = "Falhou.";
            MessageBox.Show(this, ex.Message, "Erro ao validar arquivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            barraProgresso.Visible = false;
            botaoSelecionarArquivo.Enabled = true;
            botaoValidar.Enabled = true;
        }
    }

    private (ResumoValidacaoLayout Resumo, TimeSpan TempoDecorrido) Validar(string caminhoEntrada, string caminhoRelatorio, IProgress<int> progresso)
    {
        var cronometro = Stopwatch.StartNew();
        var resumo = new ResumoValidacaoLayout();
        var processados = 0;

        using var leitor = new StreamReader(caminhoEntrada);
        using var relatorio = new ErrorReportWriter(new StreamWriter(caminhoRelatorio));

        foreach (var resultado in _validadorLayout.Validar(leitor))
        {
            resumo.Registrar(resultado);
            relatorio.Write(resultado);

            if (resultado is RegistroInvalido<Funcionario> invalido)
            {
                lock (_errosParaExibir)
                {
                    _errosParaExibir.AddRange(invalido.Erros);
                }
            }

            processados++;
            if (processados % 5000 == 0)
                progresso.Report(processados);
        }

        cronometro.Stop();
        progresso.Report(processados);
        return (resumo, cronometro.Elapsed);
    }

    private void ListaErros_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
    {
        var erro = _errosParaExibir[e.ItemIndex];
        e.Item = new ListViewItem(new[]
        {
            erro.NumeroLinha.ToString(CultureInfo.InvariantCulture),
            erro.NomeCampo,
            erro.NomeRegra,
            erro.Mensagem,
            erro.ValorRaw
        });
    }

    private void BotaoAbrirRelatorio_Click(object? sender, EventArgs e)
    {
        if (_caminhoRelatorioGerado is null)
            return;

        Process.Start(new ProcessStartInfo(_caminhoRelatorioGerado) { UseShellExecute = true });
    }
}
