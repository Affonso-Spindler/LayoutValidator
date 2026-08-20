using System.Text.Json;
using System.Text.RegularExpressions;
using LayoutValidator.Api.Contratos;
using LayoutValidator.Api.Regras;

namespace LayoutValidator.Api.Validacao;

/// <summary>
/// Valida a definição de um layout no momento do cadastro — Código no formato esperado,
/// chave de regra existe no catálogo, e todo parâmetro obrigatório da regra está presente
/// com o tipo esperado. Existe para que um layout mal cadastrado nunca chegue a ser salvo:
/// sem isso, o erro só apareceria depois, quando alguém chamasse /validar com dado real.
/// </summary>
public static class ValidadorDeDefinicaoDeLayout
{
    // Código vira segmento de URL (/layouts/{codigo}) — só letras e números, até 20
    // caracteres, pra não virar um segundo nome descritivo (ver ADR-0002).
    private static readonly Regex PadraoCodigo = new(@"^[A-Za-z0-9]{1,20}$", RegexOptions.Compiled);

    public static IReadOnlyList<string> Validar(LayoutRequest requisicao, ICatalogoDeRegras catalogo)
    {
        var erros = new List<string>();

        if (!PadraoCodigo.IsMatch(requisicao.Codigo))
            erros.Add($"Código '{requisicao.Codigo}' inválido: use só letras e números, até 20 caracteres.");

        foreach (var campo in requisicao.Campos)
        {
            foreach (var regraCampo in campo.Regras)
                ValidarRegraDoCampo(campo, regraCampo, catalogo, erros);
        }

        return erros;
    }

    private static void ValidarRegraDoCampo(CampoRequest campo, RegraCampoRequest regraCampo, ICatalogoDeRegras catalogo, List<string> erros)
    {
        if (!catalogo.Existe(regraCampo.ChaveRegra))
        {
            erros.Add($"Campo '{campo.Nome}': regra '{regraCampo.ChaveRegra}' não existe no catálogo.");
            return;
        }

        var regra = catalogo.Obter(regraCampo.ChaveRegra);

        foreach (var parametroEsperado in regra.ParametrosEsperados.Where(p => p.Obrigatorio))
        {
            if (!TemParametroDoTipoEsperado(regraCampo.ParametrosJson, parametroEsperado))
            {
                erros.Add($"Campo '{campo.Nome}': regra '{regraCampo.ChaveRegra}' exige o parâmetro " +
                          $"'{parametroEsperado.Nome}' ({parametroEsperado.Tipo}).");
            }
        }
    }

    private static bool TemParametroDoTipoEsperado(JsonElement? parametros, ParametroEsperado parametroEsperado)
    {
        if (parametros is null || parametros.Value.ValueKind != JsonValueKind.Object)
            return false;

        if (!parametros.Value.TryGetProperty(parametroEsperado.Nome, out var valor))
            return false;

        return parametroEsperado.Tipo switch
        {
            TipoParametro.Inteiro => valor.ValueKind == JsonValueKind.Number && valor.TryGetInt64(out _),
            TipoParametro.Decimal => valor.ValueKind == JsonValueKind.Number,
            TipoParametro.Texto => valor.ValueKind == JsonValueKind.String,
            TipoParametro.ListaDeTexto => valor.ValueKind == JsonValueKind.Array,
            _ => false
        };
    }
}
