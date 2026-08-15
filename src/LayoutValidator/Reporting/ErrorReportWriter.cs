using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using LayoutValidator.Core;

namespace LayoutValidator.Reporting;

/// <summary>
/// Escreve o relatório de erros linha a linha, na mesma iteração em que o
/// <see cref="LayoutValidationEngine"/> produz os resultados — nunca acumula os erros
/// inteiros em memória antes de gravar.
/// </summary>
public sealed class ErrorReportWriter : IDisposable
{
    private readonly CsvWriter _csv;
    private bool _cabecalhoEscrito;

    public ErrorReportWriter(TextWriter writer, CsvConfiguration? configuracaoCsv = null)
    {
        _csv = new CsvWriter(writer, configuracaoCsv ?? new CsvConfiguration(CultureInfo.InvariantCulture));
    }

    public int TotalErrorsWritten { get; private set; }

    public void Write<T>(ResultadoValidacaoRegistro<T> resultado)
    {
        if (resultado is not RegistroInvalido<T> invalido)
            return;

        if (!_cabecalhoEscrito)
        {
            _csv.WriteHeader<LinhaRelatorioErro>();
            _csv.NextRecord();
            _cabecalhoEscrito = true;
        }

        foreach (var erro in invalido.Erros)
        {
            _csv.WriteRecord(new LinhaRelatorioErro
            {
                NumeroLinha = erro.NumeroLinha,
                NomeCampo = erro.NomeCampo,
                ValorRaw = erro.ValorRaw,
                NomeRegra = erro.NomeRegra,
                Mensagem = erro.Mensagem
            });
            _csv.NextRecord();
            TotalErrorsWritten++;
        }
    }

    public void Dispose() => _csv.Dispose();
}
