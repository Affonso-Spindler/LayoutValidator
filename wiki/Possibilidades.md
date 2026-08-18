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

## Processamento assíncrono (`IAsyncEnumerable`)

A versão atual é síncrona porque leitura de arquivo local linha a linha já é rápida o
suficiente (1M linhas em ~6-7s no teste com o layout de 22 campos). Se um dia isso
rodar contra um stream de rede (blob storage, SFTP remoto) onde I/O é o gargalo, faz
sentido um `LayoutValidationEngine.ValidarAsync` retornando `IAsyncEnumerable<T>` — a
CsvHelper já suporta `GetRecordsAsync`.

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
