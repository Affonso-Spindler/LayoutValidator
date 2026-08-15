namespace LayoutValidator.TesteApp;

partial class TelaPrincipal
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        botaoSelecionarArquivo = new Button();
        rotuloArquivoSelecionado = new Label();
        botaoValidar = new Button();
        rotuloStatus = new Label();
        barraProgresso = new ProgressBar();
        painelResumo = new GroupBox();
        rotuloTempo = new Label();
        rotuloTempoTitulo = new Label();
        rotuloInvalidos = new Label();
        rotuloInvalidosTitulo = new Label();
        rotuloValidos = new Label();
        rotuloValidosTitulo = new Label();
        rotuloTotal = new Label();
        rotuloTotalTitulo = new Label();
        listaErros = new ListView();
        colunaLinha = new ColumnHeader();
        colunaCampo = new ColumnHeader();
        colunaRegra = new ColumnHeader();
        colunaMensagem = new ColumnHeader();
        colunaValorRaw = new ColumnHeader();
        botaoAbrirRelatorio = new Button();
        rotuloContagemErros = new Label();
        painelResumo.SuspendLayout();
        SuspendLayout();
        //
        // botaoSelecionarArquivo
        //
        botaoSelecionarArquivo.Location = new System.Drawing.Point(12, 12);
        botaoSelecionarArquivo.Name = "botaoSelecionarArquivo";
        botaoSelecionarArquivo.Size = new System.Drawing.Size(160, 30);
        botaoSelecionarArquivo.Text = "Selecionar arquivo...";
        botaoSelecionarArquivo.UseVisualStyleBackColor = true;
        botaoSelecionarArquivo.Click += BotaoSelecionarArquivo_Click;
        //
        // rotuloArquivoSelecionado
        //
        rotuloArquivoSelecionado.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        rotuloArquivoSelecionado.AutoEllipsis = true;
        rotuloArquivoSelecionado.Location = new System.Drawing.Point(184, 18);
        rotuloArquivoSelecionado.Name = "rotuloArquivoSelecionado";
        rotuloArquivoSelecionado.Size = new System.Drawing.Size(704, 20);
        rotuloArquivoSelecionado.Text = "(nenhum arquivo selecionado)";
        //
        // botaoValidar
        //
        botaoValidar.Enabled = false;
        botaoValidar.Location = new System.Drawing.Point(12, 50);
        botaoValidar.Name = "botaoValidar";
        botaoValidar.Size = new System.Drawing.Size(160, 30);
        botaoValidar.Text = "Validar";
        botaoValidar.UseVisualStyleBackColor = true;
        botaoValidar.Click += BotaoValidar_Click;
        //
        // rotuloStatus
        //
        rotuloStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        rotuloStatus.Location = new System.Drawing.Point(184, 57);
        rotuloStatus.Name = "rotuloStatus";
        rotuloStatus.Size = new System.Drawing.Size(704, 20);
        rotuloStatus.Text = "";
        //
        // barraProgresso
        //
        barraProgresso.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        barraProgresso.Location = new System.Drawing.Point(12, 90);
        barraProgresso.Name = "barraProgresso";
        barraProgresso.Size = new System.Drawing.Size(876, 20);
        barraProgresso.Style = ProgressBarStyle.Marquee;
        barraProgresso.Visible = false;
        //
        // painelResumo
        //
        painelResumo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        painelResumo.Controls.Add(rotuloTempo);
        painelResumo.Controls.Add(rotuloTempoTitulo);
        painelResumo.Controls.Add(rotuloInvalidos);
        painelResumo.Controls.Add(rotuloInvalidosTitulo);
        painelResumo.Controls.Add(rotuloValidos);
        painelResumo.Controls.Add(rotuloValidosTitulo);
        painelResumo.Controls.Add(rotuloTotal);
        painelResumo.Controls.Add(rotuloTotalTitulo);
        painelResumo.Location = new System.Drawing.Point(12, 120);
        painelResumo.Name = "painelResumo";
        painelResumo.Size = new System.Drawing.Size(876, 70);
        painelResumo.TabStop = false;
        painelResumo.Text = "Resumo";
        //
        // rotuloTempo
        //
        rotuloTempo.Location = new System.Drawing.Point(615, 30);
        rotuloTempo.Name = "rotuloTempo";
        rotuloTempo.Size = new System.Drawing.Size(100, 20);
        rotuloTempo.Text = "-";
        //
        // rotuloTempoTitulo
        //
        rotuloTempoTitulo.Location = new System.Drawing.Point(550, 30);
        rotuloTempoTitulo.Name = "rotuloTempoTitulo";
        rotuloTempoTitulo.Size = new System.Drawing.Size(60, 20);
        rotuloTempoTitulo.Text = "Tempo:";
        //
        // rotuloInvalidos
        //
        rotuloInvalidos.Location = new System.Drawing.Point(450, 30);
        rotuloInvalidos.Name = "rotuloInvalidos";
        rotuloInvalidos.Size = new System.Drawing.Size(90, 20);
        rotuloInvalidos.Text = "-";
        //
        // rotuloInvalidosTitulo
        //
        rotuloInvalidosTitulo.Location = new System.Drawing.Point(375, 30);
        rotuloInvalidosTitulo.Name = "rotuloInvalidosTitulo";
        rotuloInvalidosTitulo.Size = new System.Drawing.Size(70, 20);
        rotuloInvalidosTitulo.Text = "Inválidos:";
        //
        // rotuloValidos
        //
        rotuloValidos.Location = new System.Drawing.Point(275, 30);
        rotuloValidos.Name = "rotuloValidos";
        rotuloValidos.Size = new System.Drawing.Size(90, 20);
        rotuloValidos.Text = "-";
        //
        // rotuloValidosTitulo
        //
        rotuloValidosTitulo.Location = new System.Drawing.Point(200, 30);
        rotuloValidosTitulo.Name = "rotuloValidosTitulo";
        rotuloValidosTitulo.Size = new System.Drawing.Size(70, 20);
        rotuloValidosTitulo.Text = "Válidos:";
        //
        // rotuloTotal
        //
        rotuloTotal.Location = new System.Drawing.Point(100, 30);
        rotuloTotal.Name = "rotuloTotal";
        rotuloTotal.Size = new System.Drawing.Size(90, 20);
        rotuloTotal.Text = "-";
        //
        // rotuloTotalTitulo
        //
        rotuloTotalTitulo.Location = new System.Drawing.Point(15, 30);
        rotuloTotalTitulo.Name = "rotuloTotalTitulo";
        rotuloTotalTitulo.Size = new System.Drawing.Size(80, 20);
        rotuloTotalTitulo.Text = "Total lido:";
        //
        // listaErros
        //
        listaErros.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        listaErros.Columns.AddRange(new ColumnHeader[] { colunaLinha, colunaCampo, colunaRegra, colunaMensagem, colunaValorRaw });
        listaErros.FullRowSelect = true;
        listaErros.GridLines = true;
        listaErros.Location = new System.Drawing.Point(12, 220);
        listaErros.Name = "listaErros";
        listaErros.Size = new System.Drawing.Size(876, 380);
        listaErros.UseCompatibleStateImageBehavior = false;
        listaErros.View = View.Details;
        listaErros.VirtualMode = true;
        listaErros.RetrieveVirtualItem += ListaErros_RetrieveVirtualItem;
        //
        // colunaLinha
        //
        colunaLinha.Text = "Linha";
        colunaLinha.Width = 70;
        //
        // colunaCampo
        //
        colunaCampo.Text = "Campo";
        colunaCampo.Width = 150;
        //
        // colunaRegra
        //
        colunaRegra.Text = "Regra";
        colunaRegra.Width = 220;
        //
        // colunaMensagem
        //
        colunaMensagem.Text = "Mensagem";
        colunaMensagem.Width = 300;
        //
        // colunaValorRaw
        //
        colunaValorRaw.Text = "Valor bruto";
        colunaValorRaw.Width = 130;
        //
        // botaoAbrirRelatorio
        //
        botaoAbrirRelatorio.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        botaoAbrirRelatorio.Enabled = false;
        botaoAbrirRelatorio.Location = new System.Drawing.Point(12, 610);
        botaoAbrirRelatorio.Name = "botaoAbrirRelatorio";
        botaoAbrirRelatorio.Size = new System.Drawing.Size(220, 30);
        botaoAbrirRelatorio.Text = "Abrir relatório de erros completo";
        botaoAbrirRelatorio.UseVisualStyleBackColor = true;
        botaoAbrirRelatorio.Click += BotaoAbrirRelatorio_Click;
        //
        // rotuloContagemErros
        //
        rotuloContagemErros.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        rotuloContagemErros.Location = new System.Drawing.Point(250, 616);
        rotuloContagemErros.Name = "rotuloContagemErros";
        rotuloContagemErros.Size = new System.Drawing.Size(638, 20);
        rotuloContagemErros.Text = "";
        //
        // TelaPrincipal
        //
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(900, 652);
        Controls.Add(rotuloContagemErros);
        Controls.Add(botaoAbrirRelatorio);
        Controls.Add(listaErros);
        Controls.Add(painelResumo);
        Controls.Add(barraProgresso);
        Controls.Add(rotuloStatus);
        Controls.Add(botaoValidar);
        Controls.Add(rotuloArquivoSelecionado);
        Controls.Add(botaoSelecionarArquivo);
        MinimumSize = new System.Drawing.Size(700, 400);
        Name = "TelaPrincipal";
        Text = "LayoutValidator - Teste de Layout";
        painelResumo.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Button botaoSelecionarArquivo;
    private Label rotuloArquivoSelecionado;
    private Button botaoValidar;
    private Label rotuloStatus;
    private ProgressBar barraProgresso;
    private GroupBox painelResumo;
    private Label rotuloTotalTitulo;
    private Label rotuloTotal;
    private Label rotuloValidosTitulo;
    private Label rotuloValidos;
    private Label rotuloInvalidosTitulo;
    private Label rotuloInvalidos;
    private Label rotuloTempoTitulo;
    private Label rotuloTempo;
    private ListView listaErros;
    private ColumnHeader colunaLinha;
    private ColumnHeader colunaCampo;
    private ColumnHeader colunaRegra;
    private ColumnHeader colunaMensagem;
    private ColumnHeader colunaValorRaw;
    private Button botaoAbrirRelatorio;
    private Label rotuloContagemErros;
}
