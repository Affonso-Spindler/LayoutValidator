# LayoutValidator.Web

Tela Angular pra cadastrar, listar e testar layouts sem escrever classe C# nenhuma — é a
interface do caminho "layout cadastrado" descrito em
[Cadastro de Layouts via API](../../wiki/Cadastro-de-Layouts-via-API.md).

**Ela não valida nada sozinha.** Toda regra, validação e persistência vive em
`apps/LayoutValidator.Api`; aqui só tem formulário, listagem e apresentação de erro. Por
isso a API precisa estar no ar, em `http://localhost:5000`:

```bash
# terminal 1 — a API (cria/migra o SQLite sozinha no startup)
dotnet run --project ../LayoutValidator.Api/LayoutValidator.Api.csproj

# terminal 2 — a tela
npm install    # só na primeira vez
npm start      # http://localhost:4200
```

A URL da API fica em [`src/app/api-base-url.ts`](src/app/api-base-url.ts). Ela é liberada no
CORS da API só pra `http://localhost:4200` — se mudar a porta do dev server, tem que mudar lá
também (`Program.cs`).

## As três abas

| Aba | O que faz |
|---|---|
| **Cadastrar** | monta o layout: código, nome, delimitador e os campos, cada um com N regras |
| **Listar** | tabela dos layouts cadastrados, com editar e remover |
| **Testar** | escolhe um layout, cola uma ou mais linhas e vê o resultado linha a linha |

O formulário de cadastro **não tem lista de regras hardcoded**: ele monta o dropdown e os
campos de parâmetro a partir do `GET /regras`. Regra nova no catálogo da API aparece aqui
sozinha, sem mexer neste projeto — inclusive os parâmetros dela (`minimo`, `formato`,
`casasDecimais`...), que são renderizados conforme o tipo que a API declara.

Na aba Testar, o `POST /layouts/{codigo}/validar` da API valida **uma linha por chamada** —
colar várias linhas dispara uma chamada por linha. Serve pra conferir algumas linhas na mão,
não pra arquivo grande (ver
[Possibilidades](../../wiki/Possibilidades.md#validar-arquivolote-pela-api)).

## Comandos

```bash
npm start           # dev server em http://localhost:4200
npm run build       # build de produção em dist/
npm test            # testes unitários (Vitest)
```

Gerado com Angular CLI 22, standalone components + Angular Material.
