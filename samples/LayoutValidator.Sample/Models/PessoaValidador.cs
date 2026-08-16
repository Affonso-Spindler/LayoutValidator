using FluentValidation;
using LayoutValidator.Regras;

namespace LayoutValidator.Sample.Models;

public sealed class PessoaValidador : AbstractValidator<PessoaRaw>
{
    public PessoaValidador()
    {
        // Um erro por campo: sem isso, encadear duas regras no mesmo campo reportaria o
        // mesmo problema duas vezes no relatório e no ResumoValidacaoLayout.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(p => p.Nome).Obrigatorio();

        RuleFor(p => p.Idade).Obrigatorio().Inteiro();

        RuleFor(p => p.DataNascimento).Obrigatorio().Data();

        RuleFor(p => p.Cpf).Obrigatorio().Cpf();
    }
}
