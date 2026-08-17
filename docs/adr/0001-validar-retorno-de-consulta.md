# ADR-0001: Validar retorno de consulta sem passar por arquivo

**Status:** Proposed
**Date:** 2026-08-17
**Deciders:** Affonso Spindler

## Contexto

Hoje a única porta de entrada da biblioteca é `IValidadorLayout<T>.Validar(TextReader)`,
implementada por `LayoutValidationEngine.Validar<TRaw,T>` — que lê o `TextReader` com
`CsvHelper.CsvReader`. O dado a validar precisa ser (ou virar) um arquivo delimitado.

Existe um caso de uso real onde isso não se encaixa: o dado a validar já é o retorno de
uma consulta (MySQL, Postgres, SQL Server) — já em memória, já tipado, já nomeado por
coluna. Hoje a única forma de usar a ferramenta nesse caso seria serializar esse
retorno num arquivo/texto CSV temporário só para reabrir e validar — I/O e trabalho de
(des)serialização desperdiçados para um dado que nunca precisou virar texto.

Card de acompanhamento: https://trello.com/c/5dTQ1ncV
Mapeamento inicial (não técnico) do problema: [wiki/Possibilidades.md](../../wiki/Possibilidades.md#validar-retorno-de-consulta-sem-passar-por-arquivo)

Duas perguntas de design precisam ser respondidas antes de codar:

1. **Onde entra a fonte nova** — reaproveitar a engine existente (compartilhando o loop
   de validação+mapeamento) ou construir um caminho paralelo dedicado a `IDataReader`?
2. **O que vira o Raw Model** — hoje `TRaw` é sempre `string` por contrato (ver "Por que
   dois Models por layout" no README); um `IDataReader` entrega valor já tipado (`int`,
   `DateTime`, `decimal`). Manter `string` (convertendo na borda) ou aceitar um Raw
   Model tipado só para fonte DB?

Este ADR não implementa nada — define o caminho recomendado e o que fica em aberto
para quando a implementação for de fato encarada.

## Decisão (proposta)

**Opção A com Raw Model convertido para string na borda** — ver seções abaixo.
Estende `LayoutValidationEngine` com um novo método (ex.: `ValidarConsulta<TRaw,T>`),
extraindo do método `Validar` atual a parte que já é agnóstica de fonte (validação
FluentValidation + mapeamento) para um helper privado reusado pelos dois caminhos. A
conversão de cada coluna do `IDataReader` para `string` acontece na borda do novo
método, com `CultureInfo.InvariantCulture`, preservando o contrato "Raw é sempre
string" que o resto da biblioteca (regras do catálogo, `ExtrairValoresRaw`, relatório
de erros) já assume.

Esta é uma recomendação para discussão — o "Deciders" acima ainda precisa confirmar
antes de qualquer código ser escrito, conforme combinado (nenhuma implementação nesta
etapa).

## Opções consideradas

### Opção A: extrair o loop de validação+mapeamento, reusar entre CSV e `IDataReader`

| Dimensão | Avaliação |
|---|---|
| Complexidade | Média — exige refatorar `LayoutValidationEngine.Validar` sem quebrar o caminho CSV existente (que tem cobertura de teste hoje) |
| Risco de regressão no caminho CSV | Baixo-médio — a extração precisa manter idênticos os casos de linha malformada / `LayoutIncompativelException`, que são exclusivos do CSV |
| Reuso | Alto — um único loop de validação+mapeamento para as duas fontes; `TRaw`/`Validador`/`Mapper` e `IValidadorLayout<T>` continuam os mesmos |
| Extensibilidade futura | Alta — abre caminho natural para uma 3ª fonte (ex.: `IEnumerable<Dictionary<string,object>>` de uma API) sem duplicar o loop de novo |

**Pros:** uma fonte de verdade para a lógica de validação/mapeamento; menor chance de
as duas fontes divergirem em comportamento com o tempo (ex.: alguém corrige um bug no
loop de validação e esquece de replicar no outro).
**Cons:** toca código que hoje só serve o caminho CSV, testado e em uso — qualquer
refatoração ali carrega risco de regressão que precisa ser coberto por teste antes do
merge.

### Opção B: caminho paralelo dedicado, sem tocar a engine existente

Um método/classe novos (`ValidadorConsultaEngine` ou similar) implementam do zero o
loop de validação+mapeamento para `IDataReader`, sem compartilhar código com
`LayoutValidationEngine.Validar`.

| Dimensão | Avaliação |
|---|---|
| Complexidade | Baixa — não depende de entender/alterar a engine CSV existente |
| Risco de regressão no caminho CSV | Nenhum — zero linhas do caminho CSV são tocadas |
| Reuso | Baixo — duas implementações do mesmo loop de validação+mapeamento (FluentValidation + `Mapper`) |
| Extensibilidade futura | Baixa — uma 3ª fonte exigiria uma 3ª implementação do mesmo loop |

**Pros:** mais rápido de entregar, risco zero para o caminho CSV que já está em
produção/uso. Também é consistente com "regra dos três" (YAGNI): hoje só há um
consumidor concreto de `IDataReader`, então abstrair pode ser prematuro.
**Cons:** duplica a lógica central da biblioteca (validação + mapeamento); qualquer
mudança futura no comportamento de validação (ex.: como erros são reportados) precisa
ser replicada manualmente nos dois lugares.

