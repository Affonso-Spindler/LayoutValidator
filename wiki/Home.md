# LayoutValidator — Wiki

Biblioteca C# (.NET 8) para validar arquivos delimitados (CSV, pipe, etc.) contra um
layout declarado, antes de qualquer carga em banco. Detecta campo fora do formato
esperado (data, inteiro, regex, obrigatoriedade, etc.) e devolve um relatório de quais
linhas/campos estão fora da especificação — sem precisar chegar no `COPY` do banco pra
descobrir isso.

Para o resumo de decisões de arquitetura (por que dois Models por layout, streaming,
tratamento de linha quebrada, DI), ver o [README.md](../README.md) na raiz do repo.
Esta wiki é focada em **como usar e como estender** no dia a dia.

## Páginas

- [Usando a Ferramenta](Usando-a-Ferramenta.md) — como consumir a lib num projeto, como
  ler o resultado, como rodar o app de teste e o gerador de dados.
- [Criando Layouts](Criando-Layouts.md) — passo a passo pra declarar um layout novo,
  do zero, com exemplo completo.
- [Regras Reutilizáveis](Regras-Reutilizaveis.md) — o catálogo de regras prontas (CPF, data,
  UF, moeda...) e como escrever as suas próprias.
- [Cadastro de Layouts via API](Cadastro-de-Layouts-via-API.md) — caminho alternativo ao
  layout-como-código: cadastra layout num banco e valida uma linha por HTTP, sem escrever
  classe C# nenhuma.
- [Possibilidades](Possibilidades.md) — o que a lib não faz hoje e caminhos possíveis
  pra evoluir.

O delimitador padrão é **`;`** (arquivo brasileiro usa vírgula como separador decimal, então
vírgula delimitadora colidiria com todo campo de valor). Delimitador e tratamento da primeira
linha são declarados no próprio layout — ver
[Declarando o formato do arquivo](Criando-Layouts.md#declarando-o-formato-do-arquivo).

## Mapa do repositório

```
src/LayoutValidator/                     a biblioteca em si (o que você referencia no seu projeto)
src/LayoutValidator.Regras/              catálogo opcional de regras prontas (CPF, data, UF...)
samples/LayoutValidator.Sample/          exemplo mínimo — layout "Pessoa" (4 campos)
tests/LayoutValidator.Tests/             testes xUnit do motor de validação
tests/LayoutValidator.Regras.Tests/      testes xUnit do catálogo de regras
apps/LayoutValidator.LayoutFuncionario/  layout de referência maior — "Funcionário" (22 campos)
apps/LayoutValidator.GeradorDados/       gera arquivo CSV de teste com erros diversos
apps/LayoutValidator.TesteApp/           app WinForms pra validar qualquer .csv/.txt na mão
apps/LayoutValidator.Api/                API local: cadastra layout em banco, valida por HTTP
tests/LayoutValidator.Api.Tests/         testes xUnit do app de cadastro
dados-teste/                             saída do gerador (não versionado; recriável a qualquer momento)
wiki/                                    você está aqui
```

O layout "Pessoa" (em `samples/`) é o exemplo mais simples pra entender o padrão.
O layout "Funcionário" (em `apps/LayoutValidator.LayoutFuncionario`) é mais completo —
22 campos, usado pelo gerador de dados e pelo app de teste — bom ponto de partida pra
copiar quando for criar um layout novo com bastante variedade de regras.
