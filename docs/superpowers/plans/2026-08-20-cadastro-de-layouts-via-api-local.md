# Cadastro de Layouts via API Local Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir cadastrar layouts (campos + regras de validação) em um banco SQLite e
validar uma linha de texto contra um layout cadastrado através de uma API HTTP local —
sem exigir que o layout exista como código C#.

**Architecture:** Novo app `apps/LayoutValidator.Api` (ASP.NET Core Minimal API, net8.0)
na mesma solution, referenciando `LayoutValidator` (core) e `LayoutValidator.Regras`
diretamente. Layouts/campos/regras persistem via EF Core + SQLite. Um catálogo de regras
fixo (`ICatalogoDeRegras`) mapeia cada `ChaveRegra` cadastrável (`Cpf`, `InteiroEntre`,
`ValorEm`, etc.) para os predicados puros já existentes em `LayoutValidator.Regras.Predicados`
— zero reescrita da lógica de validação. Validar uma linha não produz mais um Model final
tipado: a resposta é só `aderente/não-aderente` + lista de erros por campo.

**Tech Stack:** .NET 8, ASP.NET Core Minimal API, EF Core + SQLite, CsvHelper (via
`LayoutValidator` core), xUnit, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory).

**Spec:** [docs/adr/0002-cadastro-de-layouts-via-api-local.md](../../adr/0002-cadastro-de-layouts-via-api-local.md)

## Global Constraints

- Layout cadastrado é sempre **posicional** — sem conceito de cabeçalho (v1 só valida uma
  linha solta por chamada, nunca um arquivo).
- `Codigo` do layout: único, alfanumérico (letras e números), no máximo **20 caracteres**.
  `Nome` é só um rótulo descritivo, não único.
- `Obrigatorio` é uma entrada de regra como qualquer outra (`ChaveRegra="Obrigatorio"`),
  nunca uma coluna booleana separada. Toda outra regra **nunca reprova valor vazio**
  (mesmo contrato de `ConstrutorRegra.DeFormato` do core).
- Regras avaliadas **na ordem cadastrada, com cascade-stop por campo**: para na primeira
  regra que falhar naquele campo, mas continua avaliando os demais campos.
- `POST`/`PUT /layouts` valida `ParametrosJson` de cada regra contra os parâmetros
  esperados do catálogo **antes de salvar** — layout mal cadastrado nunca chega a existir.
