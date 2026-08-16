namespace LayoutValidator.Regras.Predicados;

/// <summary>
/// Predicados de documentos, sem dependência de FluentValidation.
/// Todos são totais: nunca lançam exceção, qualquer que seja a entrada.
/// Nenhum deles aceita máscara — o valor precisa vir só com dígitos.
/// </summary>
public static class Documentos
{
    public static bool CpfValido(string? valor)
    {
        if (!EstruturaNumericaValida(valor, 11))
            return false;

        var digitos = ParaDigitos(valor!);
        var primeiro = DigitoModulo11(digitos, 9, 10);
        var segundo = DigitoModulo11(digitos, 10, 11);

        return digitos[9] == primeiro && digitos[10] == segundo;
    }

    public static bool CnpjValido(string? valor)
    {
        if (!EstruturaNumericaValida(valor, 14))
            return false;

        var digitos = ParaDigitos(valor!);
        var primeiro = DigitoModulo11ComPesosCiclicos(digitos, 12);
        var segundo = DigitoModulo11ComPesosCiclicos(digitos, 13);

        return digitos[12] == primeiro && digitos[13] == segundo;
    }

    public static bool CpfOuCnpjValido(string? valor) => CpfValido(valor) || CnpjValido(valor);

    /// <summary>PIS/PASEP/NIT/NIS — 11 dígitos, módulo 11 com pesos 3,2,9,8,7,6,5,4,3,2.</summary>
    public static bool PisPasepValido(string? valor)
    {
        if (!EstruturaNumericaValida(valor, 11))
            return false;

        var digitos = ParaDigitos(valor!);
        int[] pesos = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var soma = 0;
        for (var i = 0; i < 10; i++)
            soma += digitos[i] * pesos[i];

        var digito = 11 - (soma % 11);
        if (digito >= 10)
            digito = 0;

        return digitos[10] == digito;
    }

    /// <summary>CNH — 11 dígitos, algoritmo do Denatran com o desconto de 2 no segundo dígito.</summary>
    public static bool CnhValida(string? valor)
    {
        if (!EstruturaNumericaValida(valor, 11))
            return false;

        var digitos = ParaDigitos(valor!);

        var somaDecrescente = 0;
        var somaCrescente = 0;
        for (int i = 0, peso = 9; i < 9; i++, peso--)
        {
            somaDecrescente += digitos[i] * peso;
            somaCrescente += digitos[i] * (i + 1);
        }

        var desconto = 0;
        var primeiro = somaDecrescente % 11;
        if (primeiro >= 10)
        {
            primeiro = 0;
            desconto = 2;
        }

        var segundo = (somaCrescente % 11) - desconto;
        if (segundo < 0)
            segundo += 11;
        if (segundo >= 10)
            segundo = 0;

        return digitos[9] == primeiro && digitos[10] == segundo;
    }

    /// <summary>Algoritmo de Luhn — cartão de crédito, entre 13 e 19 dígitos.</summary>
    public static bool LuhnValido(string? valor)
    {
        if (!Formatos.SomenteDigitos(valor))
            return false;

        if (valor!.Length is < 13 or > 19)
            return false;

        var soma = 0;
        var dobrar = false;

        for (var i = valor.Length - 1; i >= 0; i--)
        {
            var digito = valor[i] - '0';

            if (dobrar)
            {
                digito *= 2;
                if (digito > 9)
                    digito -= 9;
            }

            soma += digito;
            dobrar = !dobrar;
        }

        return soma % 10 == 0;
    }

    /// <summary>
    /// Comprimento certo, só dígitos, e não é sequência de um dígito repetido —
    /// "00000000000" passa em qualquer módulo 11 mas não é documento de ninguém.
    /// </summary>
    private static bool EstruturaNumericaValida(string? valor, int comprimento)
    {
        if (valor is null || valor.Length != comprimento || !Formatos.SomenteDigitos(valor))
            return false;

        for (var i = 1; i < valor.Length; i++)
        {
            if (valor[i] != valor[0])
                return true;
        }

        return false;
    }

    private static int[] ParaDigitos(string valor)
    {
        var digitos = new int[valor.Length];
        for (var i = 0; i < valor.Length; i++)
            digitos[i] = valor[i] - '0';

        return digitos;
    }

    /// <summary>Módulo 11 com pesos decrescentes — usado pelo CPF.</summary>
    private static int DigitoModulo11(int[] digitos, int quantidade, int pesoInicial)
    {
        var soma = 0;
        for (int i = 0, peso = pesoInicial; i < quantidade; i++, peso--)
            soma += digitos[i] * peso;

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    /// <summary>Módulo 11 com pesos que reiniciam em 9 ao passar de 2 — usado pelo CNPJ.</summary>
    private static int DigitoModulo11ComPesosCiclicos(int[] digitos, int quantidade)
    {
        var soma = 0;
        var peso = 2;

        for (var i = quantidade - 1; i >= 0; i--)
        {
            soma += digitos[i] * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
