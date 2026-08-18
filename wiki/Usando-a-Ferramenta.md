# Usando a Ferramenta

## 1. Referenciar a biblioteca

No seu projeto:

```bash
dotnet add reference caminho/para/src/LayoutValidator/LayoutValidator.csproj
```

(Enquanto a lib não for publicada como pacote NuGet interno, é referência de projeto
mesmo. Ver [Possibilidades](Possibilidades.md) sobre empacotar como NuGet.)

## 2. Ter um layout já declarado

Cada layout é um conjunto de 5 peças — `XxxRaw`, `XxxValidador`, `Xxx`, `XxxMapper`,
`XxxValidadorLayout`. Ver [Criando Layouts](Criando-Layouts.md) pra montar um do zero.
Os exemplos prontos no repo:

- `samples/LayoutValidator.Sample/Models/` — layout `Pessoa` (4 campos, simples).
- `apps/LayoutValidator.LayoutFuncionario/` — layout `Funcionario` (22 campos, mais
  representativo de um arquivo real).

## 3. Validar um arquivo — sem DI

Se seu projeto não usa injeção de dependência, dá pra chamar a engine direto:

```csharp
using var leitor = new StreamReader("arquivo.csv");
var opcoes = new OpcoesLayout();   // delimitador ';' com cabeçalho
var validador = new PessoaValidador();
var mapper = new PessoaMapper();

foreach (var resultado in LayoutValidationEngine.Validar(leitor, opcoes, validador, mapper))
{
    switch (resultado)
    {
        case RegistroValido<Pessoa> valido:
            // inserir valido.Registro no banco
            break;
        case RegistroInvalido<Pessoa> invalido:
            // logar invalido.Erros, invalido.NumeroLinha, invalido.ValoresRaw
            break;
    }
}
```

