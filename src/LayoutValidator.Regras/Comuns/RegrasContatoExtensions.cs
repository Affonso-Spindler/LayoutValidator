using System.Text.RegularExpressions;
using FluentValidation;

namespace LayoutValidator.Regras;

public static class RegrasContatoExtensions
{
    // Validação de formato, não de existência: parte local, arroba, domínio com ao menos um
    // ponto, sem espaço em lugar nenhum. Deliberadamente permissiva — a RFC 5322 completa
    // aceita coisas que ninguém digita e recusar e-mail real é pior do que aceitar um esquisito.
    private static readonly Regex PadraoEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> regra) =>
        ConstrutorRegra.DeFormato(
            regra,
            PadraoEmail.IsMatch,
            "EmailInvalido",
            "'{PropertyName}' não é um e-mail válido.");
}
