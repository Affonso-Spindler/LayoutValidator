# LayoutValidator

Biblioteca C# (.NET 8) para validar arquivos delimitados (CSV, pipe, etc.) contra um
layout declarado, antes de qualquer carga em banco. Detecta campo fora do formato
esperado (data, inteiro, regex, etc.) e devolve um relatório de quais linhas/campos
estão fora da especificação — sem precisar chegar no `COPY` do banco pra descobrir isso.

Pra guia de uso e tutoriais, ver a [wiki](wiki/Home.md). Este README fica focado nas
decisões de arquitetura.

## Contexto / motivação

Hoje não existe validação de layout antes da carga — o único sinal de "arquivo fora do
padrão" é o erro do `COPY` no momento em que o arquivo já está sendo inserido no banco.
Esta biblioteca existe para pegar isso antes, de forma reutilizável entre projetos.

## Decisões de escopo (v1)

- Só arquivos **delimitados** (CSV, pipe, etc.) — sem layout posicional/largura fixa.
- **Encoding fora de escopo** por enquanto (maioria dos arquivos é UTF-8).
- Linha com qualquer erro é **descartada** do conjunto de válidos — só aparece no
  relatório de erros, com o valor raw original.
- Processamento em **streaming** — nunca carrega o arquivo inteiro em memória (arquivos
  esperados com potencialmente milhões de linhas).
- Validação de regras via **FluentValidation** (não `DataAnnotations`).
- Mapeamento `TRaw -> T` é **manual** por layout (sem geração automática via
  reflection nessa v1).
- Nomes de classes/métodos/propriedades em **pt-BR**, exceto papéis técnicos de
  infraestrutura (`Raw`, `Mapper`, `Engine`, `Writer`) que ficam em inglês — ver
  "Convenção de nomenclatura" abaixo.

## Convenção de nomenclatura: domínio em pt-BR, papel técnico em inglês

Vocabulário de **domínio** (o que o layout representa — `Pessoa`, `Funcionario`,
`NumeroLinha`, `Mensagem`) e a família **Validador/Validar** ficam em português.
Papéis técnicos genéricos de infraestrutura do padrão — que o mercado .NET já
convencionou em inglês mesmo em conversa em português ("dado raw", "o mapper") — ficam
em inglês:

| Papel | Nome |
|---|---|
| Dado ainda não convertido (todas as props `string`) | `Raw` (`PessoaRaw`, `TRaw`, `ValorRaw`) |
| Converte `TRaw -> T` | `Mapper` (`ILayoutMapper<TRaw,T>`, `PessoaMapper`, método `Map`) |
| Motor de streaming que lê+valida | `Engine` (`LayoutValidationEngine`) |
| Escreve o relatório de erros | `Writer` (`ErrorReportWriter`, método `Write`) |

Quando o papel é a "cabeça" de um nome composto, o nome segue a ordem gramatical do
inglês (`LayoutValidationEngine`, não `EngineValidacaoLayout`); como sufixo de tipo
concreto, a ordem português+sufixo já é natural também em inglês (`PessoaMapper`,
`FuncionarioRaw`).

## Por que dois Models por layout

A ideia original era decorar o Model final (já tipado: `int`, `DateTime`) com
atributos. Isso não combina com FluentValidation (que valida via classe
`AbstractValidator<T>`, não atributos) nem com o requisito de "descartar a linha
inválida sem quebrar a leitura": se o CsvHelper tentasse converter direto pra
`int`/`DateTime` durante a leitura, uma célula inválida já lançaria
`TypeConverterException` e abortaria a leitura inteira antes de qualquer validação
rodar.

Por isso cada layout tem **três peças**:

1. **`XxxRaw`** — todas as propriedades como `string`, mapeadas 1:1 nas colunas pelo
   CsvHelper. Nunca falha na leitura em si, porque não há conversão de tipo envolvida.
2. **`XxxValidador : AbstractValidator<XxxRaw>`** — regras do FluentValidation sobre
   as strings raw (`.Must(...)`, `.Matches(...)`, `.Length(...)` etc.).
3. **`XxxMapper : ILayoutMapper<XxxRaw, Xxx>`** — converte pro Model final tipado, só
   chamado depois que a validação passou (conversão garantida segura nesse ponto).

