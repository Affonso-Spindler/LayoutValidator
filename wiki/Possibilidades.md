# Possibilidades

O que a v1 não faz, e caminhos possíveis pra quando isso virar necessidade real (não
implementar nada disso preventivamente — só um mapa de "se precisar, é por aqui").

## Encoding

Hoje fora de escopo (arquivos assumidos UTF-8). Se aparecer arquivo em outro encoding
(Latin-1/Windows-1252, por exemplo) ou caracteres corrompidos por encoding errado, a
validação precisaria acontecer **antes** do `StreamReader` decodificar o conteúdo —
depois de decodificado errado, o caractere original já virou `?`/lixo e não tem como
recuperar qual era o erro real. Caminho possível: uma etapa prévia que lê os bytes
crus, tenta detectar/validar o encoding (BOM, heurística tipo `Ude`/`CharsetDetector`),
e só então abre o `StreamReader` com o encoding certo — ou reporta a linha como
inválida por encoding antes mesmo de tentar parsear como CSV.

## Layout posicional / largura fixa

Hoje só delimitado. Arquivo posicional (cada campo com largura fixa em colunas, sem
delimitador — comum em integrações legadas/mainframe) precisaria de um parser próprio
no lugar do `CsvHelper.CsvReader` (que é delimitado por natureza), mas o resto da
arquitetura — `IValidadorLayout<T>`, `AbstractValidator<TRaw>`, `ResumoValidacaoLayout`,
`ErrorReportWriter` — é agnóstico a como o parsing acontece, então dava pra
reaproveitar quase tudo, só trocando a peça que lê o arquivo raw.

## Processamento assíncrono / paralelo

Avaliado e **descartado por ora** (set/2026). Medição com 1M linhas do layout
`Funcionario` (22 campos), em máquina de 16 núcleos:

| Etapa | Tempo |
|---|---|
| Leitura do disco | 0,24 s |
| Parse (CsvHelper) | ~1,8 s |
| Validação + mapeamento | ~2,8 s |
| Resumo + relatório | ~0,7 s |
| **Total** | **5,5 s** |

- **`async/await` não ajuda:** o trabalho é quase todo CPU, e o I/O de arquivo local é
  desprezível.
- **Paralelizar a validação** (parse numa thread, lotes validados em paralelo, saída
  reordenada por um `Channel`) dá o mesmo resultado, com relatório byte a byte idêntico,
  e cai para ~3,6 s (~1,5x). Não passa disso com mais núcleos: o parse é serial e custa
  ~2,1 s sozinho. PLINQ `AsOrdered` foi pior: ~6 s e até 1,3 GB de memória.
- Não compensa hoje: exigiria validador e mapper thread-safe, uma API nova e tratamento
  de exceção e cancelamento nos workers, para economizar ~2 s por milhão de linhas.

Quando reabrir: regras bem mais pesadas (que mudem a proporção parse × validação) ou
leitura de stream de rede (blob, SFTP), onde aí sim cabe um `ValidarAsync` com
`IAsyncEnumerable<T>`, já que o CsvHelper tem `ReadAsync`. Em uso real, o ganho mais
provável está no consumidor, por exemplo gravar os válidos no banco em lote.

## Mapeamento automático via reflection

O mapeamento `TRaw -> T` é manual por decisão consciente (mais simples, explícito, e
fácil de debugar). Se o número de layouts crescer muito e a maioria dos mapeamentos for
"campo a campo sem lógica nenhuma", vale considerar um mapper genérico por reflection
(ou algo como Mapster/AutoMapper) pros casos simples, mantendo o manual como opção pros
casos com lógica de conversão (como o `Funcionario`, que tem campo opcional viradando
`null`, decimal com vírgula, etc.).

## Empacotar como NuGet interno

Hoje é referência de projeto (`ProjectReference`). Se for usada em múltiplos
repositórios, vale empacotar `src/LayoutValidator` como pacote NuGet (interno, feed
privado) versionado — assim cada projeto consumidor fixa a versão que quer.

## Outros formatos de relatório

Hoje só CSV. Dependendo de quem consome o relatório, pode fazer sentido: JSON (pra
consumo programático por outro sistema), Excel/xlsx (pra time de negócio revisar sem
precisar abrir CSV com separador certo), ou publicar direto numa fila/tópico pra um
pipeline de qualidade de dados consumir.

## Regras cross-file / cross-record

Hoje cada linha é validada isoladamente. Regras que dependem de outras linhas do mesmo
arquivo (ex: "código não pode se repetir no arquivo") ou de estado externo (ex:
"código precisa já existir cadastrado no banco") não cabem no `AbstractValidator<TRaw>`
de uma linha só — precisariam de uma etapa adicional com estado acumulado (ex: um
`HashSet<string>` de códigos já vistos) ou uma consulta externa, o que também tira a
característica "streaming sem estado" que a engine tem hoje pra validação por linha.

## CLI standalone

O `LayoutValidator.TesteApp` é uma GUI pensada pra teste manual. Se surgir necessidade
de rodar validação de layout dentro de um pipeline CI/CD ou job agendado (sem tela),
vale um terceiro "consumidor" da lib: um console app que recebe caminho do arquivo e
qual layout usar por parâmetro, e retorna exit code não-zero se houver registro
inválido — encaixa no mesmo padrão dos outros apps em `apps/`.

## Validar arquivo/lote pela API

A [API de layouts cadastrados](Cadastro-de-Layouts-via-API.md) valida **uma linha por
chamada** (`POST /layouts/{codigo}/validar`) — validar um arquivo inteiro por ali está fora
do escopo da v1 (ver [ADR-0002](../docs/adr/0002-cadastro-de-layouts-via-api-local.md)). A
tela em `apps/LayoutValidator.Web` contorna isso no cliente, quebrando o texto colado em
linhas e chamando o endpoint uma vez por linha — resolve pra conferir um punhado de linhas
na mão, mas não pra arquivo de verdade.

Se virar necessidade, o caminho não é repetir a chamada mais rápido: é um endpoint que
receba o arquivo (multipart) e devolva o `ResumoValidacaoLayout` — reaproveitando o motor
de streaming do core em vez do avaliador linha a linha da API. Aí aparecem as perguntas
que a v1 não precisou responder: upload síncrono ou job assíncrono com status, e o que
devolver quando o arquivo tem milhões de erros (o resumo? o CSV de erros? um link?).

## Histórico de qualidade de dados

O `ResumoValidacaoLayout` hoje vive só durante uma execução. Se for útil acompanhar ao
longo do tempo "esse fornecedor de arquivo está piorando a qualidade dos dados?",
alguém precisaria persistir os resumos (por execução, por arquivo, por regra) em algum
lugar — um banco simples ou até um CSV/JSON append-only já resolveria pra começar.

## ~~Validar retorno de consulta (sem passar por arquivo)~~ — implementado

Deixou de ser possibilidade futura: `LayoutValidationEngine.Validar` já aceita dados que
não vêm de arquivo (ex.: retorno de consulta) direto, sem fachada de layout. Ver
[Usando a Ferramenta § 6](Usando-a-Ferramenta.md#6-validando-dados-que-já-estão-em-memória-sem-arquivo)
pro uso, e [ADR-0001](../docs/adr/0001-validar-retorno-de-consulta.md) pra decisão e
alternativas consideradas.
