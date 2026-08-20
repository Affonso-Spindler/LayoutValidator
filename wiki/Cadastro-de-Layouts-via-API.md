# Cadastro de Layouts via API

Guia de uso do `apps/LayoutValidator.Api` — uma API HTTP local que permite cadastrar um
layout (campos + regras) num banco SQLite e validar uma linha de texto contra ele, **sem
precisar escrever nenhuma classe C#**. É um caminho paralelo ao uso "layout como código"
descrito em [Usando a Ferramenta](Usando-a-Ferramenta.md) e [Criando Layouts](Criando-Layouts.md)
— não substitui, resolve um problema diferente. Pra decisões de arquitetura e trade-offs, ver
[ADR-0002](../docs/adr/0002-cadastro-de-layouts-via-api-local.md).

## Quando usar cada caminho

| | Layout como código | Layout cadastrado (esta página) |
|---|---|---|
| Onde mora | Classes C# compiladas no seu projeto | Registro em banco SQLite local |
| Quem consome | Só quem referencia a biblioteca | Qualquer coisa que fale HTTP |
| Editar um campo/regra | Muda código, recompila, reimplanta | Chama `PUT /layouts/{codigo}` |
| Entrada | Arquivo inteiro (streaming) ou dados em memória | Uma linha de texto por chamada |
| Saída | Model final tipado (`Pessoa`, `Funcionario`...) | Só `aderente`/`erros` — sem tipo final |

Se seu consumidor já é um projeto C# validando arquivo grande, o caminho de código continua
sendo a opção certa — streaming e Model final tipado só existem ali. A API entra quando quem
cadastra/edita o layout não deve depender de deploy, ou quando quem consome não fala C#.

## 1. Rodando a API

```bash
dotnet run --project apps/LayoutValidator.Api/LayoutValidator.Api.csproj
```

Cria (ou migra) `apps/LayoutValidator.Api/layoutvalidator.db` automaticamente no startup — nada
pra configurar antes da primeira execução. Por padrão sobe em `http://localhost:5000`.

Com a API no ar, `http://localhost:5000/swagger` tem a documentação interativa (Swagger UI) —
lista todos os endpoints, os schemas de request/response, e dá pra testar chamada por chamada
direto do navegador, sem precisar de `curl`.

## 2. Cadastrando um layout

```bash
curl -X POST http://localhost:5000/layouts \
  -H "Content-Type: application/json" \
  -d '{
    "codigo": "PESSOA1",
    "nome": "Pessoa",
    "delimitador": ";",
    "campos": [
      { "nome": "Cpf", "regras": [ { "chaveRegra": "Obrigatorio" }, { "chaveRegra": "Cpf" } ] },
      { "nome": "Nome", "regras": [ { "chaveRegra": "Obrigatorio" }, { "chaveRegra": "ComprimentoEntre", "parametrosJson": { "minimo": 2, "maximo": 100 } } ] },
      { "nome": "Idade", "regras": [ { "chaveRegra": "InteiroEntre", "parametrosJson": { "minimo": 0, "maximo": 120 } } ] },
      { "nome": "Uf", "regras": [ { "chaveRegra": "Uf" } ] }
    ]
  }'
```

Pontos que valem atenção:

- **`Codigo`** é o identificador usado na URL (`/layouts/{codigo}`) e em toda referência ao
  layout daqui pra frente — único, só letras e números, até 20 caracteres. `Nome` é só um
  rótulo descritivo e **não** precisa ser único (dá pra ter "Pessoa 2024" e "Pessoa 2024 v2"
  coexistindo, diferenciados pelo `Codigo`).
- **A ordem dos campos no array é o contrato posicional** — não existe cabeçalho aqui, um
  layout cadastrado é sempre posicional. O primeiro campo do array casa com o primeiro valor
  da linha, e assim por diante. Não existe um campo `ordem` pra declarar explicitamente — é
  sempre a posição no array (evita cadastrar ordem duplicada ou fora de sequência por engano).
- **A ordem das regras dentro de um campo importa**: elas são avaliadas nessa ordem, com
  cascade-stop — para na primeira regra que falhar *naquele campo*, mas os outros campos
  continuam sendo avaliados normalmente.
- **Campo opcional é só não declarar `Obrigatorio`** — mesma convenção do catálogo de código:
  toda regra de formato (tudo exceto `Obrigatorio`) deixa passar valor vazio.

