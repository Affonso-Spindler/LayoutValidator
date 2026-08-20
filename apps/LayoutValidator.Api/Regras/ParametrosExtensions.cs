using System.Text.Json;

namespace LayoutValidator.Api.Regras;

internal static class ParametrosExtensions
{
    public static long ObterInteiro(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetInt64();

    public static decimal ObterDecimal(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetDecimal();

    public static string ObterTexto(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome).GetString() ?? string.Empty;

    public static string[] ObterListaDeTexto(this JsonElement? parametros, string nome) =>
        parametros!.Value.GetProperty(nome)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

    public static bool TemPropriedade(this JsonElement? parametros, string nome) =>
        parametros.HasValue
        && parametros.Value.ValueKind == JsonValueKind.Object
        && parametros.Value.TryGetProperty(nome, out _);
}
