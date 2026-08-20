using LayoutValidator.Api.Modelos;
using System.Text.Json;

namespace LayoutValidator.Api.Contratos;

public static class MapeadorDeLayout
{
    public static LayoutCadastrado ParaEntidade(LayoutRequest requisicao) => new()
    {
        Codigo = requisicao.Codigo,
        Nome = requisicao.Nome,
        Delimitador = requisicao.Delimitador,
        Campos = requisicao.Campos.Select((campo, indiceCampo) => new CampoCadastrado
        {
            Nome = campo.Nome,
            Ordem = indiceCampo,
            Regras = campo.Regras.Select((regra, indiceRegra) => new RegraCampoCadastrada
            {
                ChaveRegra = regra.ChaveRegra,
                ParametrosJson = regra.ParametrosJson?.GetRawText(),
                Ordem = indiceRegra
            }).ToList()
        }).ToList()
    };

    public static LayoutResponse ParaResposta(LayoutCadastrado layout) => new(
        layout.Codigo,
        layout.Nome,
        layout.Delimitador,
        layout.Campos
            .OrderBy(campo => campo.Ordem)
            .Select(campo => new CampoResponse(
                campo.Nome,
                campo.Ordem,
                campo.Regras
                    .OrderBy(regra => regra.Ordem)
                    .Select(regra => new RegraCampoResponse(
                        regra.ChaveRegra,
                        regra.ParametrosJson is null ? null : JsonDocument.Parse(regra.ParametrosJson).RootElement))
                    .ToList()))
            .ToList());
}
