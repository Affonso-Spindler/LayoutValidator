# ADR-0002: Cadastro de layouts em banco, validados via API local

**Status:** Proposed
**Date:** 2026-08-19
**Deciders:** Affonso Spindler

## Contexto

Hoje um layout só existe como código C#: `XxxRaw` + `XxxValidador : AbstractValidator<XxxRaw>`
+ `XxxMapper` + `XxxValidadorLayout`. Pra consumir de outro serviço, a única forma é a
biblioteca estar referenciada no lado do servidor — o layout precisa estar compilado ali.
Isso trava qualquer cenário onde quem consome não é (ou não quer ser) um projeto C# com
essa referência: o layout tem que ser cadastrado/editado sem deploy, e validado por quem
só sabe falar HTTP.

Card de acompanhamento: (nenhum ainda — abrir se for o caso).

## Decisão

Novo app `apps/LayoutValidator.Api` (ASP.NET Core Minimal API, net8.0), adicionado à
`LayoutValidator.sln`. Referencia `LayoutValidator` (core — só para reusar o parser de
linha delimitada baseado em `OpcoesLayout`) e `LayoutValidator.Regras` (para os predicados
puros em `Predicados/*`, que já não dependem de FluentValidation). Nenhuma das duas muda.

API local, sem autenticação — decisão explícita de escopo, não descuido: roda na máquina
de quem consome, não é exposta pra fora.

### Por que não reusa o pipeline Raw → Validador → Mapper

Layout cadastrado não produz mais um Model final tipado (`Pessoa`, `Funcionario`) — não
existe mais uma classe C# de destino pra mapear. O resultado de validar uma linha contra um
layout cadastrado é só **aderente/não aderente + lista de erros por campo**, então o
`Mapper` e o Model final tipado somem desse caminho. `IValidadorLayout<T>`,
`ValidadorLayoutBase<TRaw,T>` e `LayoutValidationEngine` continuam existindo do jeito que
estão, pro caminho de layout-como-código — o caminho novo é paralelo, não substitui.

### Modelo de dados (EF Core + SQLite)

```
LayoutCadastrado     { Id, Codigo (único, alfanumérico, curto), Nome, Delimitador, CriadoEm, AtualizadoEm }
CampoCadastrado      { Id, LayoutId, Nome, Ordem }
RegraCampoCadastrada { Id, CampoId, ChaveRegra, ParametrosJson, Ordem }
```

`Codigo` é o identificador usado pra referenciar o layout (na URL, em conversa, em log) —
curto, só letras e números (ex.: `FUNC22`, `PESSOA1`), com um limite de tamanho (proposta:
20 caracteres) pra não virar um segundo nome descritivo. `Nome` continua existindo como
rótulo descritivo, mas **deixa de ser único** — pode haver vários layouts parecidos
("Funcionário 2024", "Funcionário 2024 v2") com o mesmo tipo de nome, diferenciados pelo
`Codigo`.

Sem `ModoCabecalho`: layout cadastrado é sempre posicional, porque a entrada de v1 é
sempre uma linha solta (nunca um arquivo com cabeçalho) — mesma convenção que
`ModoCabecalho.Ausente`/os overloads em memória já usam hoje.

`Obrigatorio` continua sendo só mais uma entrada de `RegraCampoCadastrada`
(`ChaveRegra="Obrigatorio"`), não uma coluna booleana à parte — preserva o contrato atual
("campo opcional = não declarar Obrigatorio", regra de formato nunca reprova vazio).

SQLite escolhido por não exigir infraestrutura pra rodar localmente, ter migrations via
EF Core, e permitir trocar de provider depois (Postgres/SQL Server) sem tocar na lógica de
validação — só a camada de acesso a dado.

### Catálogo de regras

```csharp
public interface IRegraCadastrada
{
    string Chave { get; }                                    // "Cpf", "InteiroEntre", ...
    string CodigoErro { get; }
    ParametroEsperado[] ParametrosEsperados { get; }          // nome, tipo, obrigatório — descreve o shape
    bool Avaliar(string valor, JsonElement? parametros);
    string MontarMensagem(string nomeCampo, JsonElement? parametros);
}
```

Cada regra de hoje (`Cpf`, `Cnpj`, `CpfOuCnpj`, `Cep`, `Uf`, `Telefone`, `Cnh`, `PisPasep`,
`Inteiro`, `InteiroEntre`, `Decimal`, `DecimalEntre`, `ComprimentoEntre`, `ComprimentoMaximo`,
`ComprimentoExato`, `SomenteDigitos`, `ValorEm`, `Obrigatorio`, `Formato`) ganha uma
implementação fininha que só chama o predicado já existente em `Predicados.Formatos` /
`Predicados.Documentos` / `Predicados.UnidadesFederativas` — **zero reescrita de lógica de
validação**. Registradas por chave num `ICatalogoDeRegras` (DI singleton).

O contrato "regra de formato não reprova valor vazio" (hoje em `ConstrutorRegra.DeFormato`)
é aplicado genericamente pelo avaliador do catálogo pra toda regra exceto `Obrigatorio`, em
vez de cada implementação reimplementar o check de vazio.