Se `Codigo` for inválido, alguma `chaveRegra` não existir no catálogo, ou faltar/errar o tipo
de algum parâmetro obrigatório de uma regra, o cadastro é **rejeitado com `400`** antes de
qualquer coisa ir pro banco — ver [§3](#3-consultando-o-catálogo-de-regras) pra saber quais
parâmetros cada regra espera.

## 3. Consultando o catálogo de regras

```bash
curl http://localhost:5000/regras
```

Devolve as 19 regras disponíveis nesta v1, cada uma com os parâmetros que ela espera:

```json
[
  { "chave": "Cpf", "parametrosEsperados": [] },
  {
    "chave": "ComprimentoEntre",
    "parametrosEsperados": [
      { "nome": "minimo", "tipo": "Inteiro", "obrigatorio": true },
      { "nome": "maximo", "tipo": "Inteiro", "obrigatorio": true }
    ]
  }
]
```

Esse é o mesmo endpoint que uma futura tela de cadastro usaria pra montar os campos de
parâmetro dinamicamente — nenhum parâmetro esperado é documentado só em código/README, tudo
vem daqui. As 19 chaves cobrem o mesmo catálogo do código (ver
[Regras Reutilizáveis](Regras-Reutilizaveis.md) pro equivalente em C#):

`Obrigatorio`, `ComprimentoEntre`, `ComprimentoMaximo`, `ComprimentoExato`, `SomenteDigitos`,
`ValorEm`, `Formato`, `Inteiro`, `InteiroEntre`, `Decimal`, `DecimalEntre`, `Cpf`, `Cnpj`,
`CpfOuCnpj`, `Cep`, `Uf`, `Telefone`, `Cnh`, `PisPasep`.

`ValorEm` espera `{ "valores": ["S", "N"] }` (lista de texto); `Formato` espera
`{ "expressaoRegular": "...", "codigoErro": "...", "mensagem": "..." }` — é o escape hatch pra
regra pontual que não vale virar entrada fixa do catálogo, igual o `.Formato()` do código.

## 4. Validando uma linha

```bash
curl -X POST http://localhost:5000/layouts/PESSOA1/validar \
  -H "Content-Type: application/json" \
  -d '{"linha":"11144477735;Joao;30;SP"}'
```
```json
{"aderente":true,"erros":[]}
```

Uma linha com problema em vários campos ao mesmo tempo — cada campo é avaliado
independentemente, então dá pra ver todos os erros de uma vez, não só o primeiro:

```bash
curl -X POST http://localhost:5000/layouts/PESSOA1/validar \
  -H "Content-Type: application/json" \
  -d '{"linha":"123;X;200;ZZ"}'
```
```json
{
  "aderente": false,
  "erros": [
    { "campo": "Cpf", "valorRaw": "123", "regra": "CpfInvalido", "mensagem": "'Cpf' não é um CPF válido (11 dígitos, sem máscara, com dígito verificador correto)." },
    { "campo": "Nome", "valorRaw": "X", "regra": "ComprimentoInvalido", "mensagem": "'Nome' deve ter entre 2 e 100 caracteres." },
    { "campo": "Idade", "valorRaw": "200", "regra": "InteiroForaDoIntervalo", "mensagem": "'Idade' deve ser um inteiro entre 0 e 120." },
    { "campo": "Uf", "valorRaw": "ZZ", "regra": "UfInvalida", "mensagem": "'Uf' deve ser a sigla de uma unidade federativa brasileira." }
  ]
}
```

Se o número de campos da linha não bater com o número de campos cadastrados, vem um erro
sintético só, `NomeRegra = "EstruturaDeColunas"` — mesma convenção que o motor de streaming
já usa hoje pro caminho de arquivo. Se o `Codigo` não existir, a resposta é `404`.

Nesta v1 só dá pra validar **uma linha por chamada** — validar lista/arquivo inteiro via API
ainda não existe (ver [Possibilidades](Possibilidades.md) e o ADR pra evolução futura).

## 5. Editando e removendo

```bash
# Sobrescreve a definição inteira — sem versionamento nesta v1, edição anterior se perde.
curl -X PUT http://localhost:5000/layouts/PESSOA1 -H "Content-Type: application/json" -d '{...}'

# Some do banco, junto com todos os campos e regras dele.
curl -X DELETE http://localhost:5000/layouts/PESSOA1
```

`GET /layouts` lista tudo que está cadastrado; `GET /layouts/{codigo}` traz um só, com campos
e regras completos.

## 6. Sem autenticação, de propósito

A API não tem login nem token — decisão de escopo, não descuido: ela roda na máquina de quem
consome, não é pensada pra ficar exposta pra fora. Ver "Fora de escopo v1" no
[ADR-0002](../docs/adr/0002-cadastro-de-layouts-via-api-local.md#consequências) pro resto do
que ainda não existe (versionamento de layout, validação de lote/arquivo, autenticação).
