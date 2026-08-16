# Criando Layouts

Um layout são 5 peças pequenas. Nenhuma delas é complicada sozinha — a única coisa que
exige atenção é manter os nomes de propriedade batendo entre elas (CsvHelper casa
coluna do CSV com propriedade do `XxxRaw` pelo nome, case-insensitive, sem precisar
de atributo).

Vamos criar um layout `Produto` do zero como exemplo: `Codigo`, `Descricao`, `Preco`,
`Categoria`, `EstoqueMinimo`.

## 1. `ProdutoRaw` — tudo string

```csharp
public sealed class ProdutoRaw
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Preco { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string EstoqueMinimo { get; set; } = string.Empty;
}
```

Por quê tudo string: se alguma propriedade já fosse `int`/`decimal`/`DateTime`, o
CsvHelper tentaria converter no momento da leitura e uma célula ruim já lançaria
exceção **antes** de qualquer regra rodar — quebrando o streaming pro arquivo inteiro.
Ver o README na raiz pra mais detalhes desse porquê.

## 2. `ProdutoValidador` — as regras

```csharp
using FluentValidation;
using LayoutValidator.Regras;

public sealed class ProdutoValidador : AbstractValidator<ProdutoRaw>
{
    public ProdutoValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(p => p.Codigo)
            .Obrigatorio()
            .Formato(@"^[A-Z]{3}\d{4}$", "CodigoFormatoInvalido", "'{PropertyName}' deve seguir o padrão AAA0000.");

        RuleFor(p => p.Descricao).Obrigatorio();

        RuleFor(p => p.Preco).Obrigatorio().DecimalPositivo();

        RuleFor(p => p.EstoqueMinimo).Obrigatorio().InteiroNaoNegativo();
    }
}
```

As regras vêm do pacote [`LayoutValidator.Regras`](Regras-Reutilizaveis.md) — só adicionar
a `ProjectReference` e o `using LayoutValidator.Regras`. Não precisa reescrever regex de CPF
nem `TryParse` de data em cada layout; o catálogo completo está na
[página de regras reutilizáveis](Regras-Reutilizaveis.md), junto de como escrever as suas
próprias quando o catálogo não cobrir.

Duas convenções do catálogo que valem repetir aqui, porque mudam como você escreve o
validador:

**`Obrigatorio()` é a única regra que reprova campo vazio.** Todas as outras deixam vazio
passar, então campo opcional é só não declarar obrigatório:

```csharp
RuleFor(p => p.DataDescontinuacao).Data();               // opcional
RuleFor(p => p.DataCadastro).Obrigatorio().Data();       // obrigatório
```

