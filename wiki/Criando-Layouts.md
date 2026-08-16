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
public sealed class ProdutoValidadorLayout : IValidadorLayout<Produto>
{
    private readonly IValidator<ProdutoRaw> _validador;
    private readonly ILayoutMapper<ProdutoRaw, Produto> _mapper;

    public ProdutoValidadorLayout(IValidator<ProdutoRaw> validador, ILayoutMapper<ProdutoRaw, Produto> mapper)
    {
        _validador = validador;
        _mapper = mapper;
    }

    public IEnumerable<ResultadoValidacaoRegistro<Produto>> Validar(TextReader leitor)
    {
        var configuracaoCsv = new CsvConfiguration(CultureInfo.InvariantCulture);
        return LayoutValidationEngine.Validar(leitor, configuracaoCsv, _validador, _mapper);
    }
}
```

## 6. Registrar no DI

```csharp
services.AddValidatorsFromAssemblyContaining<ProdutoValidador>();
services.AddSingleton<ILayoutMapper<ProdutoRaw, Produto>, ProdutoMapper>();
services.AddScoped<IValidadorLayout<Produto>, ProdutoValidadorLayout>();
```

Pronto — o layout `Produto` está utilizável exatamente como o `Pessoa` e o
`Funcionario` descritos em [Usando a Ferramenta](Usando-a-Ferramenta.md).

## Pegadinha: campos cujo valor "correto" contém o delimitador

Se um campo do seu layout usa vírgula como separador decimal (comum em arquivo
brasileiro, ex: `"1234,56"`) e o delimitador do CSV também é vírgula, **o arquivo
precisa colocar esse campo entre aspas** (`"1234,56"`), senão vira duas colunas e a
linha inteira desalinha — o próprio `LayoutValidationEngine` vai reportar isso como
`EstruturaDeColunas`, mas é bom saber a causa de antemão. O gerador em
`apps/LayoutValidator.GeradorDados` passou por exatamente esse bug durante o
desenvolvimento (campo `Salario` sem aspas quebrando a contagem de colunas de quase
todo o arquivo) — o código de escape ali (`EscaparCampoCsv`) é uma referência de como
lidar com isso na hora de *gerar* arquivo de teste. Na *validação* de um arquivo real
recebido de terceiros, isso não é algo que você controla — é só um lembrete de que
"quebra de coluna" nem sempre é erro de digitação, às vezes é problema de quem gerou o
arquivo não ter escapado o delimitador certo.