Isso mantém "declaro as regras perto do layout", só que a declaração migra de atributos
no Model final para uma classe de validator ao lado do Raw Model. Ganha de brinde:
regras cross-field, mensagens customizadas e reuso de validators entre layouts.

## Arquitetura de streaming

O núcleo (`LayoutValidationEngine.Validar<TRaw, T>`) é um iterador preguiçoso
(`yield return`) que lê o CSV linha a linha via `CsvHelper.CsvReader`, valida cada
linha com o `IValidator<TRaw>` do FluentValidation, e entrega um
`ResultadoValidacaoRegistro<T>` por linha — `RegistroValido<T>` ou `RegistroInvalido<T>`
(este último com os valores raw originais e a lista de `ErroValidacaoLayout`).

Quem chama itera esse `IEnumerable` com `foreach` e decide o que fazer a cada linha —
por exemplo, inserir os válidos em lote no banco e escrever os inválidos no relatório,
tudo na mesma passada. Nada fica retido em memória além da linha corrente.

```csharp
using var leitor = new StreamReader("arquivo.csv");
using var relatorio = new ErrorReportWriter(new StreamWriter("erros.csv"));
var resumo = new ResumoValidacaoLayout();

foreach (var resultado in validadorLayout.Validar(leitor))
{
    resumo.Registrar(resultado);
    relatorio.Write(resultado);

    if (resultado is RegistroValido<Pessoa> valido)
    {
        // inserir valido.Registro no banco
    }
}
```

O `ResumoValidacaoLayout` também é acumulado incrementalmente durante a mesma
iteração (não é um passe separado) — expõe `TotalRegistros`, `RegistrosValidos`,
`RegistrosInvalidos`, `ErrosPorRegra` e `ErrosPorCampo`.

## Linhas estruturalmente quebradas

Quebra de linha dentro de um campo **entre aspas** (padrão RFC4180) o CsvHelper trata
certo — não é problema. O caso tratado explicitamente é quebra de linha **solta** (sem
aspas) ou número de colunas errado numa linha: por padrão o `CsvReader` lançaria
exceção e abortaria a leitura inteira.

Tratamento: `LayoutValidationEngine` configura `BadDataFound` e `MissingFieldFound`
como `null` (não lançar) e compara o número de colunas de cada linha física contra o
cabeçalho. Se não bater, a linha vira um `RegistroInvalido<T>` com
`NomeRegra = "EstruturaDeColunas"` (sem nem rodar o FluentValidation nela) e a leitura
**continua** pra próxima linha.

Limitação conhecida: o número de linha reportado é onde o parser *percebeu* o
problema — pode não ser exatamente a origem do defeito quando uma quebra de linha
solta empurrou o desalinhamento. Não há re-sincronização/heurística nessa v1.

## Injeção de dependência

O código consumidor só conhece o Model final (`T`) — o Raw Model (`TRaw`) é detalhe
interno de como aquele layout é parseado/validado.

- **`IValidadorLayout<T>`** — interface pública (`Validar(TextReader) -> IEnumerable<ResultadoValidacaoRegistro<T>>`).
  É isso que código de negócio injeta via construtor.
- **`XxxValidadorLayout : IValidadorLayout<Xxx>`** — uma classe concreta por layout,
  compõe `IValidator<XxxRaw>` + `ILayoutMapper<XxxRaw,Xxx>` e chama o
  `LayoutValidationEngine` genérico por dentro. `TRaw` nunca vaza pra fora dessa classe.
- **Registro no DI**: os `AbstractValidator<T>` entram via scan automático do
  FluentValidation (`services.AddValidatorsFromAssemblyContaining<XxxValidador>()`);
  mapper e `IValidadorLayout<T>` são registrados explicitamente por layout, porque não
  dá pra genérico-aberto automático (cada layout tem seu próprio par `TRaw`/`T`).

```csharp
services.AddValidatorsFromAssemblyContaining<PessoaValidador>();
services.AddSingleton<ILayoutMapper<PessoaRaw, Pessoa>, PessoaMapper>();
services.AddScoped<IValidadorLayout<Pessoa>, PessoaValidadorLayout>();
```

Ver [`ExtensoesColecaoServicos.cs`](samples/LayoutValidator.Sample/ExtensoesColecaoServicos.cs)
no sample.