**`RuleLevelCascadeMode = CascadeMode.Stop` no construtor.** Sem isso, encadear duas regras
no mesmo campo faz as duas rodarem mesmo depois de uma falhar, e uma célula ruim vira dois
erros — duas linhas no relatório e contagem dobrada no `ErrosPorRegra`. O porquê detalhado
está [na página de regras](Regras-Reutilizaveis.md#2-um-erro-por-campo-declare-o-cascademode).

Se precisar de uma regra que o catálogo não tem e não vale generalizar, `Formato(regex,
codigo, mensagem)` é a saída — como no `Codigo` acima. O código de erro que você passar é o
que vira `NomeRegra` no `ErroValidacaoLayout` e o que o `ResumoValidacaoLayout` agrupa em
`ErrosPorRegra`, então escolha um nome estável.

## 3. `Produto` — o Model final

```csharp
public sealed record Produto
{
    public required string Codigo { get; init; }
    public required string Descricao { get; init; }
    public required decimal Preco { get; init; }
    public required string Categoria { get; init; }
    public required int EstoqueMinimo { get; init; }
}
```

## 4. `ProdutoMapper` — a conversão

Só roda depois que a validação passou, então a conversão é garantida segura:

```csharp
public sealed class ProdutoMapper : ILayoutMapper<ProdutoRaw, Produto>
{
    public Produto Map(ProdutoRaw raw) => new()
    {
        Codigo = raw.Codigo,
        Descricao = raw.Descricao,
        Preco = decimal.Parse(raw.Preco.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture),
        Categoria = raw.Categoria,
        EstoqueMinimo = int.Parse(raw.EstoqueMinimo)
    };
}
```

## 5. `ProdutoValidadorLayout` — a fachada

```csharp
public sealed class ProdutoValidadorLayout : ValidadorLayoutBase<ProdutoRaw, Produto>
{
    public ProdutoValidadorLayout(IValidator<ProdutoRaw> validador, ILayoutMapper<ProdutoRaw, Produto> mapper)
        : base(validador, mapper)
    {
    }
}
```

É só isso quando o arquivo é do formato padrão — delimitador `;` com cabeçalho. Se o seu
arquivo for diferente, é aqui que você declara: ver
[Declarando o formato do arquivo](#declarando-o-formato-do-arquivo) logo abaixo.

`ValidadorLayoutBase` guarda o validador e o mapper e chama o `LayoutValidationEngine` por
dentro. Herdar é conveniência: `IValidadorLayout<Produto>` continua sendo a interface que o
código de negócio injeta, e quem precisar de algo fora do comum implementa ela direto.

## 6. Registrar no DI

```csharp
services.AddValidatorsFromAssemblyContaining<ProdutoValidador>();
services.AddSingleton<ILayoutMapper<ProdutoRaw, Produto>, ProdutoMapper>();
services.AddScoped<IValidadorLayout<Produto>, ProdutoValidadorLayout>();
```

Pronto — o layout `Produto` está utilizável exatamente como o `Pessoa` e o
`Funcionario` descritos em [Usando a Ferramenta](Usando-a-Ferramenta.md).

## Declarando o formato do arquivo

Delimitador e tratamento da primeira linha são características **do arquivo que o layout
descreve**, iguais à lista de colunas — então ficam declarados no layout, não em quem
consome. Quem consome só chama `Validar(leitor)`.

O padrão é delimitador `;` com cabeçalho. Pra fugir disso, sobrescreva `Opcoes` na fachada:

```csharp
public sealed class ProdutoValidadorLayout : ValidadorLayoutBase<ProdutoRaw, Produto>
{
    public ProdutoValidadorLayout(IValidator<ProdutoRaw> validador, ILayoutMapper<ProdutoRaw, Produto> mapper)
        : base(validador, mapper)
    {
    }

    protected override OpcoesLayout Opcoes { get; } = new()
    {
        Delimitador = "|",
        Cabecalho = ModoCabecalho.Ausente
    };
}
```

### Os três modos de cabeçalho

"Tem cabeçalho" e "ignorar a primeira linha" são coisas diferentes: a primeira decide **como
as colunas casam**, a segunda só descarta uma linha. Por isso três modos, não um booleano.

| Modo | 1ª linha vira registro? | Como as colunas casam |
|---|---|---|
| `Presente` (padrão) | não | pelo **nome** da coluna |
| `Ausente` | **sim**, é dado | pela **posição** |
| `PresenteIgnorado` | não | pela **posição** |

Em `Presente` a primeira linha é consumida como cabeçalho: ela não passa pelo validador nem
pelo mapper e não entra no `TotalRegistros`. `PresenteIgnorado` é pro arquivo que *tem*
cabeçalho, mas cujos nomes não batem com as suas propriedades — a linha é descartada do mesmo
jeito e o casamento vira posicional.

### Sem cabeçalho, a ordem das propriedades é o contrato

Nos modos `Ausente` e `PresenteIgnorado` o CsvHelper casa coluna com propriedade **por
posição**, na ordem em que as propriedades estão declaradas no `XxxRaw`. Ou seja: a ordem das
propriedades tem que bater com a ordem das colunas do arquivo.

**Isso falha em silêncio.** Trocar duas propriedades de lugar não gera erro estrutural nenhum
— os dados só entram nos campos errados:

```
arquivo:    111;Maria;01/01/1994

// declarado Codigo, Nome, DataNascimento:
Codigo=111      Nome=Maria        DataNascimento=01/01/1994   ✅

// declarado DataNascimento, Codigo, Nome — mesmo arquivo:
Codigo=Maria    Nome=01/01/1994   DataNascimento=111          ❌ sem erro nenhum
```

Se quiser tornar a ordem explícita e imune a isso, dá pra anotar com `[Index(n)]` do
CsvHelper — não é obrigatório, mas em layout posicional com muitos campos vale:

```csharp
public sealed class ProdutoRaw
{
    [Index(0)] public string Codigo { get; set; } = string.Empty;
    [Index(1)] public string Descricao { get; set; } = string.Empty;
}
```

### Delimitador errado falha rápido

Se o arquivo vier com um delimitador diferente do declarado, a leitura para na primeira
iteração com `LayoutIncompativelException`, dizendo o que foi lido e o que era esperado:

```
Nenhuma coluna do cabeçalho casou com o layout 'FuncionarioRaw'. Delimitador esperado: ';'.
Cabeçalho lido em 1 coluna(s): 'MatriculaId,Nome,Cpf,Rg,...'. Confira se o arquivo usa outro
delimitador.
```

Falha rápida em vez de um registro inválido por linha, porque nesse caso o arquivo inteiro é
inaproveitável — um milhão de registros inválidos seria ruído, não informação. Não existe
detecção automática de delimitador de propósito: adivinhar errado produziria um resultado
plausível e errado, que é pior que uma falha.

### Por que `;` é o padrão

Arquivo brasileiro usa vírgula como separador decimal. Com vírgula delimitadora, todo campo
de valor (`1234,56`) colide com o delimitador e **o arquivo precisa vir com aspas**, senão
vira duas colunas e a linha desalinha. O gerador em `apps/LayoutValidator.GeradorDados` passou
exatamente por esse bug durante o desenvolvimento — `Salario` sem aspas quebrando a contagem
de colunas de quase todo o arquivo.

Com `;` o problema não existe: `1234,56` não tem nada de especial. Se você trocar o
`Delimitador` de volta pra `","`, a pegadinha volta junto — e aí vale lembrar que
"quebra de coluna" nem sempre é erro de digitação, às vezes é quem gerou o arquivo não ter
escapado o delimitador.
