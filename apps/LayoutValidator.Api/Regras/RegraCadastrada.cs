using System.Text.Json;

namespace LayoutValidator.Api.Regras;

/// <summary>
/// Uma regra do catálogo cadastrável por chave — equivalente dinâmico dos métodos de
/// extensão de LayoutValidator.Regras (Cpf(), InteiroEntre(), etc.), só que descrita como
/// dado em vez de como método fortemente tipado.
/// </summary>
public sealed class RegraCadastrada
{
    public required string Chave { get; init; }
    public required IReadOnlyList<ParametroEsperado> ParametrosEsperados { get; init; }
    public required Func<string, JsonElement?, bool> Avaliar { get; init; }
    public required Func<JsonElement?, string> ObterCodigoErro { get; init; }
    public required Func<string, JsonElement?, string> MontarMensagem { get; init; }
}