## Estrutura do projeto

```
LayoutValidator.sln
  src/LayoutValidator/                     biblioteca core (net8.0)
    Core/ResultadoValidacaoRegistro.cs     RegistroValido<T> / RegistroInvalido<T>
    Core/ErroValidacaoLayout.cs
    Core/ResumoValidacaoLayout.cs
    Core/LayoutValidationEngine.cs         engine de streaming (Validar<TRaw,T>)
    Core/IValidadorLayout.cs
    Core/ILayoutMapper.cs
    Reporting/ErrorReportWriter.cs         escreve o relatório de erros linha a linha
    Reporting/LinhaRelatorioErro.cs
  samples/LayoutValidator.Sample/          console app demonstrando o layout "Pessoa"
    Models/PessoaRaw.cs / Pessoa.cs / PessoaValidador.cs / PessoaMapper.cs / PessoaValidadorLayout.cs
    ExtensoesColecaoServicos.cs            registro no DI
    dados_exemplo.csv                      CSV de exemplo (válidos + 4 tipos de erro)
    Program.cs
  tests/LayoutValidator.Tests/             xUnit
    Modelos/                               layout de teste isolado do sample
    Fixtures/*.csv                         valido, invalido_inteiro, invalido_data, linhas_malformadas
    LayoutValidationEngineTestes.cs
    ResumoValidacaoLayoutTestes.cs
  apps/LayoutValidator.LayoutFuncionario/  layout de referência maior (22 campos), compartilhado pelos apps abaixo
  apps/LayoutValidator.GeradorDados/       console app: gera CSV de teste com erros diversos injetados
  apps/LayoutValidator.TesteApp/           WinForms: seleciona um arquivo e mostra o resultado da validação
  dados-teste/                             saída do gerador (não versionado — ver .gitignore)
  wiki/                                    guia de uso, como criar layouts, possibilidades futuras
```

## Como adicionar um novo layout

1. Criar `XxxRaw` — uma classe com todas as propriedades `string`, nomes batendo com
   o cabeçalho do CSV (o CsvHelper casa por nome, case-insensitive, sem atributo
   nenhum sendo necessário no caso comum).
2. Criar `XxxValidador : AbstractValidator<XxxRaw>` com as regras de formato,
   obrigatoriedade, etc.
3. Criar `Xxx` — o Model final já tipado.
4. Criar `XxxMapper : ILayoutMapper<XxxRaw, Xxx>` convertendo os campos.
5. Criar `XxxValidadorLayout : IValidadorLayout<Xxx>` compondo os três acima com o
   `LayoutValidationEngine.Validar`.
6. Registrar no DI: `AddValidatorsFromAssemblyContaining<XxxValidador>()` +
   `AddSingleton<ILayoutMapper<XxxRaw,Xxx>, XxxMapper>()` +
   `AddScoped<IValidadorLayout<Xxx>, XxxValidadorLayout>()`.

O layout `Pessoa` em `samples/LayoutValidator.Sample/Models/` é o exemplo de
referência completo desse padrão.

## Rodando

```bash
dotnet test tests/LayoutValidator.Tests/LayoutValidator.Tests.csproj
dotnet run --project samples/LayoutValidator.Sample/LayoutValidator.Sample.csproj
```

O sample imprime o resumo (total/válidos/inválidos, erros por regra e por campo) e
gera `relatorio_erros.csv` ao lado do executável.

## Pontos em aberto para o futuro

- **Encoding**: fora de escopo nessa v1 (maioria dos arquivos é UTF-8). Se algum dia
  virar problema, precisa ser validado no nível de bytes crus **antes** do CsvHelper
  decodificar o stream — depois de decodificado com o encoding errado, os caracteres
  corrompidos já viraram `?`/lixo e o erro original se perde.
- **Layout posicional/largura fixa**: não suportado, só delimitado.
- **Async (`IAsyncEnumerable`)**: extensão natural depois que a versão síncrona estiver
  validada em produção — não necessária agora porque leitura de arquivo local linha a
  linha já é rápida o suficiente de forma síncrona.
- **Mapper automático via reflection**: começar manual foi decisão consciente (mais
  simples e explícito); pode valer a pena mais adiante se o número de layouts crescer
  muito.
