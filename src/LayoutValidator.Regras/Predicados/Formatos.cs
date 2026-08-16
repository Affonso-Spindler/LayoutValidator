using System.Globalization;

namespace LayoutValidator.Regras.Predicados;

/// <summary>
/// Predicados de formato, sem dependência de FluentValidation.
/// Todos são totais: nunca lançam exceção, qualquer que seja a entrada.
/// </summary>
public static class Formatos
{
    /// <summary>Cultura brasileira: vírgula como separador decimal.</summary>
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Sinal e vírgula decimal, e mais nada — <b>separador de milhar não entra de propósito</b>.
    ///
    /// Aceitar "1.234,56" tornaria a validação incompatível com o Mapper típico, que faz
    /// <c>Replace(',', '.')</c> e parseia com InvariantCulture: ali "1.00" vira 1, enquanto
    /// aqui viraria 100. Um valor que passa na validação e chega diferente no banco é pior
    /// do que um valor recusado.
    /// </summary>
    private const NumberStyles EstiloDecimalBrasileiro = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool DataValida(string? valor, string formato)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        return DateTime.TryParseExact(valor, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    public static bool DataEntre(string? valor, DateTime minimo, DateTime maximo, string formato)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        if (!DateTime.TryParseExact(valor, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            return false;

        return data >= minimo && data <= maximo;
    }

    public static bool DataNoPassado(string? valor, string formato)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        if (!DateTime.TryParseExact(valor, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            return false;

        return data.Date <= DateTime.Today;
    }

    public static bool InteiroValido(string? valor) =>
        !string.IsNullOrEmpty(valor) && long.TryParse(valor, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);

    public static bool InteiroEntre(string? valor, long minimo, long maximo)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        if (!long.TryParse(valor, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var numero))
            return false;

        return numero >= minimo && numero <= maximo;
    }

    /// <summary>Decimal no padrão brasileiro: "1234,56" passa, "1.234,56" não.</summary>
    public static bool DecimalValido(string? valor) =>
        !string.IsNullOrEmpty(valor) && decimal.TryParse(valor, EstiloDecimalBrasileiro, CulturaBrasileira, out _);

    public static bool DecimalEntre(string? valor, decimal minimo, decimal maximo)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        if (!decimal.TryParse(valor, EstiloDecimalBrasileiro, CulturaBrasileira, out var numero))
            return false;

        return numero >= minimo && numero <= maximo;
    }

    public static bool DecimalMaiorQue(string? valor, decimal limite)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        if (!decimal.TryParse(valor, EstiloDecimalBrasileiro, CulturaBrasileira, out var numero))
            return false;

        return numero > limite;
    }

    /// <summary>
    /// Valor monetário com número exato de casas decimais e sem separador de milhar: "1234,56".
    /// Mais estrito que <see cref="DecimalValido"/> de propósito — é o formato que arquivo de
    /// carga costuma exigir.
    /// </summary>
    public static bool MoedaValida(string? valor, int casasDecimais)
    {
        if (string.IsNullOrEmpty(valor) || casasDecimais < 0)
            return false;

        var separador = valor.IndexOf(',');

        if (casasDecimais == 0)
            return separador < 0 && SomenteDigitos(valor);

        if (separador <= 0 || separador != valor.Length - casasDecimais - 1)
            return false;

        return SomenteDigitos(valor[..separador]) && SomenteDigitos(valor[(separador + 1)..]);
    }

    public static bool SomenteDigitos(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return false;

        foreach (var caractere in valor)
        {
            if (caractere is < '0' or > '9')
                return false;
        }

        return true;
    }

    public static bool ComprimentoEntre(string? valor, int minimo, int maximo)
    {
        var comprimento = valor?.Length ?? 0;
        return comprimento >= minimo && comprimento <= maximo;
    }
}
