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
public sealed class ProdutoValidador : AbstractValidator<ProdutoRaw>
{
    public ProdutoValidador()
    {
        RuleFor(p => p.Codigo)
            .Matches(@"^[A-Z]{3}\d{4}$")
            .WithErrorCode("CodigoFormatoInvalido")
            .WithMessage("Código deve seguir o padrão AAA0000.");

        RuleFor(p => p.Descricao)
            .NotEmpty()
            .WithErrorCode("DescricaoObrigatoria")
            .WithMessage("Descrição é obrigatória.");

        RuleFor(p => p.Preco)
            .Must(SerDecimalPositivo)
            .WithErrorCode("PrecoFormatoInvalido")
            .WithMessage("Preço deve ser um número decimal positivo (formato 0,00).");

        RuleFor(p => p.EstoqueMinimo)
            .Must(valor => int.TryParse(valor, out var n) && n >= 0)
            .WithErrorCode("EstoqueMinimoDeveSerInteiroNaoNegativo")
            .WithMessage("Estoque mínimo deve ser um inteiro maior ou igual a zero.");
    }

    private static bool SerDecimalPositivo(string valor) =>
        decimal.TryParse(valor.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) && n > 0;
}
```

Padrões úteis do FluentValidation pra esse tipo de regra:

| Regra                              | Como escrever                                  |
|-------------------------------------|-------------------------------------------------|
| Obrigatório                         | `.NotEmpty()`                                    |
| Formato fixo (CPF, CEP, código)     | `.Matches(@"regex")`                             |
| Conversão seria segura (int, data)  | `.Must(valor => TipoQualquer.TryParse(valor, ...))` |
| Intervalo numérico                  | `.Must(valor => n is >= min and <= max)`         |
| Campo opcional                      | `.Must(valor => string.IsNullOrEmpty(valor) || ...)` |
| Regra entre campos (cross-field)    | `RuleFor(x => x).Must(raw => ...)` no objeto inteiro |

Sempre usa `.WithErrorCode("NomeCurtoNoStilo PascalCase")` — é esse valor que vira
`NomeRegra` no `ErroValidacaoLayout` e que o `ResumoValidacaoLayout` agrupa em
`ErrosPorRegra`. Sem `WithErrorCode`, o motor cai pra `WithMessage` como chave, o que
funciona mas fica menos estável se você mudar o texto da mensagem depois.

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
