using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;
using LayoutValidator.Api.Validacao;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Endpoints;

public static class LayoutsEndpoints
{
    public static void MapLayoutsEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/layouts", CriarAsync);
        rotas.MapGet("/layouts", ListarAsync);
        rotas.MapGet("/layouts/{codigo}", ObterAsync);
        rotas.MapPut("/layouts/{codigo}", AtualizarAsync);
        rotas.MapDelete("/layouts/{codigo}", RemoverAsync);
    }

    private static async Task<IResult> CriarAsync(LayoutRequest requisicao, ApiDbContext db, ICatalogoDeRegras catalogo)
    {
        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, catalogo);
        if (erros.Count > 0)
            return Results.BadRequest(new { erros });

        if (await db.Layouts.AnyAsync(l => l.Codigo == requisicao.Codigo))
            return Results.Conflict(new { erro = $"Já existe um layout com o código '{requisicao.Codigo}'." });

        var layout = MapeadorDeLayout.ParaEntidade(requisicao);
        layout.CriadoEm = DateTime.UtcNow;
        layout.AtualizadoEm = layout.CriadoEm;

        db.Layouts.Add(layout);
        await db.SaveChangesAsync();

        return Results.Created($"/layouts/{layout.Codigo}", MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> ListarAsync(ApiDbContext db)
    {
        var layouts = await CarregarCompleto(db).ToListAsync();
        return Results.Ok(layouts.Select(MapeadorDeLayout.ParaResposta));
    }

    private static async Task<IResult> ObterAsync(string codigo, ApiDbContext db)
    {
        var layout = await CarregarCompleto(db).FirstOrDefaultAsync(l => l.Codigo == codigo);
        return layout is null ? Results.NotFound() : Results.Ok(MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> AtualizarAsync(string codigo, LayoutRequest requisicao, ApiDbContext db, ICatalogoDeRegras catalogo)
    {
        var erros = ValidadorDeDefinicaoDeLayout.Validar(requisicao, catalogo);
        if (erros.Count > 0)
            return Results.BadRequest(new { erros });

        var layout = await CarregarCompleto(db).FirstOrDefaultAsync(l => l.Codigo == codigo);
        if (layout is null)
            return Results.NotFound();

        if (requisicao.Codigo != codigo && await db.Layouts.AnyAsync(l => l.Codigo == requisicao.Codigo))
            return Results.Conflict(new { erro = $"Já existe um layout com o código '{requisicao.Codigo}'." });

        var atualizado = MapeadorDeLayout.ParaEntidade(requisicao);
        layout.Codigo = atualizado.Codigo;
        layout.Nome = atualizado.Nome;
        layout.Delimitador = atualizado.Delimitador;
        layout.Campos.Clear();
        layout.Campos.AddRange(atualizado.Campos);
        layout.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(MapeadorDeLayout.ParaResposta(layout));
    }

    private static async Task<IResult> RemoverAsync(string codigo, ApiDbContext db)
    {
        var layout = await db.Layouts.FirstOrDefaultAsync(l => l.Codigo == codigo);
        if (layout is null)
            return Results.NotFound();

        db.Layouts.Remove(layout);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static IQueryable<LayoutCadastrado> CarregarCompleto(ApiDbContext db) =>
        db.Layouts.Include(l => l.Campos).ThenInclude(c => c.Regras);
}