### Opção C: serializar o `IDataReader` para CSV em memória e reusar `Validar(TextReader)` sem tocar a engine

Escrever o retorno da consulta como texto CSV num `StringWriter`/`MemoryStream` e
chamar o `Validar(TextReader)` já existente, sem nenhuma mudança na engine.

| Dimensão | Avaliação |
|---|---|
| Complexidade | Baixa — zero mudança na engine, só um adaptador que serializa |
| Risco de regressão no caminho CSV | Nenhum |
| Reuso | Total (reusa 100% do caminho existente) |
| Extensibilidade futura | Baixa e enganosa — parece resolver o problema mas não resolve a motivação original |

**Pros:** implementação trivial, zero risco ao caminho CSV.
**Cons:** **não resolve o problema que motivou o pedido** — continua round-tripando o
dado por uma serialização texto que ele nunca precisou ter, e reintroduz exatamente o
risco de falso positivo de formatação (`CultureInfo`, formatação de data/decimal) que
essa alteração deveria evitar, já que agora o dado passa por `ToString` E por um
parser CSV de volta. Descartada.

## Trade-off Analysis

A escolha real está entre A e B — C está descartada por não atacar a motivação.

- **B é o caminho mais seguro para entregar primeiro**, principalmente porque a engine
  atual não tem um `IDataReader` real disponível para testar contra (sem banco no
  ambiente de teste, ver Contexto) — qualquer refatoração da Opção A precisaria ser
  validada só com fakes/mocks de `IDataReader`, o que reduz a confiança de que a
  extração não introduziu regressão sutil no caminho CSV.
- **A é o caminho estruturalmente mais correto** se a expectativa é que fontes de dado
  além de arquivo se tornem comuns (ex.: se depois de consulta vier "validar retorno de
  API" também) — mas paga esse preço adiantado sem uma segunda fonte concreta ainda
  confirmada além da de banco.
- Dado que hoje só existe **um** caso concreto de segunda fonte (consulta), e que a
  regra dos três favorece não abstrair no primeiro caso, a recomendação (Opção A com
  extração mínima, não uma reescrita ampla) é um meio-termo: extrai só a parte do loop
  que é claramente idêntica hoje (validação FluentValidation + `Mapper.Map`), sem tentar
  antecipar uma abstração de "fonte de linha" genérica que ainda não tem um terceiro
  consumidor para validar o formato certo.

## Consequences

- **Fica mais fácil:** validar dado que já está em memória (retorno de query) sem
  round-trip por arquivo; adicionar uma eventual 3ª fonte de dado no futuro reusa o
  helper de validação+mapeamento extraído.
- **Fica mais difícil:** revisar mudanças na engine, porque agora um método interno é
  compartilhado por dois caminhos com formatos de erro de "estrutura" diferentes (CSV
  tem `EstruturaDeColunas`/`LayoutIncompativelException`; consulta não tem equivalente
  direto — uma consulta bem formada sempre retorna o número de colunas do `SELECT`).
- **Precisa ser revisitado:** a decisão de escopo v1 do README ("Só arquivos
  delimitados") passa a estar desatualizada e precisaria de uma frase nova cobrindo a
  fonte de consulta, quando essa alteração for de fato implementada.
- **Testes:** sem banco disponível no ambiente de teste (nem será instalado só para
  isso — ver card do Trello), qualquer suíte de teste da Opção A/B precisa validar
  contra um `IDataReader` fake construído na mão (ex.: `DataTable.CreateDataReader()`
  com dados fixos, ou uma implementação mínima de `IDataReader` para os testes), nunca
  contra uma conexão real com MySQL/Postgres/SQL Server.

## Action Items

Nenhum destes itens está sendo executado nesta etapa — ficam registrados para quando a
decisão acima for confirmada e a implementação for de fato encarada:

1. [ ] Confirmar a decisão deste ADR (Opção A vs. B) com o decider antes de codar.
2. [ ] Extrair (Opção A) ou duplicar deliberadamente (Opção B) o loop de
   validação+mapeamento hoje dentro de `LayoutValidationEngine.Validar`.
3. [ ] Decidir a assinatura do novo ponto de entrada (ex.:
   `IValidadorLayout<T>.ValidarConsulta(IDataReader)` vs. método estático separado fora
   da interface pública) e se ele entra na interface `IValidadorLayout<T>` ou fica à
   parte.
4. [ ] Implementar a conversão de coluna tipada → `string` na borda, com
   `CultureInfo.InvariantCulture`, documentando caso a caso onde isso pode gerar falso
   positivo de formato (datas e decimais são os candidatos óbvios).
5. [ ] Escrever testes usando `IDataReader` fake (`DataTable.CreateDataReader()` ou
   equivalente) cobrindo: linha válida, linha com erro de regra de validação, e o
   caso de campo nulo/`DBNull` vindo do banco (não tem equivalente direto no CSV).
6. [ ] Atualizar o README (seção "Decisões de escopo (v1)") e a wiki
   (`Usando a Ferramenta.md`) para documentar o novo ponto de entrada, uma vez
   implementado — a wiki hoje só tem a entrada em "Possibilidades", que deixa de valer
   depois que isso for implementado.
7. [ ] Confirmar que nenhum app (`TesteApp`, `GeradorDados`) precisa da opção nova —
   por ora, nenhum consumidor concreto de consulta existe nos apps do repo.