- Sem autenticação (API roda local). Sem versionamento de layout (editar sobrescreve).
- Nomes de classes/métodos/propriedades em pt-BR, papéis técnicos de infraestrutura em
  inglês — mesma convenção do resto do repositório (ver README, seção "Convenção de
  nomenclatura").
- Catálogo de regras cobre exatamente: `Obrigatorio`, `ComprimentoEntre`,
  `ComprimentoMaximo`, `ComprimentoExato`, `SomenteDigitos`, `ValorEm`, `Formato`,
  `Inteiro`, `InteiroEntre`, `Decimal`, `DecimalEntre`, `Cpf`, `Cnpj`, `CpfOuCnpj`, `Cep`,
  `Uf`, `Telefone`, `Cnh`, `PisPasep` — nenhuma regra fora dessa lista entra nesta v1.

---

## Task 1: Scaffold dos projetos Api e Api.Tests

**Files:**
- Create: `apps/LayoutValidator.Api/LayoutValidator.Api.csproj`
- Create: `apps/LayoutValidator.Api/Program.cs`
- Create: `tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj`
- Modify: `LayoutValidator.sln`

**Interfaces:**
- Produces: projeto `LayoutValidator.Api` (Sdk.Web, net8.0) referenciando `LayoutValidator`
  e `LayoutValidator.Regras`; classe `public partial class Program` acessível para
  `WebApplicationFactory<Program>` nos testes de integração das próximas tasks.

- [ ] **Step 1: Criar a pasta e o csproj do app**

Crie `apps/LayoutValidator.Api/LayoutValidator.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\..\src\LayoutValidator\LayoutValidator.csproj" />
    <ProjectReference Include="..\..\src\LayoutValidator.Regras\LayoutValidator.Regras.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

- [ ] **Step 2: Criar o Program.cs mínimo**

Crie `apps/LayoutValidator.Api/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Sem isso, a resposta sai em camelCase (padrão do ASP.NET Core) enquanto os DTOs em
// Contratos/ são PascalCase — ReadFromJsonAsync<T> nos testes de integração usa matching
// case-sensitive por padrão e preencheria tudo com null/default silenciosamente.
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

app.Run();

public partial class Program;
```

- [ ] **Step 3: Criar o csproj do projeto de testes**

Crie `tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\apps\LayoutValidator.Api\LayoutValidator.Api.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Adicionar os dois projetos à solution**

Run:
```bash
dotnet sln LayoutValidator.sln add apps/LayoutValidator.Api/LayoutValidator.Api.csproj --solution-folder apps
dotnet sln LayoutValidator.sln add tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --solution-folder tests
```

- [ ] **Step 5: Build e teste (vazio) devem passar**

Run: `dotnet build LayoutValidator.sln`
Expected: `Build succeeded.`

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj`
Expected: build succeeded, "0 tests" (nenhum arquivo de teste ainda — esperado).

- [ ] **Step 6: Commit**

```bash
git add apps/LayoutValidator.Api tests/LayoutValidator.Api.Tests LayoutValidator.sln
git commit -m "feat(api): scaffold do app LayoutValidator.Api"
```

---

## Task 2: Catálogo de regras — infraestrutura + regras de texto

**Files:**
- Create: `apps/LayoutValidator.Api/Regras/ParametroEsperado.cs`
- Create: `apps/LayoutValidator.Api/Regras/RegraCadastrada.cs`
- Create: `apps/LayoutValidator.Api/Regras/ConstrutorDeRegraCadastrada.cs`
- Create: `apps/LayoutValidator.Api/Regras/ParametrosExtensions.cs`
- Create: `apps/LayoutValidator.Api/Regras/RegrasDeTextoCatalogo.cs`
- Test: `tests/LayoutValidator.Api.Tests/Regras/RegrasDeTextoCatalogoTestes.cs`

**Interfaces:**
- Produces: `enum TipoParametro { Inteiro, Decimal, Texto, ListaDeTexto }`;
  `record ParametroEsperado(string Nome, TipoParametro Tipo, bool Obrigatorio)`;
  `class RegraCadastrada { string Chave; IReadOnlyList<ParametroEsperado> ParametrosEsperados;
  Func<string, JsonElement?, bool> Avaliar; Func<JsonElement?, string> ObterCodigoErro;
  Func<string, JsonElement?, string> MontarMensagem; }`;
  `static class ConstrutorDeRegraCadastrada { static RegraCadastrada DeFormato(string chave,
  string codigoErro, IReadOnlyList<ParametroEsperado> parametrosEsperados,
  Func<string, JsonElement?, bool> predicado, Func<string, JsonElement?, string> montarMensagem) }`;
  extension methods em `JsonElement?` (`ObterInteiro`, `ObterDecimal`, `ObterTexto`,
  `ObterListaDeTexto`, `TemPropriedade`); `static class RegrasDeTextoCatalogo { static
  IEnumerable<RegraCadastrada> Construir() }` com as chaves `Obrigatorio`,
  `ComprimentoEntre`, `ComprimentoMaximo`, `ComprimentoExato`, `SomenteDigitos`, `ValorEm`,
  `Formato`.

- [ ] **Step 1: Criar `ParametroEsperado.cs`**

```csharp
namespace LayoutValidator.Api.Regras;

public enum TipoParametro
{
    Inteiro,
    Decimal,
    Texto,
    ListaDeTexto
}

public sealed record ParametroEsperado(string Nome, TipoParametro Tipo, bool Obrigatorio);
```

- [ ] **Step 2: Criar `RegraCadastrada.cs`**

```csharp
using System.Text.Json;

namespace LayoutValidator.Api.Regras;

/// <summary>
/// Uma regra do catálogo cadastrável por chave — equivalente dinâmico dos métodos de
/// extensão de LayoutValidator.Regras (Cpf(), InteiroEntre(), etc.), só que descrita como
/// dado em vez de como método fortemente tipado.
/// </summary>
public sealed class RegraCadastrada
{
    public required string Chave { get; init; }
    public required IReadOnlyList<ParametroEsperado> ParametrosEsperados { get; init; }
    public required Func<string, JsonElement?, bool> Avaliar { get; init; }
    public required Func<JsonElement?, string> ObterCodigoErro { get; init; }
    public required Func<string, JsonElement?, string> MontarMensagem { get; init; }
}
```

- [ ] **Step 3: Criar `ParametrosExtensions.cs`**

```csharp
using System.Text.Json;

namespace LayoutValidator.Api.Regras;

internal static class ParametrosExtensions
{
    public static long ObterInteiro(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetInt64();

    public static decimal ObterDecimal(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetDecimal();

    public static string ObterTexto(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetString() ?? string.Empty;

    public static string[] ObterListaDeTexto(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

    public static bool TemPropriedade(this JsonElement? parametros, string nome) =>
        parametros.HasValue
        && parametros.Value.ValueKind == JsonValueKind.Object
        && parametros.Value.TryGetProperty(nome, out _);
}
```

- [ ] **Step 4: Criar `ConstrutorDeRegraCadastrada.cs`**

```csharp
using System.Text.Json;

namespace LayoutValidator.Api.Regras;

/// <summary>
/// Equivalente dinâmico de LayoutValidator.Regras.ConstrutorRegra: costura um predicado com
/// código de erro e mensagem, aplicando o mesmo contrato do catálogo estático — regra de
/// formato nunca reprova valor vazio. Obrigatoriedade é a única exceção e não passa por aqui.
/// </summary>
internal static class ConstrutorDeRegraCadastrada
{
    public static RegraCadastrada DeFormato(
        string chave,
        string codigoErro,
        IReadOnlyList<ParametroEsperado> parametrosEsperados,
        Func<string, JsonElement?, bool> predicado,
        Func<string, JsonElement?, string> montarMensagem) =>
        new()
        {
            Chave = chave,
            ParametrosEsperados = parametrosEsperados,
            Avaliar = (valor, parametros) => string.IsNullOrWhiteSpace(valor) || predicado(valor, parametros),
            ObterCodigoErro = _ => codigoErro,
            MontarMensagem = montarMensagem
        };
}
```

- [ ] **Step 5: Criar `RegrasDeTextoCatalogo.cs`**

```csharp
using System.Text.RegularExpressions;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasTextoExtensions.</summary>
internal static class RegrasDeTextoCatalogo
{
    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return new RegraCadastrada
        {
            Chave = "Obrigatorio",
            ParametrosEsperados = Array.Empty<ParametroEsperado>(),
            Avaliar = (valor, _) => !string.IsNullOrWhiteSpace(valor),
            ObterCodigoErro = _ => "CampoObrigatorio",
            MontarMensagem = (nomeCampo, _) => $"'{nomeCampo}' é obrigatório."
        };

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoEntre",
            "ComprimentoInvalido",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Inteiro, true),
                new ParametroEsperado("maximo", TipoParametro.Inteiro, true)
            },
            (valor, p) => (valor?.Length ?? 0) >= p.ObterInteiro("minimo") && (valor?.Length ?? 0) <= p.ObterInteiro("maximo"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter entre {p.ObterInteiro("minimo")} e {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoMaximo",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("maximo", TipoParametro.Inteiro, true) },
            (valor, p) => (valor?.Length ?? 0) <= p.ObterInteiro("maximo"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter no máximo {p.ObterInteiro("maximo")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ComprimentoExato",
            "ComprimentoInvalido",
            new[] { new ParametroEsperado("comprimento", TipoParametro.Inteiro, true) },
            (valor, p) => (valor?.Length ?? 0) == p.ObterInteiro("comprimento"),
            (nomeCampo, p) => $"'{nomeCampo}' deve ter exatamente {p.ObterInteiro("comprimento")} caracteres.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "SomenteDigitos",
            "SomenteDigitosInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => valor.Length > 0 && valor.All(char.IsDigit),
            (nomeCampo, _) => $"'{nomeCampo}' deve conter somente dígitos.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "ValorEm",
            "ValorForaDoDominio",
            new[] { new ParametroEsperado("valores", TipoParametro.ListaDeTexto, true) },
            (valor, p) => p.ObterListaDeTexto("valores").Contains(valor, StringComparer.OrdinalIgnoreCase),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um destes valores: {string.Join(", ", p.ObterListaDeTexto("valores"))}.");

        yield return new RegraCadastrada
        {
            Chave = "Formato",
            ParametrosEsperados = new[]
            {
                new ParametroEsperado("expressaoRegular", TipoParametro.Texto, true),
                new ParametroEsperado("codigoErro", TipoParametro.Texto, true),
                new ParametroEsperado("mensagem", TipoParametro.Texto, true)
            },
            Avaliar = (valor, p) => string.IsNullOrWhiteSpace(valor)
                || Regex.IsMatch(valor, p.ObterTexto("expressaoRegular")),
            ObterCodigoErro = p => p.ObterTexto("codigoErro"),
            MontarMensagem = (nomeCampo, p) => p.ObterTexto("mensagem").Replace("{PropertyName}", nomeCampo)
        };
    }
}
```

- [ ] **Step 6: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Regras/RegrasDeTextoCatalogoTestes.cs`:

```csharp
using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeTextoCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeTextoCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("abc", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Obrigatorio_ReprovaVazioAceitaResto(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Obrigatorio"].Avaliar(valor, null));

    [Theory]
    [InlineData("", true)]       // regra de formato nunca reprova vazio
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    [InlineData("abcde", true)]
    [InlineData("abcdef", false)]
    public void ComprimentoEntre_RespeitaLimitesEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":3,"maximo":5}""");
        Assert.Equal(esperado, Regras["ComprimentoEntre"].Avaliar(valor, parametros));
    }

    [Fact]
    public void ComprimentoEntre_MontaMensagemComOsParametros()
    {
        var parametros = Parametros("""{"minimo":3,"maximo":5}""");
        Assert.Equal("'Nome' deve ter entre 3 e 5 caracteres.", Regras["ComprimentoEntre"].MontarMensagem("Nome", parametros));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("12a", false)]
    [InlineData("", true)]
    public void SomenteDigitos_AceitaSoDigitosOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["SomenteDigitos"].Avaliar(valor, null));

    [Theory]
    [InlineData("S", true)]
    [InlineData("n", true)]
    [InlineData("X", false)]
    [InlineData("", true)]
    public void ValorEm_AceitaDominioIgnorandoCaixaEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"valores":["S","N"]}""");
        Assert.Equal(esperado, Regras["ValorEm"].Avaliar(valor, parametros));
    }

    [Fact]
    public void Formato_UsaRegexECodigoErroEMensagemDosParametros()
    {
        var parametros = Parametros("""{"expressaoRegular":"^[0-9]{3}$","codigoErro":"CodigoInvalido","mensagem":"'{PropertyName}' precisa de 3 dígitos."}""");
        var regra = Regras["Formato"];

        Assert.True(regra.Avaliar("123", parametros));
        Assert.False(regra.Avaliar("12", parametros));
        Assert.True(regra.Avaliar("", parametros));
        Assert.Equal("CodigoInvalido", regra.ObterCodigoErro(parametros));
        Assert.Equal("'Codigo' precisa de 3 dígitos.", regra.MontarMensagem("Codigo", parametros));
    }
}
```

- [ ] **Step 7: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter RegrasDeTextoCatalogoTestes`
Expected: todos os testes passam.

- [ ] **Step 8: Commit**

```bash
git add apps/LayoutValidator.Api/Regras tests/LayoutValidator.Api.Tests/Regras
git commit -m "feat(api): catalogo de regras cadastraveis - infraestrutura e regras de texto"
```

---

## Task 3: Catálogo de regras — numéricas

**Files:**
- Create: `apps/LayoutValidator.Api/Regras/RegrasNumericasCatalogo.cs`
- Test: `tests/LayoutValidator.Api.Tests/Regras/RegrasNumericasCatalogoTestes.cs`

**Interfaces:**
- Consumes: `RegraCadastrada`, `ParametroEsperado`, `TipoParametro`, `ConstrutorDeRegraCadastrada`,
  `ParametrosExtensions` (Task 2).
- Produces: `static class RegrasNumericasCatalogo { static IEnumerable<RegraCadastrada> Construir() }`
  com as chaves `Inteiro`, `InteiroEntre`, `Decimal`, `DecimalEntre`.

- [ ] **Step 1: Criar `RegrasNumericasCatalogo.cs`**

```csharp
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasNumericasExtensions.</summary>
internal static class RegrasNumericasCatalogo
{
    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Inteiro",
            "InteiroInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.InteiroValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número inteiro.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "InteiroEntre",
            "InteiroForaDoIntervalo",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Inteiro, true),
                new ParametroEsperado("maximo", TipoParametro.Inteiro, true)
            },
            (valor, p) => Formatos.InteiroEntre(valor, p.ObterInteiro("minimo"), p.ObterInteiro("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um inteiro entre {p.ObterInteiro("minimo")} e {p.ObterInteiro("maximo")}.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Decimal",
            "DecimalInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => Formatos.DecimalValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um número decimal (vírgula como separador decimal).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "DecimalEntre",
            "DecimalForaDoIntervalo",
            new[]
            {
                new ParametroEsperado("minimo", TipoParametro.Decimal, true),
                new ParametroEsperado("maximo", TipoParametro.Decimal, true)
            },
            (valor, p) => Formatos.DecimalEntre(valor, p.ObterDecimal("minimo"), p.ObterDecimal("maximo")),
            (nomeCampo, p) => $"'{nomeCampo}' deve ser um decimal entre {p.ObterDecimal("minimo")} e {p.ObterDecimal("maximo")}.");
    }
}
```

- [ ] **Step 2: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Regras/RegrasNumericasCatalogoTestes.cs`:

```csharp
using System.Text.Json;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasNumericasCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasNumericasCatalogo.Construir().ToDictionary(r => r.Chave);

    private static JsonElement Parametros(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("42", true)]
    [InlineData("-7", true)]
    [InlineData("4,2", false)]
    [InlineData("", true)]
    public void Inteiro_AceitaInteiroOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Inteiro"].Avaliar(valor, null));

    [Theory]
    [InlineData("1", false)]
    [InlineData("18", true)]
    [InlineData("60", true)]
    [InlineData("61", false)]
    [InlineData("", true)]
    public void InteiroEntre_RespeitaLimitesInclusiveEDeixaVazioPassar(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":18,"maximo":60}""");
        Assert.Equal(esperado, Regras["InteiroEntre"].Avaliar(valor, parametros));
    }

    [Theory]
    [InlineData("1234,56", true)]
    [InlineData("1.234,56", false)]
    [InlineData("", true)]
    public void Decimal_AceitaFormatoBrasileiroOuVazio(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Decimal"].Avaliar(valor, null));

    [Theory]
    [InlineData("0,00", false)]
    [InlineData("10,50", true)]
    [InlineData("100,00", true)]
    [InlineData("100,01", false)]
    public void DecimalEntre_RespeitaLimitesInclusive(string valor, bool esperado)
    {
        var parametros = Parametros("""{"minimo":10.50,"maximo":100.00}""");
        Assert.Equal(esperado, Regras["DecimalEntre"].Avaliar(valor, parametros));
    }
}
```

- [ ] **Step 3: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter RegrasNumericasCatalogoTestes`
Expected: todos os testes passam.

- [ ] **Step 4: Commit**

```bash
git add apps/LayoutValidator.Api/Regras/RegrasNumericasCatalogo.cs tests/LayoutValidator.Api.Tests/Regras/RegrasNumericasCatalogoTestes.cs
git commit -m "feat(api): catalogo de regras cadastraveis - regras numericas"
```

---

## Task 4: Catálogo de regras — documentos + agregador

**Files:**
- Create: `apps/LayoutValidator.Api/Regras/RegrasDeDocumentoCatalogo.cs`
- Create: `apps/LayoutValidator.Api/Regras/ICatalogoDeRegras.cs`
- Create: `apps/LayoutValidator.Api/Regras/CatalogoDeRegras.cs`
- Modify: `apps/LayoutValidator.Api/Program.cs`
- Test: `tests/LayoutValidator.Api.Tests/Regras/RegrasDeDocumentoCatalogoTestes.cs`
- Test: `tests/LayoutValidator.Api.Tests/Regras/CatalogoDeRegrasTestes.cs`

**Interfaces:**
- Consumes: `RegraCadastrada`, `ConstrutorDeRegraCadastrada`, `RegrasDeTextoCatalogo.Construir()`
  (Task 2), `RegrasNumericasCatalogo.Construir()` (Task 3).
- Produces: `interface ICatalogoDeRegras { bool Existe(string chave); RegraCadastrada
  Obter(string chave); IReadOnlyList<RegraCadastrada> Todas { get; } }`; `class
  CatalogoDeRegras : ICatalogoDeRegras` (as 19 chaves da v1). Registrado em `Program.cs`
  como singleton — próximas tasks injetam `ICatalogoDeRegras` via DI.

- [ ] **Step 1: Criar `RegrasDeDocumentoCatalogo.cs`**

```csharp
using System.Text.RegularExpressions;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasBrasilExtensions.</summary>
internal static class RegrasDeDocumentoCatalogo
{
    private static readonly Regex PadraoCep = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);

    private static readonly Regex PadraoTelefone =
        new(@"^(\(\d{2}\) \d{4,5}-\d{4}|\d{10,11})$", RegexOptions.Compiled);

    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cpf", "CpfInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CpfValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CPF válido (11 dígitos, sem máscara, com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cnpj", "CnpjInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CnpjValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CNPJ válido (14 dígitos, sem máscara, com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "CpfOuCnpj", "CpfOuCnpjInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CpfOuCnpjValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CPF nem um CNPJ válido.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cep", "CepInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => PadraoCep.IsMatch(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um CEP no formato 00000-000 ou 00000000.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Uf", "UfInvalida", Array.Empty<ParametroEsperado>(),
            (valor, _) => UnidadesFederativas.Valida(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser a sigla de uma unidade federativa brasileira.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Telefone", "TelefoneInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => PadraoTelefone.IsMatch(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um telefone no formato (00) 00000-0000 ou só os dígitos.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cnh", "CnhInvalida", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CnhValida(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é uma CNH válida (11 dígitos com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "PisPasep", "PisPasepInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.PisPasepValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um PIS/PASEP válido (11 dígitos com dígito verificador correto).");
    }
}
```

- [ ] **Step 2: Criar `ICatalogoDeRegras.cs`**

```csharp
namespace LayoutValidator.Api.Regras;

public interface ICatalogoDeRegras
{
    bool Existe(string chave);
    RegraCadastrada Obter(string chave);
    IReadOnlyList<RegraCadastrada> Todas { get; }
}
```

- [ ] **Step 3: Criar `CatalogoDeRegras.cs`**

```csharp
namespace LayoutValidator.Api.Regras;

public sealed class CatalogoDeRegras : ICatalogoDeRegras
{
    private readonly IReadOnlyDictionary<string, RegraCadastrada> _regras;

    public CatalogoDeRegras()
    {
        var todas = RegrasDeTextoCatalogo.Construir()
            .Concat(RegrasNumericasCatalogo.Construir())
            .Concat(RegrasDeDocumentoCatalogo.Construir());

        _regras = todas.ToDictionary(regra => regra.Chave, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RegraCadastrada> Todas => _regras.Values.ToList();

    public bool Existe(string chave) => _regras.ContainsKey(chave);

    public RegraCadastrada Obter(string chave) =>
        _regras.TryGetValue(chave, out var regra)
            ? regra
            : throw new InvalidOperationException($"Regra '{chave}' não existe no catálogo.");
}
```

- [ ] **Step 4: Registrar o catálogo no `Program.cs`**

Atualize `apps/LayoutValidator.Api/Program.cs`:

```csharp
using LayoutValidator.Api.Regras;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

app.Run();

public partial class Program;
```

- [ ] **Step 5: Escrever os testes de documento**

Crie `tests/LayoutValidator.Api.Tests/Regras/RegrasDeDocumentoCatalogoTestes.cs`:

```csharp
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class RegrasDeDocumentoCatalogoTestes
{
    private static readonly Dictionary<string, RegraCadastrada> Regras =
        RegrasDeDocumentoCatalogo.Construir().ToDictionary(r => r.Chave);

    [Theory]
    [InlineData("11144477735", true)]  // CPF válido conhecido
    [InlineData("11111111111", false)] // dígitos repetidos
    [InlineData("12345678900", false)] // dígito verificador errado
    [InlineData("", true)]
    public void Cpf_ValidaDigitoVerificadorEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cpf"].Avaliar(valor, null));

    [Theory]
    [InlineData("11222333000181", true)] // CNPJ válido conhecido
    [InlineData("11111111000111", false)]
    [InlineData("", true)]
    public void Cnpj_ValidaDigitoVerificadorEDeixaVazioPassar(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cnpj"].Avaliar(valor, null));

    [Theory]
    [InlineData("11144477735", true)]      // CPF
    [InlineData("11222333000181", true)]   // CNPJ
    [InlineData("123", false)]
    public void CpfOuCnpj_AceitaQualquerUmDosDois(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["CpfOuCnpj"].Avaliar(valor, null));

    [Theory]
    [InlineData("01310-100", true)]
    [InlineData("01310100", true)]
    [InlineData("1310-100", false)]
    public void Cep_AceitaComOuSemHifen(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cep"].Avaliar(valor, null));

    [Theory]
    [InlineData("SP", true)]
    [InlineData("sp", true)]
    [InlineData("CC", false)]
    public void Uf_AceitaSiglaRealIgnorandoCaixa(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Uf"].Avaliar(valor, null));

    [Theory]
    [InlineData("(11) 98888-7777", true)]
    [InlineData("11988887777", true)]
    [InlineData("998887777", false)]
    public void Telefone_AceitaFormatosConhecidos(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Telefone"].Avaliar(valor, null));

    [Theory]
    [InlineData("02650306461", true)] // CNH válida conhecida
    [InlineData("00000000000", false)]
    public void Cnh_ValidaDigitoVerificador(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["Cnh"].Avaliar(valor, null));

    [Theory]
    [InlineData("12045678905", true)] // PIS/PASEP válido (dígito verificador conferido à mão contra o algoritmo)
    [InlineData("00000000000", false)]
    public void PisPasep_ValidaDigitoVerificador(string valor, bool esperado) =>
        Assert.Equal(esperado, Regras["PisPasep"].Avaliar(valor, null));
}
```

- [ ] **Step 6: Escrever o teste do agregador**

Crie `tests/LayoutValidator.Api.Tests/Regras/CatalogoDeRegrasTestes.cs`:

```csharp
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Tests.Regras;

public class CatalogoDeRegrasTestes
{
    private static readonly string[] ChavesEsperadas =
    {
        "Obrigatorio", "ComprimentoEntre", "ComprimentoMaximo", "ComprimentoExato",
        "SomenteDigitos", "ValorEm", "Formato", "Inteiro", "InteiroEntre", "Decimal",
        "DecimalEntre", "Cpf", "Cnpj", "CpfOuCnpj", "Cep", "Uf", "Telefone", "Cnh", "PisPasep"
    };

    [Fact]
    public void Todas_ContemExatamenteAs19ChavesDaV1()
    {
        var catalogo = new CatalogoDeRegras();
        var chaves = catalogo.Todas.Select(r => r.Chave).ToArray();

        Assert.Equal(ChavesEsperadas.Length, chaves.Length);
        foreach (var chave in ChavesEsperadas)
            Assert.Contains(chave, chaves);
    }

    [Fact]
    public void Existe_EObter_SaoCaseInsensitive()
    {
        var catalogo = new CatalogoDeRegras();

        Assert.True(catalogo.Existe("cpf"));
        Assert.Equal("Cpf", catalogo.Obter("CPF").Chave);
    }

    [Fact]
    public void Obter_LancaParaChaveInexistente()
    {
        var catalogo = new CatalogoDeRegras();

        Assert.Throws<InvalidOperationException>(() => catalogo.Obter("NaoExiste"));
    }
}
```

- [ ] **Step 7: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter "RegrasDeDocumentoCatalogoTestes|CatalogoDeRegrasTestes"`
Expected: todos os testes passam.

- [ ] **Step 8: Build completo**

Run: `dotnet build LayoutValidator.sln`
Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add apps/LayoutValidator.Api/Regras apps/LayoutValidator.Api/Program.cs tests/LayoutValidator.Api.Tests/Regras
git commit -m "feat(api): catalogo de regras cadastraveis - documentos, agregador e registro no DI"
```

---

## Task 5: EF Core — modelos, DbContext e migração inicial

**Files:**
- Create: `apps/LayoutValidator.Api/Modelos/LayoutCadastrado.cs`
- Create: `apps/LayoutValidator.Api/Modelos/CampoCadastrado.cs`
- Create: `apps/LayoutValidator.Api/Modelos/RegraCampoCadastrada.cs`
- Create: `apps/LayoutValidator.Api/Dados/ApiDbContext.cs`
- Create: `apps/LayoutValidator.Api/Dados/ApiDbContextFactory.cs`
- Create: `apps/LayoutValidator.Api/Dados/Migrations/*` (gerado pelo `dotnet ef`)
- Create: `apps/LayoutValidator.Api/appsettings.json`
- Modify: `apps/LayoutValidator.Api/LayoutValidator.Api.csproj`
- Modify: `apps/LayoutValidator.Api/Program.cs`
- Test: `tests/LayoutValidator.Api.Tests/Dados/ApiDbContextTestes.cs`

**Interfaces:**
- Produces: entidades `LayoutCadastrado { int Id; string Codigo; string Nome; string
  Delimitador; DateTime CriadoEm; DateTime AtualizadoEm; List<CampoCadastrado> Campos; }`,
  `CampoCadastrado { int Id; int LayoutId; string Nome; int Ordem; List<RegraCampoCadastrada>
  Regras; }`, `RegraCampoCadastrada { int Id; int CampoId; string ChaveRegra; string?
  ParametrosJson; int Ordem; }`; `class ApiDbContext : DbContext { DbSet<LayoutCadastrado>
  Layouts; }` registrado via `AddDbContext` em `Program.cs`. Próximas tasks consomem
  `ApiDbContext` e as três entidades.

- [ ] **Step 1: Adicionar os pacotes do EF Core + SQLite**

Run:
```bash
dotnet add apps/LayoutValidator.Api/LayoutValidator.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add apps/LayoutValidator.Api/LayoutValidator.Api.csproj package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Sincronizar a ferramenta `dotnet-ef` com a versão do pacote resolvida**

Run: `dotnet tool update --global dotnet-ef`
Expected: instala/atualiza `dotnet-ef` para compatibilizar com `Microsoft.EntityFrameworkCore.Design`
recém-adicionado (evita erro de versão divergente ao rodar `dotnet ef` no Step 6).

- [ ] **Step 3: Criar as entidades**

Crie `apps/LayoutValidator.Api/Modelos/LayoutCadastrado.cs`:

```csharp
namespace LayoutValidator.Api.Modelos;

public sealed class LayoutCadastrado
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nome { get; set; }
    public required string Delimitador { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public List<CampoCadastrado> Campos { get; set; } = new();
}
```

Crie `apps/LayoutValidator.Api/Modelos/CampoCadastrado.cs`:

```csharp
namespace LayoutValidator.Api.Modelos;

public sealed class CampoCadastrado
{
    public int Id { get; set; }
    public int LayoutId { get; set; }
    public required string Nome { get; set; }
    public int Ordem { get; set; }
    public List<RegraCampoCadastrada> Regras { get; set; } = new();
}
```

Crie `apps/LayoutValidator.Api/Modelos/RegraCampoCadastrada.cs`:

```csharp
namespace LayoutValidator.Api.Modelos;

public sealed class RegraCampoCadastrada
{
    public int Id { get; set; }
    public int CampoId { get; set; }
    public required string ChaveRegra { get; set; }
    public string? ParametrosJson { get; set; }
    public int Ordem { get; set; }
}
```

- [ ] **Step 4: Criar o `ApiDbContext`**

Crie `apps/LayoutValidator.Api/Dados/ApiDbContext.cs`:

```csharp
using LayoutValidator.Api.Modelos;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Dados;

public sealed class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> opcoes) : base(opcoes) { }

    public DbSet<LayoutCadastrado> Layouts => Set<LayoutCadastrado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LayoutCadastrado>(layout =>
        {
            layout.Property(l => l.Codigo).HasMaxLength(20).IsRequired();
            layout.HasIndex(l => l.Codigo).IsUnique();
            layout.Property(l => l.Nome).IsRequired();
            layout.Property(l => l.Delimitador).IsRequired();

            // FK obrigatória (int, não anulável): limpar Campos da coleção apaga os órfãos
            // automaticamente no SaveChanges, sem precisar de OnDelete explícito adicional.
            layout.HasMany(l => l.Campos)
                .WithOne()
                .HasForeignKey(c => c.LayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampoCadastrado>(campo =>
        {
            campo.Property(c => c.Nome).IsRequired();

            campo.HasMany(c => c.Regras)
                .WithOne()
                .HasForeignKey(r => r.CampoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegraCampoCadastrada>(regra =>
        {
            regra.Property(r => r.ChaveRegra).IsRequired();
        });
    }
}
```

- [ ] **Step 5: Criar a factory de design-time**

Crie `apps/LayoutValidator.Api/Dados/ApiDbContextFactory.cs` — necessária para que `dotnet
ef` construa o `ApiDbContext` sem depender do `Program.cs` (que mais adiante vai rodar
`Database.Migrate()` no startup; a factory evita qualquer ambiguidade sobre isso em tempo
de design):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LayoutValidator.Api.Dados;

public sealed class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite("Data Source=layoutvalidator.db")
            .Options;

        return new ApiDbContext(opcoes);
    }
}
```

- [ ] **Step 6: Gerar a migração inicial**

Run: `dotnet ef migrations add InitialCreate --project apps/LayoutValidator.Api --output-dir Dados/Migrations`
Expected: cria `apps/LayoutValidator.Api/Dados/Migrations/*_InitialCreate.cs` e
`ApiDbContextModelSnapshot.cs`, sem erro.

- [ ] **Step 7: Criar o `appsettings.json`**

Crie `apps/LayoutValidator.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Padrao": "Data Source=layoutvalidator.db"
  }
}
```

- [ ] **Step 8: Registrar o `ApiDbContext` e aplicar migração no startup**

Atualize `apps/LayoutValidator.Api/Program.cs`:

```csharp
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Regras;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.AddDbContext<ApiDbContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

using (var escopoDeInicializacao = app.Services.CreateScope())
{
    escopoDeInicializacao.ServiceProvider.GetRequiredService<ApiDbContext>().Database.Migrate();
}

app.Run();

public partial class Program;
```

- [ ] **Step 9: Escrever o teste do `ApiDbContext`**

Crie `tests/LayoutValidator.Api.Tests/Dados/ApiDbContextTestes.cs` — usa uma conexão SQLite
em memória mantida aberta durante o teste (padrão recomendado para testar EF Core + SQLite
sem tocar disco):

```csharp
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Modelos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Tests.Dados;

public class ApiDbContextTestes : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly ApiDbContext _db;

    public ApiDbContextTestes()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var opcoes = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(_conexao).Options;
        _db = new ApiDbContext(opcoes);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexao.Dispose();
    }

    [Fact]
    public async Task SalvaLayoutComCamposERegrasAninhados()
    {
        _db.Layouts.Add(new LayoutCadastrado
        {
            Codigo = "PESSOA1",
            Nome = "Pessoa",
            Delimitador = ";",
            Campos =
            {
                new CampoCadastrado
                {
                    Nome = "Cpf",
                    Ordem = 0,
                    Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
                }
            }
        });

        await _db.SaveChangesAsync();

        var salvo = await _db.Layouts
            .Include(l => l.Campos).ThenInclude(c => c.Regras)
            .FirstAsync(l => l.Codigo == "PESSOA1");

        Assert.Single(salvo.Campos);
        Assert.Single(salvo.Campos[0].Regras);
        Assert.Equal("Cpf", salvo.Campos[0].Regras[0].ChaveRegra);
    }

    [Fact]
    public async Task Codigo_NaoAceitaDuplicado()
    {
        _db.Layouts.Add(new LayoutCadastrado { Codigo = "DUP1", Nome = "A", Delimitador = ";" });
        await _db.SaveChangesAsync();

        _db.Layouts.Add(new LayoutCadastrado { Codigo = "DUP1", Nome = "B", Delimitador = ";" });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task LimparColecaoDeCampos_ApagaOsCamposOrfaosAoSalvar()
    {
        var layout = new LayoutCadastrado
        {
            Codigo = "ORFAO1",
            Nome = "Teste",
            Delimitador = ";",
            Campos = { new CampoCadastrado { Nome = "X", Ordem = 0 } }
        };
        _db.Layouts.Add(layout);
        await _db.SaveChangesAsync();

        layout.Campos.Clear();
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.Set<CampoCadastrado>().ToListAsync());
    }
}
```

- [ ] **Step 10: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter ApiDbContextTestes`
Expected: todos os testes passam.

- [ ] **Step 11: Build completo**

Run: `dotnet build LayoutValidator.sln`
Expected: `Build succeeded.`

- [ ] **Step 12: Commit**

```bash
git add apps/LayoutValidator.Api/Modelos apps/LayoutValidator.Api/Dados apps/LayoutValidator.Api/appsettings.json apps/LayoutValidator.Api/Program.cs apps/LayoutValidator.Api/LayoutValidator.Api.csproj tests/LayoutValidator.Api.Tests/Dados
git commit -m "feat(api): entidades EF Core, ApiDbContext, SQLite e migracao inicial"
```

---

## Task 6: DivisorDeLinha

**Files:**
- Create: `apps/LayoutValidator.Api/Validacao/DivisorDeLinha.cs`
- Test: `tests/LayoutValidator.Api.Tests/Validacao/DivisorDeLinhaTestes.cs`

**Interfaces:**
- Consumes: `LayoutValidator.Core.OpcoesLayout`, `LayoutValidator.Core.ModoCabecalho` (já
  existentes no core).
- Produces: `static class DivisorDeLinha { static string[] Dividir(string linha, string
  delimitador) }` — usado pelo endpoint de validação (Task 13) e testável isoladamente.

- [ ] **Step 1: Criar `DivisorDeLinha.cs`**

Reusa o mesmo parser (CsvHelper via `OpcoesLayout`) que
`LayoutValidationEngine.Validar(IEnumerable<string> linhas, OpcoesLayout, ...)` já usa
internamente para o caminho de linha delimitada sem cabeçalho — mesmo tratamento de aspas
e escape do resto da biblioteca, sem duplicar a configuração do CsvHelper:

```csharp
using CsvHelper;
using LayoutValidator.Core;

namespace LayoutValidator.Api.Validacao;

public static class DivisorDeLinha
{
    public static string[] Dividir(string linha, string delimitador)
    {
        var opcoes = new OpcoesLayout { Delimitador = delimitador, Cabecalho = ModoCabecalho.Ausente };
        var configuracao = opcoes.ParaConfiguracaoCsv();

        using var leitor = new StringReader(linha);
        using var parser = new CsvParser(leitor, configuracao);

        return parser.Read() ? (parser.Record ?? Array.Empty<string>()) : Array.Empty<string>();
    }
}
```

- [ ] **Step 2: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Validacao/DivisorDeLinhaTestes.cs`:

```csharp
using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class DivisorDeLinhaTestes
{
    [Fact]
    public void Dividir_QuebraPeloDelimitadorInformado()
    {
        var campos = DivisorDeLinha.Dividir("12345678901;João;30", ";");

        Assert.Equal(new[] { "12345678901", "João", "30" }, campos);
    }

    [Fact]
    public void Dividir_RespeitaAspasAoRedorDeCampoComDelimitadorDentro()
    {
        var campos = DivisorDeLinha.Dividir("\"Rua A; 123\";São Paulo", ";");

        Assert.Equal(new[] { "Rua A; 123", "São Paulo" }, campos);
    }

    [Fact]
    public void Dividir_AceitaDelimitadorDiferenteDePontoEVirgula()
    {
        var campos = DivisorDeLinha.Dividir("a|b|c", "|");

        Assert.Equal(new[] { "a", "b", "c" }, campos);
    }

    [Fact]
    public void Dividir_LinhaVaziaRetornaArrayVazio()
    {
        var campos = DivisorDeLinha.Dividir("", ";");

        Assert.Empty(campos);
    }
}
```

- [ ] **Step 3: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter DivisorDeLinhaTestes`
Expected: todos os testes passam.

- [ ] **Step 4: Commit**

```bash
git add apps/LayoutValidator.Api/Validacao/DivisorDeLinha.cs tests/LayoutValidator.Api.Tests/Validacao/DivisorDeLinhaTestes.cs
git commit -m "feat(api): divisor de linha delimitada reusando OpcoesLayout do core"
```

---

## Task 7: AvaliadorDeCampo

**Files:**
- Create: `apps/LayoutValidator.Api/Validacao/ErroDeCampo.cs`
- Create: `apps/LayoutValidator.Api/Validacao/AvaliadorDeCampo.cs`
- Test: `tests/LayoutValidator.Api.Tests/Validacao/AvaliadorDeCampoTestes.cs`

**Interfaces:**
- Consumes: `CampoCadastrado`, `RegraCampoCadastrada` (Task 5), `ICatalogoDeRegras`,
  `RegraCadastrada` (Task 4).
- Produces: `record ErroDeCampo(string Campo, string ValorRaw, string Regra, string
  Mensagem)`; `static class AvaliadorDeCampo { static ErroDeCampo? Avaliar(CampoCadastrado
  campo, string valor, ICatalogoDeRegras catalogo) }` — usado pelo endpoint de validação
  (Task 13).

- [ ] **Step 1: Criar `ErroDeCampo.cs`**

```csharp
namespace LayoutValidator.Api.Validacao;

public sealed record ErroDeCampo(string Campo, string ValorRaw, string Regra, string Mensagem);
```

- [ ] **Step 2: Criar `AvaliadorDeCampo.cs`**

```csharp
using System.Text.Json;
using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Validacao;

/// <summary>
/// Avalia as regras de um campo na ordem cadastrada, com cascade-stop: para na primeira
/// regra que falhar naquele campo — mesmo comportamento de RuleLevelCascadeMode.Stop do
/// catálogo estático (LayoutValidator.Regras).
/// </summary>
public static class AvaliadorDeCampo
{
    public static ErroDeCampo? Avaliar(CampoCadastrado campo, string valor, ICatalogoDeRegras catalogo)
    {
        foreach (var regraCampo in campo.Regras.OrderBy(r => r.Ordem))
        {
            var regra = catalogo.Obter(regraCampo.ChaveRegra);
            var parametros = ParseParametros(regraCampo.ParametrosJson);

            if (!regra.Avaliar(valor, parametros))
                return new ErroDeCampo(campo.Nome, valor, regra.ObterCodigoErro(parametros), regra.MontarMensagem(campo.Nome, parametros));
        }

        return null;
    }

    private static JsonElement? ParseParametros(string? parametrosJson) =>
        parametrosJson is null ? null : JsonDocument.Parse(parametrosJson).RootElement;
}
```

- [ ] **Step 3: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Validacao/AvaliadorDeCampoTestes.cs`:

```csharp
using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class AvaliadorDeCampoTestes
{
    private readonly ICatalogoDeRegras _catalogo = new CatalogoDeRegras();

    [Fact]
    public void Avaliar_RetornaNuloQuandoTodasAsRegrasPassam()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Cpf",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
        };

        Assert.Null(AvaliadorDeCampo.Avaliar(campo, "11144477735", _catalogo));
    }

    [Fact]
    public void Avaliar_RetornaErroDaRegraQueFalhou()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Cpf",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
        };

        var erro = AvaliadorDeCampo.Avaliar(campo, "12345678900", _catalogo);

        Assert.NotNull(erro);
        Assert.Equal("Cpf", erro!.Campo);
        Assert.Equal("CpfInvalido", erro.Regra);
    }

    [Fact]
    public void Avaliar_ParaNaPrimeiraRegraQueFalha_CascadeStop()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Idade",
            Ordem = 0,
            Regras =
            {
                new RegraCampoCadastrada { ChaveRegra = "Inteiro", Ordem = 0 },
                new RegraCampoCadastrada
                {
                    ChaveRegra = "InteiroEntre",
                    ParametrosJson = """{"minimo":18,"maximo":60}""",
                    Ordem = 1
                }
            }
        };

        // "abc" falha em Inteiro (primeira regra) e também falharia em InteiroEntre (segunda) —
        // o teste só prova cascade-stop porque o erro retornado é o de Inteiro, não o de
        // InteiroEntre: confirma que a segunda regra nunca chegou a ser avaliada.
        var erro = AvaliadorDeCampo.Avaliar(campo, "abc", _catalogo);

        Assert.NotNull(erro);
        Assert.Equal("InteiroInvalido", erro!.Regra);
    }

    [Fact]
    public void Avaliar_CampoOpcionalVazioPassaSemObrigatorio()
    {
        var campo = new CampoCadastrado
        {
            Nome = "Observacao",
            Ordem = 0,
            Regras = { new RegraCampoCadastrada { ChaveRegra = "ComprimentoMaximo", ParametrosJson = """{"maximo":100}""", Ordem = 0 } }
        };

        Assert.Null(AvaliadorDeCampo.Avaliar(campo, "", _catalogo));
    }
}
```

- [ ] **Step 4: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter AvaliadorDeCampoTestes`
Expected: todos os testes passam.

- [ ] **Step 5: Commit**

```bash
git add apps/LayoutValidator.Api/Validacao/ErroDeCampo.cs apps/LayoutValidator.Api/Validacao/AvaliadorDeCampo.cs tests/LayoutValidator.Api.Tests/Validacao/AvaliadorDeCampoTestes.cs
git commit -m "feat(api): avaliador de campo com cascade-stop por regra"
```

---

## Task 8: Contratos (DTOs) + MapeadorDeLayout

**Files:**
- Create: `apps/LayoutValidator.Api/Contratos/LayoutContratos.cs`
- Create: `apps/LayoutValidator.Api/Contratos/ValidacaoContratos.cs`
- Create: `apps/LayoutValidator.Api/Contratos/RegraContratos.cs`
- Create: `apps/LayoutValidator.Api/Contratos/MapeadorDeLayout.cs`
- Test: `tests/LayoutValidator.Api.Tests/Contratos/MapeadorDeLayoutTestes.cs`

**Interfaces:**
- Consumes: `LayoutCadastrado`, `CampoCadastrado`, `RegraCampoCadastrada` (Task 5).
- Produces: records `LayoutRequest`, `CampoRequest`, `RegraCampoRequest`, `LayoutResponse`,
  `CampoResponse`, `RegraCampoResponse`, `ValidarRequest`, `ValidarResponse`,
  `RegraDisponivelResponse`, `ParametroEsperadoResponse`; `static class MapeadorDeLayout {
  static LayoutCadastrado ParaEntidade(LayoutRequest requisicao); static LayoutResponse
  ParaResposta(LayoutCadastrado layout); }`. Usado pelos endpoints (Tasks 10-13).

- [ ] **Step 1: Criar `Contratos/LayoutContratos.cs`**

```csharp
using System.Text.Json;

namespace LayoutValidator.Api.Contratos;

public sealed record RegraCampoRequest(string ChaveRegra, JsonElement? ParametrosJson);
public sealed record CampoRequest(string Nome, IReadOnlyList<RegraCampoRequest> Regras);
public sealed record LayoutRequest(string Codigo, string Nome, string Delimitador, IReadOnlyList<CampoRequest> Campos);

public sealed record RegraCampoResponse(string ChaveRegra, JsonElement? ParametrosJson);
public sealed record CampoResponse(string Nome, int Ordem, IReadOnlyList<RegraCampoResponse> Regras);
public sealed record LayoutResponse(string Codigo, string Nome, string Delimitador, IReadOnlyList<CampoResponse> Campos);
```

- [ ] **Step 2: Criar `Contratos/ValidacaoContratos.cs`**

```csharp
namespace LayoutValidator.Api.Contratos;

public sealed record ValidarRequest(string Linha);
public sealed record ErroDeCampoResponse(string Campo, string ValorRaw, string Regra, string Mensagem);
public sealed record ValidarResponse(bool Aderente, IReadOnlyList<ErroDeCampoResponse> Erros);
```

- [ ] **Step 3: Criar `Contratos/RegraContratos.cs`**

```csharp
namespace LayoutValidator.Api.Contratos;

public sealed record ParametroEsperadoResponse(string Nome, string Tipo, bool Obrigatorio);
public sealed record RegraDisponivelResponse(string Chave, IReadOnlyList<ParametroEsperadoResponse> ParametrosEsperados);
```

- [ ] **Step 4: Criar `Contratos/MapeadorDeLayout.cs`**

`Ordem` de campos e regras não vem do request — é derivada da posição no array, evitando
que o cliente possa cadastrar `Ordem` duplicada ou fora de sequência:

```csharp
using LayoutValidator.Api.Modelos;
using System.Text.Json;

namespace LayoutValidator.Api.Contratos;

public static class MapeadorDeLayout
{
    public static LayoutCadastrado ParaEntidade(LayoutRequest requisicao) => new()
    {
        Codigo = requisicao.Codigo,
        Nome = requisicao.Nome,
        Delimitador = requisicao.Delimitador,
        Campos = requisicao.Campos.Select((campo, indiceCampo) => new CampoCadastrado
        {
            Nome = campo.Nome,
            Ordem = indiceCampo,
            Regras = campo.Regras.Select((regra, indiceRegra) => new RegraCampoCadastrada
            {
                ChaveRegra = regra.ChaveRegra,
                ParametrosJson = regra.ParametrosJson?.GetRawText(),
                Ordem = indiceRegra
            }).ToList()
        }).ToList()
    };

    public static LayoutResponse ParaResposta(LayoutCadastrado layout) => new(
        layout.Codigo,
        layout.Nome,
        layout.Delimitador,
        layout.Campos
            .OrderBy(campo => campo.Ordem)
            .Select(campo => new CampoResponse(
                campo.Nome,
                campo.Ordem,
                campo.Regras
                    .OrderBy(regra => regra.Ordem)
                    .Select(regra => new RegraCampoResponse(
                        regra.ChaveRegra,
                        regra.ParametrosJson is null ? null : JsonDocument.Parse(regra.ParametrosJson).RootElement))
                    .ToList()))
            .ToList());
}
```

- [ ] **Step 5: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Contratos/MapeadorDeLayoutTestes.cs`:

```csharp
using System.Text.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Contratos;

public class MapeadorDeLayoutTestes
{
    [Fact]
    public void ParaEntidade_DerivaOrdemDaPosicaoNoArray()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", Array.Empty<RegraCampoRequest>())
        });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        Assert.Equal(0, layout.Campos[0].Ordem);
        Assert.Equal(1, layout.Campos[1].Ordem);
        Assert.Equal(0, layout.Campos[0].Regras[0].Ordem);
        Assert.Equal(1, layout.Campos[0].Regras[1].Ordem);
    }

    [Fact]
    public void ParaEntidade_SerializaParametrosJsonComoTextoCru()
    {
        var parametros = JsonDocument.Parse("""{"minimo":1,"maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        Assert.Equal("""{"minimo":1,"maximo":60}""", layout.Campos[0].Regras[0].ParametrosJson);
    }

    [Fact]
    public void ParaResposta_OrdenaCamposERegrasPelaOrdemCadastrada()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null) }),
            new CampoRequest("Nome", Array.Empty<RegraCampoRequest>())
        });
        var layout = MapeadorDeLayout.ParaEntidade(requisicao);

        var resposta = MapeadorDeLayout.ParaResposta(layout);

        Assert.Equal("Cpf", resposta.Campos[0].Nome);
        Assert.Equal("Nome", resposta.Campos[1].Nome);
        Assert.Equal("Obrigatorio", resposta.Campos[0].Regras[0].ChaveRegra);
    }
}
```

- [ ] **Step 6: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter MapeadorDeLayoutTestes`
Expected: todos os testes passam.

- [ ] **Step 7: Commit**

```bash
git add apps/LayoutValidator.Api/Contratos tests/LayoutValidator.Api.Tests/Contratos
git commit -m "feat(api): contratos de request/response e mapeador de layout"
```

---

## Task 9: ValidadorDeDefinicaoDeLayout

**Files:**
- Create: `apps/LayoutValidator.Api/Validacao/ValidadorDeDefinicaoDeLayout.cs`
- Test: `tests/LayoutValidator.Api.Tests/Validacao/ValidadorDeDefinicaoDeLayoutTestes.cs`

**Interfaces:**
- Consumes: `LayoutRequest`, `CampoRequest`, `RegraCampoRequest` (Task 8), `ICatalogoDeRegras`,
  `RegraCadastrada`, `ParametroEsperado`, `TipoParametro` (Task 4/2).
- Produces: `static class ValidadorDeDefinicaoDeLayout { static IReadOnlyList<string>
  Validar(LayoutRequest requisicao, ICatalogoDeRegras catalogo) }` — usado pelos endpoints
  de cadastro (Tasks 10 e 11) para rejeitar layout mal formado antes de salvar.

- [ ] **Step 1: Criar `ValidadorDeDefinicaoDeLayout.cs`**

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Validacao;

/// <summary>
/// Valida a definição de um layout no momento do cadastro — Código no formato esperado,
/// chave de regra existe no catálogo, e todo parâmetro obrigatório da regra está presente
/// com o tipo esperado. Existe para que um layout mal cadastrado nunca chegue a ser salvo:
/// sem isso, o erro só apareceria depois, quando alguém chamasse /validar com dado real.
/// </summary>
public static class ValidadorDeDefinicaoDeLayout
{
    // Código vira segmento de URL (/layouts/{codigo}) — só letras e números, até 20
    // caracteres, pra não virar um segundo nome descritivo (ver ADR-0002).
    private static readonly Regex PadraoCodigo = new(@"^[A-Za-z0-9]{1,20}$", RegexOptions.Compiled);

    public static IReadOnlyList<string> Validar(LayoutRequest requisicao, ICatalogoDeRegras catalogo)
    {
        var erros = new List<string>();

        if (!PadraoCodigo.IsMatch(requisicao.Codigo))
            erros.Add($"Código '{requisicao.Codigo}' inválido: use só letras e números, até 20 caracteres.");

        foreach (var campo in requisicao.Campos)
        {
            foreach (var regraCampo in campo.Regras)
                ValidarRegraDoCampo(campo, regraCampo, catalogo, erros);
        }

        return erros;
    }

    private static void ValidarRegraDoCampo(CampoRequest campo, RegraCampoRequest regraCampo, ICatalogoDeRegras catalogo, List<string> erros)
    {
        if (!catalogo.Existe(regraCampo.ChaveRegra))
        {
            erros.Add($"Campo '{campo.Nome}': regra '{regraCampo.ChaveRegra}' não existe no catálogo.");
            return;
        }

        var regra = catalogo.Obter(regraCampo.ChaveRegra);

        foreach (var parametroEsperado in regra.ParametrosEsperados.Where(p => p.Obrigatorio))
        {
            if (!TemParametroDoTipoEsperado(regraCampo.ParametrosJson, parametroEsperado))
            {
                erros.Add($"Campo '{campo.Nome}': regra '{regraCampo.ChaveRegra}' exige o parâmetro " +
                          $"'{parametroEsperado.Nome}' ({parametroEsperado.Tipo}).");
            }
        }
    }

    private static bool TemParametroDoTipoEsperado(JsonElement? parametros, ParametroEsperado parametroEsperado)
    {
        if (parametros is null || parametros.Value.ValueKind != JsonValueKind.Object)
            return false;

        if (!parametros.Value.TryGetProperty(parametroEsperado.Nome, out var valor))
            return false;

        return parametroEsperado.Tipo switch
        {
            TipoParametro.Inteiro => valor.ValueKind == JsonValueKind.Number && valor.TryGetInt64(out _),
            TipoParametro.Decimal => valor.ValueKind == JsonValueKind.Number,
            TipoParametro.Texto => valor.ValueKind == JsonValueKind.String,
            TipoParametro.ListaDeTexto => valor.ValueKind == JsonValueKind.Array,
            _ => false
        };
    }
}
```

- [ ] **Step 2: Escrever os testes**

Crie `tests/LayoutValidator.Api.Tests/Validacao/ValidadorDeDefinicaoDeLayoutTestes.cs`:

```csharp
using System.Text.Json;
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;

namespace LayoutValidator.Api.Tests.Validacao;

public class ValidadorDeDefinicaoDeLayoutTestes
{
    private readonly ICatalogoDeRegras _catalogo = new CatalogoDeRegras();

    [Fact]
    public void Validar_SemErrosParaLayoutBemFormado()
    {
        var parametros = JsonDocument.Parse("""{"minimo":18,"maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }

    [Theory]
    [InlineData("FUNC 2024")]                    // espaço
    [InlineData("FUNC-2024")]                     // hífen
    [InlineData("FUNC/2024")]                     // vira segmento de URL — não pode ter barra
    [InlineData("CODIGOMUITOGRANDEDEMAISPARASER")] // mais de 20 caracteres
    [InlineData("")]
    public void Validar_RejeitaCodigoForaDoFormato(string codigo)
    {
        var requisicao = new LayoutRequest(codigo, "Pessoa", ";", Array.Empty<CampoRequest>());

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("inválido"));
    }

    [Fact]
    public void Validar_RejeitaChaveDeRegraInexistente()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("NaoExiste", null) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("NaoExiste") && e.Contains("não existe no catálogo"));
    }

    [Fact]
    public void Validar_RejeitaParametroObrigatorioFaltando()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", null) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("minimo"));
        Assert.Contains(erros, e => e.Contains("maximo"));
    }

    [Fact]
    public void Validar_RejeitaParametroComTipoErrado()
    {
        var parametros = JsonDocument.Parse("""{"minimo":"dezoito","maximo":60}""").RootElement;
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", parametros) })
        });

        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo);

        Assert.Contains(erros, e => e.Contains("minimo"));
    }

    [Fact]
    public void Validar_RegraSemParametrosObrigatoriosNuncaGeraErro()
    {
        var requisicao = new LayoutRequest("PESSOA1", "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) })
        });

        Assert.Empty(ValidadorDeDefinicaoDeLayout.Validar(requisicao, _catalogo));
    }
}
```

- [ ] **Step 3: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter ValidadorDeDefinicaoDeLayoutTestes`
Expected: todos os testes passam.

- [ ] **Step 4: Commit**

```bash
git add apps/LayoutValidator.Api/Validacao/ValidadorDeDefinicaoDeLayout.cs tests/LayoutValidator.Api.Tests/Validacao/ValidadorDeDefinicaoDeLayoutTestes.cs
git commit -m "feat(api): valida definicao do layout contra o schema do catalogo no cadastro"
```

---

## Task 10: Endpoint — criar / listar / obter layout

**Files:**
- Create: `apps/LayoutValidator.Api/Endpoints/LayoutsEndpoints.cs`
- Modify: `apps/LayoutValidator.Api/Program.cs`
- Modify: `tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj`
- Create: `tests/LayoutValidator.Api.Tests/Integracao/ApiFactoryDeTeste.cs`
- Test: `tests/LayoutValidator.Api.Tests/Integracao/LayoutsEndpointsTestes.cs`

**Interfaces:**
- Consumes: `ApiDbContext` (Task 5), `ICatalogoDeRegras` (Task 4), `LayoutRequest`,
  `LayoutResponse`, `MapeadorDeLayout` (Task 8), `ValidadorDeDefinicaoDeLayout` (Task 9).
- Produces: `static class LayoutsEndpoints { static void MapLayoutsEndpoints(this
  IEndpointRouteBuilder rotas) }` mapeando `POST /layouts`, `GET /layouts`, `GET
  /layouts/{codigo}` (PUT/DELETE entram na Task 11, no mesmo método); `class
  ApiFactoryDeTeste : WebApplicationFactory<Program>` reusada pelas próximas tasks de
  integração.

- [ ] **Step 1: Adicionar o pacote de testes de integração**

Run: `dotnet add tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing`

- [ ] **Step 2: Criar `Endpoints/LayoutsEndpoints.cs`**

```csharp
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Endpoints;

public static class LayoutsEndpoints
{
    public static void MapLayoutsEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/layouts", CriarAsync);
        rotas.MapGet("/layouts", ListarAsync);
        rotas.MapGet("/layouts/{codigo}", ObterAsync);
        rotas.MapPut("/layouts/{codigo}", AtualizarAsync);
        rotas.MapDelete("/layouts/{codigo}", RemoverAsync);
    }

    private static async Task<IResult> CriarAsync(LayoutRequest requisicao, ApiDbContext db, ICatalogoDeRegras catalogo)
    {
        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, catalogo);
        if (erros.Count > 0)
            return Results.BadRequest(new { erros });

        if (await db.Layouts.AnyAsync(l => l.Codigo == requisicao.Codigo))
            return Results.Conflict(new { erro = $"Já existe um layout com o código '{requisicao.Codigo}'." });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);
        layout.CriadoEm = DateTime.UtcNow;
        layout.AtualizadoEm = layout.CriadoEm;

        db.Layouts.Add(layout);
        await db.SaveChangesAsync();

        return Results.Created($"/layouts/{layout.Codigo}", MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> ListarAsync(ApiDbContext db)
    {
        var layouts = await CarregarCompleto(db).ToListAsync();
        return Results.Ok(layouts.Select(MapeadorDeLayout.ParaResposta));
    }

    private static async Task<IResult> ObterAsync(string codigo, ApiDbContext db)
    {
        var layout = await CarregarCompleto(db).FirstOrDefaultAsync(l => l.Codigo == codigo);
        return layout is null ? Results.NotFound() : Results.Ok(MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> AtualizarAsync(string codigo, LayoutRequest requisicao, ApiDbContext db, ICatalogoDeRegras catalogo)
    {
        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, catalogo);
        if (erros.Count > 0)
            return Results.BadRequest(new { erros });

        var layout = await CarregarCompleto(db).FirstOrDefaultAsync(l => l.Codigo == codigo);
        if (layout is null)
            return Results.NotFound();

        if (requisicao.Codigo != codigo && await db.Layouts.AnyAsync(l => l.Codigo == requisicao.Codigo))
            return Results.Conflict(new { erro = $"Já existe um layout com o código '{requisicao.Codigo}'." });

        var atualizado = MapeadorDeLayout.ParaEntidade(requisicao);
        layout.Codigo = atualizado.Codigo;
        layout.Nome = atualizado.Nome;
        layout.Delimitador = atualizado.Delimitador;
        layout.Campos.Clear();
        layout.Campos.AddRange(atualizado.Campos);
        layout.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> RemoverAsync(string codigo, ApiDbContext db)
    {
        var layout = await db.Layouts.FirstOrDefaultAsync(l => l.Codigo == codigo);
        if (layout is null)
            return Results.NotFound();

        db.Layouts.Remove(layout);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static IQueryable<LayoutCadastrado> CarregarCompleto(ApiDbContext db) =>
        db.Layouts.Include(l => l.Campos).ThenInclude(c => c.Regras);
}
```

- [ ] **Step 3: Mapear o endpoint no `Program.cs`**

Atualize `apps/LayoutValidator.Api/Program.cs`:

```csharp
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Endpoints;
using LayoutValidator.Api.Regras;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.AddDbContext<ApiDbContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

using (var escopoDeInicializacao = app.Services.CreateScope())
{
    escopoDeInicializacao.ServiceProvider.GetRequiredService<ApiDbContext>().Database.Migrate();
}

app.MapLayoutsEndpoints();

app.Run();

public partial class Program;
```

- [ ] **Step 4: Criar a factory de teste de integração**

Substitui o registro de `ApiDbContext` por um SQLite isolado (arquivo temporário por
instância de factory), e roda `Database.Migrate()` na inicialização:

Crie `tests/LayoutValidator.Api.Tests/Integracao/ApiFactoryDeTeste.cs`:

```csharp
using LayoutValidator.Api.Dados;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LayoutValidator.Api.Tests.Integracao;

public sealed class ApiFactoryDeTeste : WebApplicationFactory<Program>
{
    private readonly string _arquivoDeBanco = Path.Combine(Path.GetTempPath(), $"layoutvalidator-teste-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApiDbContext>>();
            services.AddDbContext<ApiDbContext>(opcoes => opcoes.UseSqlite($"Data Source={_arquivoDeBanco}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(_arquivoDeBanco))
            File.Delete(_arquivoDeBanco);
    }
}
```

- [ ] **Step 5: Escrever os testes de integração**

Crie `tests/LayoutValidator.Api.Tests/Integracao/LayoutsEndpointsTestes.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class LayoutsEndpointsTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public LayoutsEndpointsTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    private static LayoutRequest LayoutPessoaValido(string codigo) => new(
        codigo, "Pessoa", ";",
        new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", new[] { new RegraCampoRequest("Obrigatorio", null) })
        });

    [Fact]
    public async Task Post_CriaLayoutERetorna201ComOCodigoNaLocation()
    {
        var resposta = await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA1"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("/layouts/PESSOA1", resposta.Headers.Location?.OriginalString);

        var corpo = await resposta.Content.ReadFromJsonAsync<LayoutResponse>();
        Assert.Equal("PESSOA1", corpo!.Codigo);
        Assert.Equal(2, corpo.Campos.Count);
    }

    [Fact]
    public async Task Post_RejeitaCodigoDuplicadoCom409()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA2"));

        var resposta = await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA2"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_RejeitaLayoutComParametroFaltandoCom400()
    {
        var requisicao = new LayoutRequest("PESSOA3", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", null) })
        });

        var resposta = await _cliente.PostAsJsonAsync("/layouts", requisicao);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_ObtemLayoutPeloCodigo()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA4"));

        var resposta = await _cliente.GetAsync("/layouts/PESSOA4");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<LayoutResponse>();
        Assert.Equal("PESSOA4", corpo!.Codigo);
    }

    [Fact]
    public async Task Get_RetornaNotFoundParaCodigoInexistente()
    {
        var resposta = await _cliente.GetAsync("/layouts/NAOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetLista_RetornaOsLayoutsCadastrados()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA5"));

        var resposta = await _cliente.GetAsync("/layouts");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<List<LayoutResponse>>();
        Assert.Contains(corpo!, l => l.Codigo == "PESSOA5");
    }
}
```

- [ ] **Step 6: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter LayoutsEndpointsTestes`
Expected: todos os testes passam.

- [ ] **Step 7: Commit**

```bash
git add apps/LayoutValidator.Api/Endpoints/LayoutsEndpoints.cs apps/LayoutValidator.Api/Program.cs tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj tests/LayoutValidator.Api.Tests/Integracao
git commit -m "feat(api): endpoint de criar/listar/obter layout"
```

---

## Task 11: Endpoint — atualizar / remover layout

**Files:**
- Test: `tests/LayoutValidator.Api.Tests/Integracao/LayoutsEndpointsTestes.cs` (Modify —
  adiciona casos de PUT/DELETE; as rotas já foram mapeadas na Task 10, dentro do mesmo
  `MapLayoutsEndpoints`)

**Interfaces:**
- Consumes: `LayoutsEndpoints.AtualizarAsync`/`RemoverAsync` (já implementados na Task 10),
  `ApiFactoryDeTeste` (Task 10).

> As rotas `PUT /layouts/{codigo}` e `DELETE /layouts/{codigo}` já foram implementadas e
> mapeadas na Task 10 (um único `MapLayoutsEndpoints` cobre as cinco rotas de layout). Esta
> task só adiciona a cobertura de teste que faltou.

- [ ] **Step 1: Adicionar os testes de PUT e DELETE**

Adicione à classe `LayoutsEndpointsTestes` em
`tests/LayoutValidator.Api.Tests/Integracao/LayoutsEndpointsTestes.cs`:

```csharp
    [Fact]
    public async Task Put_SobrescreveCamposDoLayoutExistente()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA6"));

        var novaDefinicao = new LayoutRequest("PESSOA6", "Pessoa Atualizada", "|", new[]
        {
            new CampoRequest("Email", Array.Empty<RegraCampoRequest>())
        });

        var resposta = await _cliente.PutAsJsonAsync("/layouts/PESSOA6", novaDefinicao);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<LayoutResponse>();
        Assert.Equal("Pessoa Atualizada", corpo!.Nome);
        Assert.Equal("|", corpo.Delimitador);
        Assert.Single(corpo.Campos);
        Assert.Equal("Email", corpo.Campos[0].Nome);
    }

    [Fact]
    public async Task Put_RetornaNotFoundParaCodigoInexistente()
    {
        var resposta = await _cliente.PutAsJsonAsync("/layouts/NAOEXISTE", LayoutPessoaValido("NAOEXISTE"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Put_RejeitaLayoutComParametroFaltandoCom400()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA7"));

        var definicaoInvalida = new LayoutRequest("PESSOA7", "Pessoa", ";", new[]
        {
            new CampoRequest("Idade", new[] { new RegraCampoRequest("InteiroEntre", null) })
        });

        var resposta = await _cliente.PutAsJsonAsync("/layouts/PESSOA7", definicaoInvalida);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_RemoveOLayoutCadastrado()
    {
        await _cliente.PostAsJsonAsync("/layouts", LayoutPessoaValido("PESSOA8"));

        var resposta = await _cliente.DeleteAsync("/layouts/PESSOA8");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/layouts/PESSOA8")).StatusCode);
    }

    [Fact]
    public async Task Delete_RetornaNotFoundParaCodigoInexistente()
    {
        var resposta = await _cliente.DeleteAsync("/layouts/NAOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
```

- [ ] **Step 2: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter LayoutsEndpointsTestes`
Expected: todos os testes (criação + listagem + atualização + remoção) passam.

- [ ] **Step 3: Commit**

```bash
git add tests/LayoutValidator.Api.Tests/Integracao/LayoutsEndpointsTestes.cs
git commit -m "test(api): cobre atualizar e remover layout via PUT/DELETE"
```

---

## Task 12: Endpoint — GET /regras

**Files:**
- Create: `apps/LayoutValidator.Api/Endpoints/RegrasEndpoints.cs`
- Modify: `apps/LayoutValidator.Api/Program.cs`
- Test: `tests/LayoutValidator.Api.Tests/Integracao/RegrasEndpointTestes.cs`

**Interfaces:**
- Consumes: `ICatalogoDeRegras`, `RegraCadastrada` (Task 4), `RegraDisponivelResponse`,
  `ParametroEsperadoResponse` (Task 8).
- Produces: `static class RegrasEndpoints { static void MapRegrasEndpoints(this
  IEndpointRouteBuilder rotas) }` mapeando `GET /regras`.

- [ ] **Step 1: Criar `Endpoints/RegrasEndpoints.cs`**

```csharp
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Endpoints;

public static class RegrasEndpoints
{
    public static void MapRegrasEndpoints(this IEndpointRouteBuilder rotas) =>
        rotas.MapGet("/regras", (ICatalogoDeRegras catalogo) => Results.Ok(
            catalogo.Todas
                .OrderBy(regra => regra.Chave)
                .Select(regra => new RegraDisponivelResponse(
                    regra.Chave,
                    regra.ParametrosEsperados
                        .Select(p => new ParametroEsperadoResponse(p.Nome, p.Tipo.ToString(), p.Obrigatorio))
                        .ToList()))));
}
```

- [ ] **Step 2: Mapear o endpoint no `Program.cs`**

Em `apps/LayoutValidator.Api/Program.cs`, adicione a chamada junto de `app.MapLayoutsEndpoints();`:

```csharp
app.MapLayoutsEndpoints();
app.MapRegrasEndpoints();
```

- [ ] **Step 3: Escrever os testes de integração**

Crie `tests/LayoutValidator.Api.Tests/Integracao/RegrasEndpointTestes.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class RegrasEndpointTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public RegrasEndpointTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Get_ListaAs19RegrasDoCatalogo()
    {
        var resposta = await _cliente.GetAsync("/regras");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<List<RegraDisponivelResponse>>();
        Assert.Equal(19, corpo!.Count);
    }

    [Fact]
    public async Task Get_DescreveOsParametrosEsperadosDeInteiroEntre()
    {
        var resposta = await _cliente.GetAsync("/regras");
        var corpo = await resposta.Content.ReadFromJsonAsync<List<RegraDisponivelResponse>>();

        var inteiroEntre = corpo!.Single(r => r.Chave == "InteiroEntre");

        Assert.Equal(2, inteiroEntre.ParametrosEsperados.Count);
        Assert.Contains(inteiroEntre.ParametrosEsperados, p => p.Nome == "minimo" && p.Obrigatorio);
        Assert.Contains(inteiroEntre.ParametrosEsperados, p => p.Nome == "maximo" && p.Obrigatorio);
    }
}
```

- [ ] **Step 4: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter RegrasEndpointTestes`
Expected: todos os testes passam.

- [ ] **Step 5: Commit**

```bash
git add apps/LayoutValidator.Api/Endpoints/RegrasEndpoints.cs apps/LayoutValidator.Api/Program.cs tests/LayoutValidator.Api.Tests/Integracao/RegrasEndpointTestes.cs
git commit -m "feat(api): endpoint GET /regras expondo o catalogo disponivel"
```

---

## Task 13: Endpoint — POST /layouts/{codigo}/validar

**Files:**
- Create: `apps/LayoutValidator.Api/Endpoints/ValidacaoEndpoints.cs`
- Modify: `apps/LayoutValidator.Api/Program.cs`
- Test: `tests/LayoutValidator.Api.Tests/Integracao/ValidacaoEndpointTestes.cs`

**Interfaces:**
- Consumes: `ApiDbContext` (Task 5), `ICatalogoDeRegras` (Task 4), `DivisorDeLinha` (Task
  6), `AvaliadorDeCampo`, `ErroDeCampo` (Task 7), `ValidarRequest`, `ValidarResponse`,
  `ErroDeCampoResponse` (Task 8).
- Produces: `static class ValidacaoEndpoints { static void MapValidacaoEndpoints(this
  IEndpointRouteBuilder rotas) }` mapeando `POST /layouts/{codigo}/validar`.

- [ ] **Step 1: Criar `Endpoints/ValidacaoEndpoints.cs`**

```csharp
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Endpoints;

public static class ValidacaoEndpoints
{
    public static void MapValidacaoEndpoints(this IEndpointRouteBuilder rotas) =>
        rotas.MapPost("/layouts/{codigo}/validar", ValidarAsync);

    private static async Task<IResult> ValidarAsync(string codigo, ValidarRequest requisicao, ApiDbContext db, ICatalogoDeRegras catalogo)
    {
        var layout = await db.Layouts
            .Include(l => l.Campos).ThenInclude(c => c.Regras)
            .FirstOrDefaultAsync(l => l.Codigo == codigo);

        if (layout is null)
            return Results.NotFound();

        var campos = layout.Campos.OrderBy(c => c.Ordem).ToList();
        var valores = DivisorDeLinha.Dividir(requisicao.Linha, layout.Delimitador);

        if (valores.Length != campos.Count)
        {
            var erroDeEstrutura = new ErroDeCampoResponse(
                "(linha)",
                string.Join(layout.Delimitador, valores),
                "EstruturaDeColunas",
                $"Linha com {valores.Length} coluna(s), esperado {campos.Count}.");

            return Results.Ok(new ValidarResponse(false, new[] { erroDeEstrutura }));
        }

        var erros = new List<ErroDeCampoResponse>();
        for (var i = 0; i < campos.Count; i++)
        {
            var erro = AvaliadorDeCampo.Avaliar(campos[i], valores[i], catalogo);
            if (erro is not null)
                erros.Add(new ErroDeCampoResponse(erro.Campo, erro.ValorRaw, erro.Regra, erro.Mensagem));
        }

        return Results.Ok(new ValidarResponse(erros.Count == 0, erros));
    }
}
```

- [ ] **Step 2: Mapear o endpoint no `Program.cs`**

Em `apps/LayoutValidator.Api/Program.cs`, adicione:

```csharp
app.MapLayoutsEndpoints();
app.MapRegrasEndpoints();
app.MapValidacaoEndpoints();
```

- [ ] **Step 3: Escrever os testes de integração**

Crie `tests/LayoutValidator.Api.Tests/Integracao/ValidacaoEndpointTestes.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using LayoutValidator.Api.Contratos;

namespace LayoutValidator.Api.Tests.Integracao;

public class ValidacaoEndpointTestes : IClassFixture<ApiFactoryDeTeste>
{
    private readonly HttpClient _cliente;

    public ValidacaoEndpointTestes(ApiFactoryDeTeste fabrica) => _cliente = fabrica.CreateClient();

    private async Task CadastrarLayoutPessoaAsync(string codigo)
    {
        var requisicao = new LayoutRequest(codigo, "Pessoa", ";", new[]
        {
            new CampoRequest("Cpf", new[] { new RegraCampoRequest("Obrigatorio", null), new RegraCampoRequest("Cpf", null) }),
            new CampoRequest("Nome", new[] { new RegraCampoRequest("Obrigatorio", null) })
        });

        await _cliente.PostAsJsonAsync("/layouts", requisicao);
    }

    [Fact]
    public async Task Validar_LinhaAderenteRetornaAderenteTrueSemErros()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA1");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA1/validar", new ValidarRequest("11144477735;João"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.True(corpo!.Aderente);
        Assert.Empty(corpo.Erros);
    }

    [Fact]
    public async Task Validar_CpfInvalidoRetornaErroDoCampo()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA2");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA2/validar", new ValidarRequest("12345678900;João"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Single(corpo.Erros);
        Assert.Equal("Cpf", corpo.Erros[0].Campo);
        Assert.Equal("CpfInvalido", corpo.Erros[0].Regra);
    }

    [Fact]
    public async Task Validar_ContagemDeColunasErradaRetornaEstruturaDeColunas()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA3");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA3/validar", new ValidarRequest("11144477735;João;Extra"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Equal("EstruturaDeColunas", corpo.Erros[0].Regra);
    }

    [Fact]
    public async Task Validar_LayoutInexistenteRetorna404()
    {
        var resposta = await _cliente.PostAsJsonAsync("/layouts/NAOEXISTE/validar", new ValidarRequest("qualquer;coisa"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Validar_CampoObrigatorioVazioRetornaErro()
    {
        await CadastrarLayoutPessoaAsync("VALPESSOA4");

        var resposta = await _cliente.PostAsJsonAsync("/layouts/VALPESSOA4/validar", new ValidarRequest("11144477735;"));

        var corpo = await resposta.Content.ReadFromJsonAsync<ValidarResponse>();
        Assert.False(corpo!.Aderente);
        Assert.Equal("Nome", corpo.Erros[0].Campo);
        Assert.Equal("CampoObrigatorio", corpo.Erros[0].Regra);
    }
}
```

- [ ] **Step 4: Rodar os testes**

Run: `dotnet test tests/LayoutValidator.Api.Tests/LayoutValidator.Api.Tests.csproj --filter ValidacaoEndpointTestes`
Expected: todos os testes passam.

- [ ] **Step 5: Rodar a suíte inteira**

Run: `dotnet test LayoutValidator.sln`
Expected: todos os testes de todos os projetos passam (core, regras, api).

- [ ] **Step 6: Commit**

```bash
git add apps/LayoutValidator.Api/Endpoints/ValidacaoEndpoints.cs apps/LayoutValidator.Api/Program.cs tests/LayoutValidator.Api.Tests/Integracao/ValidacaoEndpointTestes.cs
git commit -m "feat(api): endpoint POST /layouts/{codigo}/validar"
```

---

## Task 14: Revisão final e smoke test manual

**Files:**
- Modify: `README.md` (seção "Estrutura do projeto" e "Rodando")
- Modify: `.gitignore` (garantir que `*.db` do SQLite local não é versionado)

**Interfaces:**
- Nenhuma nova — task de fechamento, sem código de produção novo.

- [ ] **Step 1: Ignorar o arquivo SQLite local**

`.gitignore` hoje não tem entrada para `*.db` — adicione ao final do arquivo:

```
# Banco SQLite local da API de cadastro de layouts — recriado pela migração no startup.
apps/LayoutValidator.Api/*.db
```

- [ ] **Step 2: Atualizar o README com o novo app**

Em `README.md`, na árvore de "Estrutura do projeto", adicione uma entrada para o novo app
logo após `apps/LayoutValidator.TesteApp/`:

```
  apps/LayoutValidator.Api/                cadastro de layouts em banco + API local de validacao (ADR-0002)
    Modelos/                               entidades EF Core (LayoutCadastrado, CampoCadastrado, RegraCampoCadastrada)
    Regras/                                catalogo de regras cadastraveis por chave (equivalente dinamico de LayoutValidator.Regras)
    Dados/                                 ApiDbContext + migrations (SQLite)
    Validacao/                             DivisorDeLinha, AvaliadorDeCampo, ValidadorDeDefinicaoDeLayout
    Contratos/                             DTOs de request/response + MapeadorDeLayout
    Endpoints/                             LayoutsEndpoints, RegrasEndpoints, ValidacaoEndpoints
  tests/LayoutValidator.Api.Tests/         xUnit do app de cadastro (unidade + integracao via WebApplicationFactory)
```

Na seção "Rodando", adicione depois do bloco de comandos existente:

```bash
dotnet run --project apps/LayoutValidator.Api/LayoutValidator.Api.csproj
```

- [ ] **Step 3: Rodar a API manualmente e validar o fluxo completo**

Run (em background ou outro terminal): `dotnet run --project apps/LayoutValidator.Api/LayoutValidator.Api.csproj`
Expected: log mostra a URL local (ex. `http://localhost:5000`).

Cadastrar um layout:
```bash
curl -X POST http://localhost:5000/layouts -H "Content-Type: application/json" -d '{"codigo":"PESSOA1","nome":"Pessoa","delimitador":";","campos":[{"nome":"Cpf","regras":[{"chaveRegra":"Obrigatorio"},{"chaveRegra":"Cpf"}]},{"nome":"Nome","regras":[{"chaveRegra":"Obrigatorio"}]}]}'
```
Expected: `201 Created` com o layout cadastrado no corpo.

Validar uma linha aderente:
```bash
curl -X POST http://localhost:5000/layouts/PESSOA1/validar -H "Content-Type: application/json" -d '{"linha":"11144477735;João"}'
```
Expected: `{"Aderente":true,"Erros":[]}` (PascalCase — a política de nomes foi desativada no Step 4 da Task 1).

Validar uma linha não aderente:
```bash
curl -X POST http://localhost:5000/layouts/PESSOA1/validar -H "Content-Type: application/json" -d '{"linha":"123;João"}'
```
Expected: `{"Aderente":false,"Erros":[{"Campo":"Cpf","ValorRaw":"123","Regra":"CpfInvalido","Mensagem":"..."}]}`.

Consultar o catálogo:
```bash
curl http://localhost:5000/regras
```
Expected: array JSON com as 19 regras e seus parâmetros esperados.

Encerre o `dotnet run` (Ctrl+C ou finalize o processo em background) ao terminar.

- [ ] **Step 4: Conferir a ADR contra o comportamento observado**

Releia `docs/adr/0002-cadastro-de-layouts-via-api-local.md` e confirme que cada decisão
documentada (schema, endpoints, cascade-stop, validação no cadastro, contrato de vazio)
bate com o que foi implementado e observado no Step 3. Ajuste a ADR se algo divergiu
durante a implementação (mantendo `Status: Proposed` até o usuário revisar, ou atualizando
para `Accepted` se for esse o critério do projeto).

- [ ] **Step 5: Commit**

```bash
git add README.md .gitignore
git commit -m "docs: documenta o app LayoutValidator.Api no README"
```
