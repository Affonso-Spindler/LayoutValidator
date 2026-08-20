using System.Text.RegularExpressions;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasBrasilExtensions.</summary>
internal static class RegrasDeDocumentoCatalogo
{
    private static readonly Regex PadraoCep = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);

    private static readonly Regex PadraoTelefone =
        new(@"^(\(\d{2}\) \d{4,5}-\d{4}|\d{10,11})$", RegexOptions.Compiled);

    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cpf", "CpfInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CpfValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CPF válido (11 dígitos, sem máscara, com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cnpj", "CnpjInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CnpjValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CNPJ válido (14 dígitos, sem máscara, com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "CpfOuCnpj", "CpfOuCnpjInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CpfOuCnpjValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um CPF nem um CNPJ válido.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cep", "CepInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => PadraoCep.IsMatch(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um CEP no formato 00000-000 ou 00000000.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Uf", "UfInvalida", Array.Empty<ParametroEsperado>(),
            (valor, _) => UnidadesFederativas.Valida(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser a sigla de uma unidade federativa brasileira.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Telefone", "TelefoneInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => PadraoTelefone.IsMatch(valor),
            (nomeCampo, _) => $"'{nomeCampo}' deve ser um telefone no formato (00) 00000-0000 ou só os dígitos.");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Cnh", "CnhInvalida", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.CnhValida(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é uma CNH válida (11 dígitos com dígito verificador correto).");

        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "PisPasep", "PisPasepInvalido", Array.Empty<ParametroEsperado>(),
            (valor, _) => Documentos.PisPasepValido(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um PIS/PASEP válido (11 dígitos com dígito verificador correto).");
    }
}
