using FluentValidation;

namespace LayoutValidator.Regras;

/// <summary>
/// Costura um predicado puro com o código de erro e a mensagem, aplicando o contrato de
/// vazio do catálogo: <b>regra de formato nunca reprova valor vazio</b>.
///
/// Obrigatoriedade é declarada à parte, com <c>.Obrigatorio()</c>. Assim campo opcional é só
/// não declarar obrigatório, e campo vazio produz um único erro (<c>CampoObrigatorio</c>) em
/// vez de um por regra de formato encadeada.
/// </summary>
internal static class ConstrutorRegra
{
    internal static IRuleBuilderOptions<T, string> DeFormato<T>(
        IRuleBuilder<T, string> regra,
        Func<string, bool> predicado,
        string codigo,
        string mensagem) =>
        regra.Must(valor => string.IsNullOrWhiteSpace(valor) || predicado(valor))
            .WithErrorCode(codigo)
            .WithMessage(mensagem);
}