O formato do arquivo (delimitador e tratamento da primeira linha) é declarado **no layout**,
não aqui — ver [Declarando o formato do arquivo](Criando-Layouts.md#declarando-o-formato-do-arquivo).
O padrão é `;` com cabeçalho. Se o arquivo vier com outro delimitador, a leitura para na
primeira iteração com `LayoutIncompativelException` dizendo o que foi lido e o que era
esperado.

## 4. Validar um arquivo — com DI

Registra uma vez no startup:

```csharp
services.AddValidatorsFromAssemblyContaining<PessoaValidador>();
services.AddSingleton<ILayoutMapper<PessoaRaw, Pessoa>, PessoaMapper>();
services.AddScoped<IValidadorLayout<Pessoa>, PessoaValidadorLayout>();
```

E injeta onde precisar:

```csharp
public class ImportadorPessoas
{
    private readonly IValidadorLayout<Pessoa> _validadorLayout;

    public ImportadorPessoas(IValidadorLayout<Pessoa> validadorLayout) => _validadorLayout = validadorLayout;

    public void Importar(string caminhoArquivo)
    {
        using var leitor = new StreamReader(caminhoArquivo);
        foreach (var resultado in _validadorLayout.Validar(leitor))
        {
            // mesma lógica do exemplo acima
        }
    }
}
```

Note que quem consome `ImportadorPessoas` nunca vê `PessoaRaw` — é detalhe interno.

## 5. Lendo o resultado

- **Por registro**: `ResultadoValidacaoRegistro<T>` é ou `RegistroValido<T>` (tem
  `.Registro`, o Model final já tipado) ou `RegistroInvalido<T>` (tem `.ValoresRaw` —
  o que veio no arquivo — e `.Erros`, uma lista de `ErroValidacaoLayout` com
  `NumeroLinha`, `NomeCampo`, `ValorRaw`, `NomeRegra`, `Mensagem`).
- **Agregado**: acumule um `ResumoValidacaoLayout` chamando `.Registrar(resultado)` a
  cada iteração — dá total/válidos/inválidos e contagem por regra e por campo, sem
  guardar nada em memória além dos contadores.
- **Relatório em arquivo**: `ErrorReportWriter` escreve um CSV de erros (uma linha
  por erro) na mesma iteração, também sem buffer.

```csharp
var resumo = new ResumoValidacaoLayout();
using var relatorio = new ErrorReportWriter(new StreamWriter("erros.csv"));

foreach (var resultado in validadorLayout.Validar(leitor))
{
    resumo.Registrar(resultado);
    relatorio.Write(resultado);
}

Console.WriteLine($"{resumo.RegistrosInvalidos} de {resumo.TotalRegistros} registros com erro.");
```

## 6. Validando dados que já estão em memória (sem arquivo)

Quando o dado a validar já é o retorno de uma consulta (MySQL, Postgres, SQL Server) —
ou qualquer outra fonte que não seja um arquivo — não é preciso escrever nada em disco
nem criar uma fachada de layout (`XxxValidadorLayout`). `LayoutValidationEngine.Validar`
tem dois overloads pra isso, os dois **sempre posicionais** (sem conceito de cabeçalho —
a ordem dos valores tem que bater com a ordem de declaração das propriedades `string` do
Raw Model) e **sem `OpcoesLayout`**:

```csharp
using System.Data;

var tabela = new DataTable();
// ... tabela.Load(leitor) ou new SqlDataAdapter(comando).Fill(tabela) ...

var validador = new PessoaValidador();
var mapper = new PessoaMapper();

var linhas = tabela.Rows.Cast<DataRow>().Select(linha => (IReadOnlyList<string>) new[]
{
    linha["Nome"].ToString() ?? "",
    linha["Idade"].ToString() ?? "",
    // Quem monta a linha formata cada valor do jeito que o Validador/Mapper espera — a
    // engine não converte tipo nenhum. Uma coluna DateTime do banco já é sempre uma data
    // válida (garantida pelo próprio tipo); o formato aqui só precisa bater com o que o
    // PessoaMapper.Map vai fazer um DateTime.ParseExact em cima.
    ((DateTime) linha["DataNascimento"]).ToString("dd/MM/yyyy"),
    linha["Cpf"].ToString() ?? ""
});

var resumo = new ResumoValidacaoLayout();
var pessoasValidas = new List<Pessoa>();

foreach (var resultado in LayoutValidationEngine.Validar(linhas, validador, mapper))
{
    resumo.Registrar(resultado);
    if (resultado is RegistroValido<Pessoa> valido)
        pessoasValidas.Add(valido.Registro);
}
```

Se preferir montar uma linha de texto já delimitada em vez de separar os valores você
mesmo (por exemplo, reaproveitando um `CsvWriter` ou lógica que já existe), tem o
overload equivalente que recebe o delimitador:

```csharp
var linhas = new[] { "Maria;30;01/01/1994;12345678900" };

foreach (var resultado in LayoutValidationEngine.Validar(linhas, ";", validador, mapper))
{
    // mesma lógica de leitura do resultado
}
```

Dois pontos que vale ter em mente:

- **O delimitador desse overload não tem nenhum vínculo com o `OpcoesLayout.Delimitador`**
  de uma eventual fachada do mesmo layout usada pro caminho de arquivo — se os dois
  caminhos convivem no mesmo projeto com delimitador customizado, é responsabilidade de
  quem chama manter os dois consistentes.
- **`PessoaValidador`/`PessoaMapper` são os mesmos objetos que o layout CSV já usa** —
  só funciona bem quando os valores chegam já formatados exatamente como aquele
  `Validador`/`Mapper` espera. Uma regra de formato pensada pro texto de um arquivo
  (ex.: `.Data()` conferindo `dd/MM/yyyy`) só faz sentido aqui se você formatar o valor
  tipado do banco nesse mesmo padrão antes de montar a linha.

## 7. Testando na unha com o App de Teste

`apps/LayoutValidator.TesteApp` é um WinForms simples pra validar qualquer arquivo sem
escrever código: seleciona o `.csv`/`.txt`, clica em Validar, e mostra total lido,
tempo decorrido, válidos/inválidos e a lista de erros (com botão pra abrir o relatório
completo gerado ao lado do arquivo). Hoje ele está fixado no layout `Funcionario` — pra
testar outro layout, troca a injeção em
[`Program.cs`](../apps/LayoutValidator.TesteApp/Program.cs) pelo `IValidadorLayout<T>`
que quiser.

```bash
dotnet run --project apps/LayoutValidator.TesteApp/LayoutValidator.TesteApp.csproj
```

## 8. Gerando um arquivo de teste grande

`apps/LayoutValidator.GeradorDados` gera um CSV com o layout `Funcionario`, misturando
linhas válidas, com 1 erro, com 2 erros na mesma linha e linhas estruturalmente
quebradas (colunas faltando). Por padrão gera 1.000.000 de linhas em
`dados-teste/funcionarios_1000000.csv`, com seed fixa (reprodutível) e um
`resumo_geracao.txt` ao lado com as contagens exatas de cada categoria — útil pra
conferir se a validação está batendo com o que foi injetado de propósito.

```bash
dotnet run --project apps/LayoutValidator.GeradorDados/LayoutValidator.GeradorDados.csproj -- 500000 caminho/saida.csv
```

Os dois argumentos são opcionais: quantidade de linhas (default 1.000.000) e caminho de
saída (default `dados-teste/funcionarios_1000000.csv`).
