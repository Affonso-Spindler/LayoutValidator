using System.Text.RegularExpressions;

namespace LayoutValidator.Api.Regras;

/// <summary>Equivalente cadastrável de LayoutValidator.Regras.RegrasContatoExtensions.</summary>
internal static class RegrasDeContatoCatalogo
{
    // Mesmo padrão de RegrasContatoExtensions (validação de formato, não de existência):
    // parte local, arroba, domínio com ao menos um ponto, sem espaço em lugar nenhum. O
    // literal é duplicado de propósito — o padrão de lá é privado, e extrair um predicado
    // compartilhado é a mesma discussão já registrada em RegrasDeDocumentoCatalogo pra
    // Cep/Telefone.
    private static readonly Regex PadraoEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static IEnumerable<RegraCadastrada> Construir()
    {
        yield return ConstrutorDeRegraCadastrada.DeFormato(
            "Email",
            "EmailInvalido",
            Array.Empty<ParametroEsperado>(),
            (valor, _) => PadraoEmail.IsMatch(valor),
            (nomeCampo, _) => $"'{nomeCampo}' não é um e-mail válido.");
    }
}
