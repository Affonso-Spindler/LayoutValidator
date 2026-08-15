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
- [Possibilidades](Possibilidades.md) — o que a lib não faz hoje e caminhos possíveis
  pra evoluir.

## Mapa do repositório

```
src/LayoutValidator/                     a biblioteca em si (o que você referencia no seu projeto)
samples/LayoutValidator.Sample/          exemplo mínimo — layout "Pessoa" (4 campos)
tests/LayoutValidator.Tests/             testes xUnit do motor de validação
apps/LayoutValidator.LayoutFuncionario/  layout de referência maior — "Funcionário" (22 campos)
apps/LayoutValidator.GeradorDados/       gera arquivo CSV de teste com erros diversos
apps/LayoutValidator.TesteApp/           app WinForms pra validar qualquer .csv/.txt na mão
dados-teste/                             arquivo de 1M linhas gerado + resumo do que foi injetado
wiki/                                    você está aqui
```

O layout "Pessoa" (em `samples/`) é o exemplo mais simples pra entender o padrão.
O layout "Funcionário" (em `apps/LayoutValidator.LayoutFuncionario`) é mais completo —
22 campos, usado pelo gerador de dados e pelo app de teste — bom ponto de partida pra
copiar quando for criar um layout novo com bastante variedade de regras.
