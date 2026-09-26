using FluentValidation;
using LayoutValidator.Regras;

namespace LayoutValidator.LayoutFuncionario;

public sealed class FuncionarioValidador : AbstractValidator<FuncionarioRaw>
{
    public FuncionarioValidador()
    {
        // Um erro por campo: sem isso, encadear duas regras no mesmo campo reportaria o
        // mesmo problema duas vezes no relatório e no ResumoValidacaoLayout.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(f => f.MatriculaId).Obrigatorio().InteiroPositivo();

        RuleFor(f => f.Nome).Obrigatorio();

        RuleFor(f => f.Cpf).Obrigatorio().Cpf();

        RuleFor(f => f.Rg).Obrigatorio().SomenteDigitos().ComprimentoEntre(7, 9);

        RuleFor(f => f.DataNascimento).Obrigatorio().Data();

        RuleFor(f => f.Email).Obrigatorio().Email();

        RuleFor(f => f.Telefone).Obrigatorio().Telefone();

        RuleFor(f => f.Cargo).Obrigatorio();

        RuleFor(f => f.Departamento).Obrigatorio();

        RuleFor(f => f.Salario).Obrigatorio().Moeda();

        RuleFor(f => f.DataAdmissao).Obrigatorio().Data();

        // Regra entre campos: só vale Ativo = N com data, ou Ativo = S sem data. Cada When
        // condiciona só a regra ao lado (CurrentValidator) — sem isso a condição desligaria
        // também o Data(), e uma data inválida passaria calada. A comparação ignora maiúsculas
        // e minúsculas porque o ValorEm do Ativo aceita "s" e "n" — o mesmo vale no mapper.
        RuleFor(f => f.DataDemissao)
            .Obrigatorio().When(f => AtivoIgual(f, "N"), ApplyConditionTo.CurrentValidator)
            .Empty().When(f => AtivoIgual(f, "S"), ApplyConditionTo.CurrentValidator)
                .WithErrorCode("DataDemissaoEmFuncionarioAtivo")
                .WithMessage("'DataDemissao' deve ficar vazia quando Ativo = S.")
            .Data();

        RuleFor(f => f.Ativo).Obrigatorio().ValorEm("S", "N");

        RuleFor(f => f.Cep).Obrigatorio().Cep();

        RuleFor(f => f.Endereco).Obrigatorio();

        RuleFor(f => f.NumeroEndereco).Obrigatorio().Inteiro();

        RuleFor(f => f.Bairro).Obrigatorio();

        RuleFor(f => f.Cidade).Obrigatorio();

        RuleFor(f => f.Uf).Obrigatorio().Uf();

        RuleFor(f => f.CargaHoraria).Obrigatorio().InteiroEntre(1, 60);

        RuleFor(f => f.PercentualComissao).Percentual();
    }

    private static bool AtivoIgual(FuncionarioRaw funcionario, string valor) =>
        string.Equals(funcionario.Ativo, valor, StringComparison.OrdinalIgnoreCase);
}
