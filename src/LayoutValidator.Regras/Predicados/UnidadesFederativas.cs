namespace LayoutValidator.Regras.Predicados;

/// <summary>
/// As 27 unidades federativas do Brasil — 26 estados mais o Distrito Federal.
/// </summary>
public static class UnidadesFederativas
{
    private static readonly HashSet<string> Siglas = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
        "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public static IReadOnlyCollection<string> Todas => Siglas;

    /// <summary>
    /// Case-insensitive, mas sem trim: " SP" reprova de propósito — espaço sobrando numa
    /// célula é defeito do arquivo, não algo pra corrigir calado durante a validação.
    /// </summary>
    public static bool Valida(string? valor) => valor is not null && Siglas.Contains(valor);
}
