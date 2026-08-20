using System.Text.Json;
using LayoutValidator.Api.Modelos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Validacao;

/// <summary>
/// Avalia as regras de um campo na ordem cadastrada, com cascade-stop: para na primeira
/// regra que falhar naquele campo — mesmo comportamento de RuleLevelCascadeMode.Stop do
/// catálogo estático (LayoutValidator.Regras).
/// </summary>
public static class AvaliadorDeCampo
{
    public static ErroDeCampo? Avaliar(CampoCadastrado campo, string valor, ICatalogoDeRegras catalogo)
    {
        foreach (var regraCampo in campo.Regras.OrderBy(r => r.Ordem))
        {
            var regra = catalogo.Obter(regraCampo.ChaveRegra);
            var parametros = ParseParametros(regraCampo.ParametrosJson);

            if (!regra.Avaliar(valor, parametros))
                return new ErroDeCampo(campo.Nome, valor, regra.ObterCodigoErro(parametros), regra.MontarMensagem(campo.Nome, parametros));
        }

        return null;
    }

    private static JsonElement? ParseParametros(string? parametrosJson) =>
        parametrosJson is null ? null : JsonDocument.Parse(parametrosJson).RootElement;
}
