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

- Arquivos **delimitados** (CSV, pipe, etc.) — sem layout posicional/largura fixa — ou dados já
  em memória no mesmo formato (linhas com valores posicionais, com ou sem delimitador) pra quem
  não tem um arquivo pra validar — ver "Validando dados que já estão em memória" na
  [wiki](wiki/Usando-a-Ferramenta.md#6-validando-dados-que-já-estão-em-memória-sem-arquivo).
- Delimitador padrão é **`;`**, e o formato do arquivo (delimitador + tratamento da primeira
  linha) é declarado **no layout** — ver "Formato do arquivo" abaixo. (Não se aplica ao caminho
  de dados em memória, que é sempre posicional e não passa por `OpcoesLayout`.)
- **Encoding fora de escopo** por enquanto (maioria dos arquivos é UTF-8).
- Linha com qualquer erro é **descartada** do conjunto de válidos — só aparece no
  relatório de erros, com o valor raw original.
- Processamento em **streaming** — nunca carrega o arquivo inteiro em memória (arquivos
  esperados com potencialmente milhões de linhas).
- Validação de regras via **FluentValidation** (não `DataAnnotations`).
- Mapeamento `TRaw -> T` é **manual** por layout (sem geração automática via
  reflection nessa v1).
- Regras comuns (CPF, data, UF, moeda...) ficam num **pacote separado e opcional**,
  `LayoutValidator.Regras` — ver "Catálogo de regras" abaixo.
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

## Formato do arquivo: declarado no layout

Delimitador e tratamento da primeira linha são características **do arquivo que aquele layout
descreve**, iguais à lista de colunas. Ficam em `OpcoesLayout`, declarada na fachada do
layout — quem consome só chama `Validar(leitor)`, sem parâmetro de formato.

```csharp
public sealed class ProdutoValidadorLayout : ValidadorLayoutBase<ProdutoRaw, Produto>
{
    public ProdutoValidadorLayout(IValidator<ProdutoRaw> validador, ILayoutMapper<ProdutoRaw, Produto> mapper)
        : base(validador, mapper) { }

    protected override OpcoesLayout Opcoes { get; } = new()
    {
        Delimitador = "|",
        Cabecalho = ModoCabecalho.Ausente
    };
}
```

**O padrão é `;`** porque arquivo brasileiro usa vírgula como separador decimal: com vírgula
delimitadora, todo campo de valor (`1234,56`) colide e o arquivo precisa vir com aspas. O
próprio gerador de dados de teste quebrou por isso durante o desenvolvimento.

**`ModoCabecalho` tem três estados, não um booleano**, porque "tem cabeçalho" e "ignorar a
primeira linha" são coisas diferentes — a primeira decide se as colunas casam por nome ou por
posição, a segunda só descarta uma linha:

| Modo | 1ª linha vira registro? | Colunas casam por |
|---|---|---|
| `Presente` (padrão) | não | nome |
| `Ausente` | sim, é dado | posição |
| `PresenteIgnorado` | não | posição |

Nos modos posicionais a **ordem de declaração das propriedades do Raw Model é o contrato**, e
a falha é silenciosa: trocar duas de lugar não gera erro, só põe os dados nos campos errados.
`[Index(n)]` do CsvHelper serve pra tornar isso explícito quando valer a pena.

**Não existe detecção automática de delimitador** de propósito: se o layout é o contrato do
formato, a ferramenta adivinhar contradiz isso — e adivinhar errado num arquivo de milhões de
linhas produz um resultado plausível e errado, que é pior que uma falha. Arquivo com
delimitador diferente do declarado para na primeira iteração com `LayoutIncompativelException`,
citando o cabeçalho lido e o delimitador esperado.

`ValidadorLayoutBase<TRaw, T>` existe porque a fachada de todo layout era o mesmo cerimonial
— dois campos, um construtor que só atribui, um `Validar` que repassa. `IValidadorLayout<T>`
continua sendo a interface pública injetada pelo código de negócio; herdar da base é
conveniência, não obrigação.

## Catálogo de regras: por que fora do core

`src/LayoutValidator.Regras` é um pacote à parte que referencia **só o FluentValidation** —
não referencia o core. Isso é deliberado: o core é a ferramenta genérica (motor + contratos),
empacotável sozinha, e regras como "CPF tem dígito verificador" são vocabulário de domínio,
não infraestrutura de leitura de arquivo.

O mecanismo é **extension method sobre `IRuleBuilder<T, string>`** — a única forma aberta por
natureza: não há interface a implementar, classe a herdar nem registro em DI. Que o pacote de
regras não precise do core é justamente a prova disso: **qualquer projeto consumidor escreve
as próprias regras exatamente do mesmo jeito**, e elas aparecem no `RuleFor` lado a lado com
as do catálogo. O passo a passo está em
[Regras Reutilizáveis](wiki/Regras-Reutilizaveis.md#criando-a-sua-própria-biblioteca-de-regras).

Cada regra é uma casca fina sobre um **predicado puro** (`bool`, sem FluentValidation) que
vive em `Predicados/` e pode ser usado fora de validação — num `Mapper`, por exemplo. Todo
predicado é **total**: nunca lança exceção. Isso não é preciosismo — numa engine de streaming,
exceção dentro de uma regra não reprova a linha, ela aborta a leitura do arquivo inteiro.

Duas convenções do catálogo mudam como o validador é escrito:

- **Regra de formato não reprova valor vazio.** `Obrigatorio()` é a única que reclama de
  campo em branco, então campo opcional é só não declarar obrigatório — some o
  `Must(valor => string.IsNullOrEmpty(valor) || ...)` repetido.
- **`RuleLevelCascadeMode = CascadeMode.Stop` em cada validador de layout.** O default do
  FluentValidation é `Continue`, então encadear `.Inteiro().InteiroEntre(1, 60)` num valor
  `"abc"` produziria dois erros pra mesma célula — duas linhas no relatório e contagem
  dobrada no `ErrosPorRegra`. `Stop` no nível de regra para dentro de um campo e segue nos
  demais.

```csharp
RuleLevelCascadeMode = CascadeMode.Stop;

RuleFor(f => f.Cpf).Obrigatorio().Cpf();
RuleFor(f => f.Uf).Obrigatorio().Uf();
RuleFor(f => f.DataDemissao).Data();   // opcional: vazio passa
```

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
- **`XxxValidadorLayout : ValidadorLayoutBase<XxxRaw, Xxx>`** — uma classe concreta por
  layout, que herda a composição de `IValidator<XxxRaw>` + `ILayoutMapper<XxxRaw,Xxx>` e a
  chamada do `LayoutValidationEngine`, e declara o formato do arquivo em `Opcoes`. `TRaw`
  nunca vaza pra fora dessa classe.
- **Registro no DI**: os `AbstractValidator<T>` entram via scan automático do
  FluentValidation (`services.AddValidatorsFromAssemblyContaining<XxxValidador>()`);
  mapper e `IValidadorLayout<T>` são registrados explicitamente por layout, porque não
  dá pra genérico-aberto automático (cada layout tem seu próprio par `TRaw`/`T`).

```csharp
services.AddValidatorsFromAssemblyContaining<PessoaValidador>();
services.AddSingleton<ILayoutMapper<PessoaRaw, Pessoa>, PessoaMapper>();
services.AddScoped<IValidadorLayout<Pessoa>, PessoaValidadorLayout>();
```

Ver [`ServiceCollectionExtensions.cs`](samples/LayoutValidator.Sample/ServiceCollectionExtensions.cs)
no sample.

## Estrutura do projeto

```
LayoutValidator.sln
  src/LayoutValidator/                     biblioteca core (net8.0)
    Core/ResultadoValidacaoRegistro.cs     RegistroValido<T> / RegistroInvalido<T>
    Core/ErroValidacaoLayout.cs
    Core/ResumoValidacaoLayout.cs
    Core/LayoutValidationEngine.cs         engine de streaming (Validar<TRaw,T>)
    Core/OpcoesLayout.cs                   formato do arquivo (delimitador ';' + cabeçalho)
    Core/ModoCabecalho.cs                  Presente / Ausente / PresenteIgnorado
    Core/ValidadorLayoutBase.cs            base das fachadas de layout
    Core/LayoutIncompativelException.cs    delimitador não bate: falha rápida
    Core/IValidadorLayout.cs
    Core/ILayoutMapper.cs
    Reporting/ErrorReportWriter.cs         escreve o relatório de erros linha a linha
    Reporting/LinhaRelatorioErro.cs
  src/LayoutValidator.Regras/              catálogo de regras (net8.0, só FluentValidation)
    Predicados/Formatos.cs                 data, inteiro, decimal, moeda — bool puro
    Predicados/Documentos.cs               CPF, CNPJ, CNH, PIS/PASEP, Luhn — bool puro
    Predicados/UnidadesFederativas.cs      as 27 siglas
    ConstrutorRegra.cs                     aplica o contrato "formato não reprova vazio"
    Comuns/*Extensions.cs                  texto, numéricas, data, financeiras, contato
    Brasil/RegrasBrasilExtensions.cs       Cpf, Cnpj, Cep, Uf, Telefone, Cnh, PisPasep
  samples/LayoutValidator.Sample/          console app demonstrando o layout "Pessoa"
    Models/PessoaRaw.cs / Pessoa.cs / PessoaValidador.cs / PessoaMapper.cs / PessoaValidadorLayout.cs
    ServiceCollectionExtensions.cs         registro no DI
    dados_exemplo.csv                      CSV de exemplo (válidos + 5 tipos de erro)
    Program.cs
  tests/LayoutValidator.Tests/             xUnit do motor
    Modelos/                               layout de teste isolado do sample e do catálogo de regras
    Fixtures/*.csv                         valido, invalido_inteiro, invalido_data, linhas_malformadas
    LayoutValidationEngineTestes.cs
    ResumoValidacaoLayoutTestes.cs
  tests/LayoutValidator.Regras.Tests/      xUnit do catálogo
    Predicados/                            vetores de teste dos predicados puros
    Extensions/                            contrato de vazio, códigos de erro, CascadeMode
  apps/LayoutValidator.LayoutFuncionario/  layout de referência maior (22 campos), compartilhado pelos apps abaixo
  apps/LayoutValidator.GeradorDados/       console app: gera CSV de teste com erros diversos injetados
  apps/LayoutValidator.TesteApp/           WinForms: seleciona um arquivo e mostra o resultado da validação
  apps/LayoutValidator.Api/                cadastro de layouts em banco + API local de validacao (ADR-0002)
    Modelos/                               entidades EF Core (LayoutCadastrado, CampoCadastrado, RegraCampoCadastrada)
    Regras/                                catalogo de regras cadastraveis por chave (equivalente dinamico de LayoutValidator.Regras)
    Dados/                                 ApiDbContext + migrations (SQLite)
    Validacao/                             DivisorDeLinha, AvaliadorDeCampo, ValidadorDeDefinicaoDeLayout
    Contratos/                             DTOs de request/response + MapeadorDeLayout
    Endpoints/                             LayoutsEndpoints, RegrasEndpoints, ValidacaoEndpoints
  tests/LayoutValidator.Api.Tests/         xUnit do app de cadastro (unidade + integracao via WebApplicationFactory)
  dados-teste/                             saída do gerador (não versionado — ver .gitignore)
  wiki/                                    guia de uso, como criar layouts, possibilidades futuras
```

## Como adicionar um novo layout

1. Criar `XxxRaw` — uma classe com todas as propriedades `string`, nomes batendo com
   o cabeçalho do CSV (o CsvHelper casa por nome, case-insensitive, sem atributo
   nenhum sendo necessário no caso comum).
2. Criar `XxxValidador : AbstractValidator<XxxRaw>` com as regras de formato,
   obrigatoriedade, etc. — usando o [catálogo de regras](wiki/Regras-Reutilizaveis.md)
   e declarando `RuleLevelCascadeMode = CascadeMode.Stop` no construtor.
3. Criar `Xxx` — o Model final já tipado.
4. Criar `XxxMapper : ILayoutMapper<XxxRaw, Xxx>` convertendo os campos.
5. Criar `XxxValidadorLayout : ValidadorLayoutBase<XxxRaw, Xxx>` — só o construtor, mais o
   `Opcoes` se o arquivo não for `;` com cabeçalho.
6. Registrar no DI: `AddValidatorsFromAssemblyContaining<XxxValidador>()` +
   `AddSingleton<ILayoutMapper<XxxRaw,Xxx>, XxxMapper>()` +
   `AddScoped<IValidadorLayout<Xxx>, XxxValidadorLayout>()`.

O layout `Pessoa` em `samples/LayoutValidator.Sample/Models/` é o exemplo de
referência completo desse padrão.

## Rodando

```bash
dotnet test LayoutValidator.sln
dotnet run --project samples/LayoutValidator.Sample/LayoutValidator.Sample.csproj
dotnet run --project apps/LayoutValidator.Api/LayoutValidator.Api.csproj
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
