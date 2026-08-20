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
