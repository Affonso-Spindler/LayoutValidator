# Regras Reutilizáveis

O pacote `LayoutValidator.Regras` é um catálogo de regras prontas pra usar nos seus
validadores — em vez de reescrever a regex do CPF e o `TryParse` da data em cada layout,
você declara:

```csharp
RuleFor(f => f.Cpf).Obrigatorio().Cpf();
RuleFor(f => f.DataNascimento).Obrigatorio().Data();
RuleFor(f => f.Uf).Obrigatorio().Uf();
```

O pacote é **opcional e independente**: ele não referencia `LayoutValidator` (o core), só o
FluentValidation. Você pode usar o core sem ele, usar ele sem o core, ou — o mais provável —
usar os dois e ainda escrever as suas próprias regras por cima, do jeito descrito lá embaixo
em [Criando a sua própria biblioteca de regras](#criando-a-sua-própria-biblioteca-de-regras).

Se o layout não é código C# e sim cadastrado em banco via
[API local](Cadastro-de-Layouts-via-API.md), o catálogo é o mesmo conjunto de regras — só
referenciado por chave (`"Cpf"`, `"InteiroEntre"`) em vez de método de extensão
(`.Cpf()`, `.InteiroEntre()`).

## As duas regras do jogo

Duas convenções valem pra todo o catálogo. Entender elas evita 90% das surpresas.

### 1. Regra de formato não reprova valor vazio

`Obrigatorio()` é a **única** regra que reclama de campo em branco. Todas as outras deixam
passar valor vazio de propósito. Isso faz campo opcional ficar trivial:

```csharp
RuleFor(f => f.DataDemissao).Data();               // opcional: vazio passa, preenchido tem que ser data
RuleFor(f => f.DataAdmissao).Obrigatorio().Data(); // obrigatório: vazio reprova
```

Sem essa convenção, todo campo opcional viraria
`Must(valor => string.IsNullOrEmpty(valor) || ...)` escrito na mão. E como consequência boa,
um campo vazio produz **um** erro (`CampoObrigatorio`) em vez de um por regra encadeada —
além de deixar "veio em branco" distinguível de "veio malformado" no relatório.

Valor só com espaços conta como vazio.

### 2. Um erro por campo: declare o `CascadeMode`

Todo validador de layout deve começar com:

```csharp
public sealed class MeuValidador : AbstractValidator<MeuRaw>
{
    public MeuValidador()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        // ...
    }
}
```

O default do FluentValidation é `Continue`: **toda a cadeia roda mesmo depois de uma regra
falhar**. Com regras encadeadas isso duplica erro. Exemplo real — `CargaHoraria` com o valor
`"abc"`:

```csharp
RuleFor(f => f.CargaHoraria).Inteiro().InteiroEntre(1, 60);
```

`Inteiro()` falha, e `InteiroEntre()` também falha (não consegue parsear "abc"). São duas
falhas pra uma célula só: duas linhas no `relatorio_erros.csv` e o mesmo problema contado
duas vezes no `ErrosPorRegra` do `ResumoValidacaoLayout`.

`Stop` no nível de **regra** interrompe a cadeia dentro de um campo, mas segue avaliando os
**outros** campos — você continua vendo todos os campos errados da linha, com um motivo por
campo. Não mexa no `ClassLevelCascadeMode`: ele em `Stop` faria a primeira coluna ruim
esconder o resto da linha inteira.

## Catálogo

O código de erro é o que vira `NomeRegra` no `ErroValidacaoLayout` e o que o
`ResumoValidacaoLayout` agrupa em `ErrosPorRegra` — é estável, pode contar com ele.

### Texto

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Obrigatorio()` | `CampoObrigatorio` | qualquer coisa que não seja vazio nem só espaços |
| `ComprimentoEntre(min, max)` | `ComprimentoInvalido` | comprimento dentro do intervalo |
| `ComprimentoMaximo(max)` | `ComprimentoInvalido` | até `max` caracteres |
| `ComprimentoExato(n)` | `ComprimentoInvalido` | exatamente `n` caracteres |
| `SomenteDigitos()` | `SomenteDigitosInvalido` | só `0`-`9`, sem sinal nem separador |
| `ValorEm("S", "N")` | `ValorForaDoDominio` | domínio fechado, ignorando caixa |
| `Formato(regex, codigo, mensagem)` | o que você passar | escape hatch pra regra pontual |

### Numéricas

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Inteiro()` | `InteiroInvalido` | inteiro com sinal opcional |
| `InteiroPositivo()` | `InteiroPositivoInvalido` | inteiro `>= 1` |
| `InteiroNaoNegativo()` | `InteiroNaoNegativoInvalido` | inteiro `>= 0` |
| `InteiroEntre(min, max)` | `InteiroForaDoIntervalo` | inteiro no intervalo, inclusive |
| `Decimal()` | `DecimalInvalido` | `1234,56` e `1.234,56` (padrão brasileiro) |
| `DecimalPositivo()` | `DecimalPositivoInvalido` | decimal `> 0` |
| `DecimalEntre(min, max)` | `DecimalForaDoIntervalo` | decimal no intervalo, inclusive |

### Data

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Data(formato = "dd/MM/yyyy")` | `DataInvalida` | data que existe de verdade — `31/02/2000` reprova |
| `DataEntre(min, max, formato)` | `DataForaDoIntervalo` | data dentro do intervalo, inclusive |
| `DataNoPassado(formato)` | `DataNoFuturo` | data não futura; hoje passa |

### Financeiras

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Moeda(casasDecimais = 2)` | `MoedaInvalida` | `1234,56` — casas exatas, sem separador de milhar |
| `Percentual()` | `PercentualInvalido` | decimal entre 0 e 100 |
| `CartaoDeCredito()` | `CartaoDeCreditoInvalido` | 13 a 19 dígitos, algoritmo de Luhn |

`Moeda()` é mais estrito que `Decimal()` de propósito: arquivo de carga costuma especificar o
número de casas, e `1234,5` num campo declarado com 2 casas é defeito.

### Contato

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Email()` | `EmailInvalido` | parte local, `@`, domínio com ponto, sem espaço |

### Brasil

| Regra | Código de erro | O que aceita |
|---|---|---|
| `Cpf()` | `CpfInvalido` | 11 dígitos, **sem máscara**, dígito verificador correto |
| `Cnpj()` | `CnpjInvalido` | 14 dígitos, **sem máscara**, dígito verificador correto |
| `CpfOuCnpj()` | `CpfOuCnpjInvalido` | um ou outro — coluna única de documento |
| `Cep()` | `CepInvalido` | `00000-000` ou `00000000` |
| `Uf()` | `UfInvalida` | as 27 siglas reais, ignorando caixa — `CC` reprova |
| `Telefone()` | `TelefoneInvalido` | `(00) 00000-0000`, `(00) 0000-0000`, ou só os 10/11 dígitos |
| `Cnh()` | `CnhInvalida` | 11 dígitos com dígito verificador correto |
| `PisPasep()` | `PisPasepInvalido` | 11 dígitos com dígito verificador correto |

Os documentos com dígito verificador (`Cpf`, `Cnpj`, `Cnh`, `PisPasep`) também recusam
sequência de um dígito repetido: `00000000000` passa no módulo 11 mas não é documento de
ninguém.

**Não tem regra de Inscrição Estadual** — validar IE de verdade precisa de um algoritmo
por estado e da UF vinda de outra coluna. Se você precisar, é um bom candidato pra sua
própria biblioteca de regras.

## Predicados puros

Cada regra é uma casquinha fina em volta de um predicado `bool` que vive em
`LayoutValidator.Regras.Predicados` e não depende do FluentValidation:

```csharp
using LayoutValidator.Regras.Predicados;

if (Documentos.CpfValido(valor)) { /* ... */ }
if (Formatos.DataValida(valor, "dd/MM/yyyy")) { /* ... */ }
if (UnidadesFederativas.Valida(valor)) { /* ... */ }
```

Útil quando você precisa da mesma checagem fora de uma cadeia de validação — dentro de um
`Mapper`, por exemplo, ou numa tela.

## Criando a sua própria biblioteca de regras

Esta é a parte que importa se o seu domínio tem regras que o catálogo não cobre — código de
produto interno, formato de contrato, matrícula com dígito verificador próprio.

**Não tem nada pra herdar, implementar ou registrar.** Regras são extension methods sobre
`IRuleBuilder<T, string>`, então qualquer assembly pode adicionar as suas e elas ficam
disponíveis no `RuleFor` do mesmo jeito que as nossas.

```csharp
using FluentValidation;

namespace MinhaEmpresa.Regras;

public static class RegrasContratoExtensions
{
    // Camada 1: o predicado puro. bool, sem FluentValidation, testável direto.
    public static bool CodigoContratoValido(string? valor)
    {
        if (string.IsNullOrEmpty(valor) || valor.Length != 10)
            return false;

        if (!valor.StartsWith("CT", StringComparison.Ordinal))
            return false;

        return valor[2..].All(char.IsDigit);
    }

    // Camada 2: a regra, que embrulha o predicado com código de erro e mensagem.
    public static IRuleBuilderOptions<T, string> CodigoContrato<T>(this IRuleBuilder<T, string> regra) =>
        regra.Must(valor => string.IsNullOrWhiteSpace(valor) || CodigoContratoValido(valor))
            .WithErrorCode("CodigoContratoInvalido")
            .WithMessage("'{PropertyName}' deve ser um código de contrato no formato CT00000000.");
}
```

E no seu layout:

```csharp
RuleFor(c => c.CodigoContrato).Obrigatorio().CodigoContrato();
```

Três coisas pra acertar, em ordem de importância:

**1. O predicado nunca pode lançar exceção.** Esta é a única que morde de verdade. O motor
de validação é um iterador preguiçoso que percorre o arquivo linha a linha — uma exceção
dentro de uma regra **não reprova a linha**, ela sobe pelo `foreach` de quem está consumindo
e aborta a leitura do arquivo inteiro. Uma célula suja no meio de um arquivo de milhões de
linhas derrubaria a carga toda. Use sempre `TryParse` em vez de `Parse`, cheque tamanho
antes de indexar, e trate `null` mesmo que a propriedade seja declarada `string`.

**2. Deixe valor vazio passar.** É o contrato descrito no começo desta página. Quem quiser
obrigatoriedade encadeia `.Obrigatorio()` antes.

**3. Use `{PropertyName}` na mensagem e um `WithErrorCode` estável.** O `{PropertyName}` faz
a mesma regra servir qualquer layout sem mensagem duplicada, e o código de erro é o que o
`ResumoValidacaoLayout` agrupa — se você mudar ele depois, quebra a comparação histórica dos
seus relatórios.

Se você tiver várias regras próprias, vale colocá-las num projeto separado do layout, pelo
mesmo motivo que `LayoutValidator.Regras` é separado do core: regra de negócio é reusável
entre layouts, e layout não é reusável entre projetos.
