using LayoutValidator.Core;
using LayoutValidator.Reporting;
using LayoutValidator.Sample;
using LayoutValidator.Sample.Models;
using Microsoft.Extensions.DependencyInjection;

var servicos = new ServiceCollection()
    .AdicionarValidadorLayoutPessoa()
    .BuildServiceProvider();

var validadorLayout = servicos.GetRequiredService<IValidadorLayout<Pessoa>>();

var caminhoEntrada = Path.Combine(AppContext.BaseDirectory, "dados_exemplo.csv");
var caminhoRelatorio = Path.Combine(AppContext.BaseDirectory, "relatorio_erros.csv");

var resumo = new ResumoValidacaoLayout();
var pessoasValidas = new List<Pessoa>();

using (var leitorArquivo = new StreamReader(caminhoEntrada))
using (var relatorio = new ErrorReportWriter(new StreamWriter(caminhoRelatorio)))
{
    foreach (var resultado in validadorLayout.Validar(leitorArquivo))
    {
        resumo.Registrar(resultado);
        relatorio.Write(resultado);

        if (resultado is RegistroValido<Pessoa> valido)
            pessoasValidas.Add(valido.Registro);
    }
}

Console.WriteLine($"Total de registros:   {resumo.TotalRegistros}");
Console.WriteLine($"Registros válidos:    {resumo.RegistrosValidos}");
Console.WriteLine($"Registros inválidos:  {resumo.RegistrosInvalidos}");
Console.WriteLine();
Console.WriteLine("Erros por regra:");
foreach (var (regra, quantidade) in resumo.ErrosPorRegra)
    Console.WriteLine($"  {regra}: {quantidade}");
Console.WriteLine();
Console.WriteLine("Erros por campo:");
foreach (var (campo, quantidade) in resumo.ErrosPorCampo)
    Console.WriteLine($"  {campo}: {quantidade}");
Console.WriteLine();
Console.WriteLine($"Pessoas válidas prontas para carga: {pessoasValidas.Count}");
Console.WriteLine($"Relatório de erros gerado em: {caminhoRelatorio}");
