using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Endpoints;

public static class RegrasEndpoints
{
    public static void MapRegrasEndpoints(this IEndpointRouteBuilder rotas) =>
        rotas.MapGet("/regras", (ICatalogoDeRegras catalogo) => Results.Ok(
            catalogo.Todas
                .OrderBy(regra => regra.Chave)
                .Select(regra => new RegraDisponivelResponse(
                    regra.Chave,
                    regra.ParametrosEsperados
                        .Select(p => new ParametroEsperadoResponse(p.Nome, p.Tipo.ToString(), p.Obrigatorio))
                        .ToList()))));
}