### Validação da definição no cadastro

`POST`/`PUT /layouts` valida `ParametrosJson` de cada `RegraCampoCadastrada` contra
`ParametrosEsperados` da regra correspondente **antes de salvar** — parâmetro faltando ou
de tipo errado rejeita o cadastro com mensagem apontando campo/regra, em vez de só falhar
quando alguém chamar `/validar` com dado real. Mesma descrição (`ParametrosEsperados`)
alimenta `GET /regras`, então não há duplicação entre "o que valida no cadastro" e "o que
aparece pra quem for montar uma tela de cadastro".

### Endpoint de validação (v1: uma linha por chamada)

```
POST /layouts/{codigo}/validar
{ "linha": "12345678901;João;30" }
```

1. Carrega `LayoutCadastrado` (+ campos + regras) pelo `Codigo`; 404 se não existe.
2. Divide a linha pelo `Delimitador` do layout, reusando o parser do CsvHelper via
   `OpcoesLayout` — mesmo tratamento de aspas/escape do resto da biblioteca.
3. Contagem de campos diferente do número de `CampoCadastrado` → erro `EstruturaDeColunas`
   (mesmo nome de regra que o `LayoutValidationEngine` já usa hoje pro mesmo caso).
4. Por campo, na ordem cadastrada, avalia as regras também na ordem, com cascade-stop
   (para na primeira regra que falhar *naquele* campo, mas continua avaliando os demais
   campos) — reproduz `RuleLevelCascadeMode.Stop` de hoje.
5. Resposta: `{ "aderente": bool, "erros": [{ "campo", "valorRaw", "regra", "mensagem" }] }`
   — mesma informação que `ErroValidacaoLayout` já expõe.

### CRUD

```
POST/GET/PUT/DELETE /layouts, /layouts/{codigo}
GET /regras   -- catálogo disponível: chave, parâmetros esperados, descrição
```

Edição é livre e sobrescreve a definição atual — sem versionamento em v1 (ver
Consequências).

## Como chegamos aqui

**Regras algorítmicas (CPF/CNPJ/CNH/PIS) como dado 100% cadastrável.** Cogitado e
descartado para v1: exigiria um motor de regra composta (passos configuráveis de peso,
módulo, comparação) pra expressar dígito verificador em dado puro — um subsistema próprio
(parser + executor) maior e mais arriscado que o resto do design inteiro junto, sem
cobertura dos testes de unidade que já existem pros predicados. Documentos, no entanto,
continuam **cadastrados por chave** igual as regras 100% dado — quem cadastra escolhe
"Cpf" de uma lista do mesmo jeito que escolhe "InteiroEntre", sem saber (nem precisar
saber) que uma é lógica em C# e outra é parâmetro puro. Só documento genuinamente novo
exige código novo — mesmo custo que hoje.

**Devolver o dado convertido/tipado na resposta de validação.** Descartado: sem Model
final por trás de um layout cadastrado, "tipo final" viraria um JSON genérico sem
garantia nenhuma de forma — reintroduziria por trás o mesmo problema que o
`Mapper`/`XxxRaw` resolve hoje (conversão só é segura depois que a validação passou), só
que sem o tipo concreto que torna isso verificável em tempo de compilação. v1 responde só
aderente/não-aderente + erros; convertido fica pra quando (se) houver um caso de uso real
puxando isso.

**Repositório separado pra API.** Descartado: LayoutValidator e LayoutValidator.Regras não
são publicados como pacote NuGet hoje, e não há necessidade de sê-lo só pra isso — API
nova entra como mais um app na mesma solution, igual `GeradorDados`/`TesteApp` já fazem,
referenciando os projetos de biblioteca diretamente.

## Consequências

- **Fica mais fácil**: cadastrar/editar layout sem deploy, validar de qualquer lugar que
  fale HTTP, sem precisar referenciar a biblioteca C#.
- **Duas formas de declarar um layout convivem**: código (`XxxValidadorLayout`, com Model
  final tipado, streaming de arquivo) e cadastro (banco, uma linha por vez, sem tipo
  final). Não são a mesma coisa nem se substituem — o caminho de código continua sendo a
  opção certa pra quem já é um projeto C# validando arquivo grande.
- **Fora de escopo v1** (documentado, não esquecido):
  - Validação de lista/arquivo via API — só uma linha por chamada agora; entra depois que
    o cenário de linha única estiver validado em uso real.
  - Versionamento de layout — edição sobrescreve; se algum consumidor precisar validar
    contra uma versão específica no futuro, isso vira uma migration de schema, não uma
    mudança de design.
  - Autenticação — API roda local; entra se/quando a API deixar de ser só local.
  - Motor de regra 100% dinâmico para documentos com dígito verificador.
- **Testes**: unidade no `ICatalogoDeRegras` reusando os mesmos casos de
  `LayoutValidator.Regras.Tests` (confirma o wrapper de parâmetros/mensagem, não recria os
  predicados); integração via `WebApplicationFactory` + SQLite em arquivo temporário
  (cadastro válido, cadastro rejeitado por parâmetro inválido, validação aderente,
  validação com erro por campo, estrutura de colunas errada, layout inexistente).
