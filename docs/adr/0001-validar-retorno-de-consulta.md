# ADR-0001: Validar dados sem passar por arquivo

**Status:** Accepted
**Date:** 2026-08-17
**Deciders:** Affonso Spindler

## Contexto

A única porta de entrada da biblioteca era `IValidadorLayout<T>.Validar(TextReader)`,
implementada por `LayoutValidationEngine.Validar<TRaw,T>` — que lê o `TextReader` com
`CsvHelper.CsvReader`. O dado a validar precisava ser (ou virar) um arquivo delimitado.

Existe um caso de uso real onde isso não se encaixa: o dado a validar já é o retorno de
uma consulta (MySQL, Postgres, SQL Server) — já em memória, num `DataTable`/`DataSet`.
Antes desta mudança, a única forma de usar a ferramenta nesse caso seria serializar
esse retorno num arquivo/texto CSV temporário só para reabrir e validar — I/O e
trabalho de (des)serialização desperdiçados para um dado que nunca precisou virar
arquivo.

Card de acompanhamento: https://trello.com/c/5dTQ1ncV

## Decisão

`LayoutValidationEngine.Validar` ganhou dois overloads novos que aceitam dados já em
memória, sem arquivo, sem `OpcoesLayout` e sem exigir uma fachada de layout
(`XxxValidadorLayout : ValidadorLayoutBase<TRaw,T>`):

```csharp
// Linhas já separadas em campos — sem delimitador, sem escaping, casamento posicional.
public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
    IEnumerable<IReadOnlyList<string>> linhas,
    IValidator<TRaw> validador,
    ILayoutMapper<TRaw, T> mapper)
    where TRaw : class, new()

// Linhas já num texto delimitado por linha — quem chama escolhe o separador, casamento posicional.
public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
    IEnumerable<string> linhas,
    string delimitador,
    IValidator<TRaw> validador,
    ILayoutMapper<TRaw, T> mapper)
    where TRaw : class, new()
```

Princípio central: **a biblioteca não lida com `DataTable`/`DataSet`/`IDataReader` nem
com nenhum tipo de dado tipado.** Quem chama extrai os valores de onde quiser
(`DataTable`, `DataSet`, API, etc.), formata cada um como string do jeito que o
`Validador`/`Mapper` daquele layout espera, e manda pros overloads acima já como texto.
Os dois overloads são sempre **posicionais** — não existe conceito de cabeçalho aqui, a
ordem dos valores tem que bater com a ordem de declaração das propriedades `string` do
Raw Model (mesma convenção que `ModoCabecalho.Ausente` já usa no caminho de arquivo).

Uso completo em [Usando a Ferramenta § 6](../../wiki/Usando-a-Ferramenta.md#6-validando-dados-que-já-estão-em-memória-sem-arquivo).

## Como chegamos aqui

Duas ideias foram cogitadas e descartadas antes desta decisão:

**A biblioteca aceitar `DataTable`/`IDataReader` diretamente**, convertendo colunas
tipadas (`DateTime`, `decimal`) pra string na borda com `ToString`. Descartada porque o
formato que o .NET escolhe pra `ToString()` não bate com o formato que um
`Validador`/`Mapper` escrito pra CSV brasileiro espera (`dd/MM/yyyy`, decimal com
vírgula) — gerando falso positivo de "erro de formato" numa data que na verdade é
válida (ela só chegou como objeto `DateTime`, não como texto formatado). O ponto chave:
formato de uma coluna **tipada** é garantido pelo próprio tipo do banco — não existe
"data em formato errado" numa coluna `DateTime`, então checar formato nesse caso não
faz sentido; o problema real é só a *biblioteca* escolher um jeito arbitrário de
transformar aquele valor em texto, que pode não bater com o que o `Validador`/`Mapper`
daquele layout foi escrito esperando.

**A biblioteca serializar o `DataTable` pra CSV em memória sozinha.** Descartada por
reintroduzir o mesmo problema acima de forma automática/implícita, contrariando a
filosofia já documentada no README ("Não existe detecção automática de delimitador de
propósito... adivinhar errado... é pior que uma falha").

A saída das duas ideias descartadas era sempre a mesma: quem sabe o formato certo pra
cada valor tipado é quem está montando a linha (porque só ele sabe o que o
`Validador`/`Mapper` daquele layout espera), não a biblioteca. Daí a decisão final:
deixar 100% da formatação na mão de quem chama, e a biblioteca só recebe texto já
pronto — nos dois formatos de entrega que fizerem sentido pro chamador (valores já
separados, ou uma linha já delimitada).

## Consequências

- **Fica mais fácil**: validar dado que já está em memória (retorno de query, ou
  qualquer outra fonte) sem round-trip por arquivo, reusando o mesmo
  `Validador`/`Mapper` que um layout de arquivo já usa — desde que os valores cheguem
  formatados do jeito que eles esperam.
- **Responsabilidade explícita de quem chama**: formatar cada valor tipado (data,
  decimal) como texto antes de montar a linha, e — no overload de string delimitada —
  manter o `delimitador` passado consistente com o `OpcoesLayout.Delimitador` de uma
  eventual fachada do mesmo layout usada pro caminho de arquivo, já que não há vínculo
  automático entre os dois.
- **Sem mudança de escopo do formato de arquivo**: o caminho `Validar(TextReader, ...)`
  não mudou de comportamento observável — só teve o loop de validação+mapeamento
  extraído internamente pra reuso (`ValidarLinha`), coberto pelos testes já existentes.
- **Testes**: sem banco disponível no ambiente de teste (nem será instalado só pra
  isso), a suíte nova (`LayoutValidationEngineLinhasTestes`) constrói os dados 100% em
  memória — sem `DataTable`, sem fake de `IDataReader`, sem nenhuma dependência externa.
