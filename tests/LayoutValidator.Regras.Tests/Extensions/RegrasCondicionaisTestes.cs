using FluentValidation;

namespace LayoutValidator.Regras.Tests.Extensions;

/// <summary>
/// Documenta executavelmente as duas pegadinhas de regra entre campos (um campo que depende
/// do valor de outro). Aqui <c>Outro</c> faz o papel do campo que decide — como o
/// <c>Ativo</c> decide o que se exige de <c>DataDemissao</c> no layout Funcionario.
/// </summary>
public class RegrasCondicionaisTestes
{
    private const string DataInvalida = "31/02/2000";

    private sealed class WhenNoFimDaCadeia : AbstractValidator<RegistroTeste>
    {
        public WhenNoFimDaCadeia()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(registro => registro.Valor).Obrigatorio().Data().When(registro => registro.Outro == "N");
        }
    }

    private sealed class WhenSoNaRegraAoLado : AbstractValidator<RegistroTeste>
    {
        public WhenSoNaRegraAoLado()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(registro => registro.Valor)
                .Obrigatorio().When(registro => registro.Outro == "N", ApplyConditionTo.CurrentValidator)
                .Data();
        }
    }

    [Fact]
    public void WhenNoFimDaCadeia_DesligaTambemAsRegrasAnteriores()
    {
        // O default é ApplyConditionTo.AllValidators: com a condição falsa, o Data() também é
        // pulado e uma data inválida passa calada. Quase nunca é o que se quer.
        var erros = new WhenNoFimDaCadeia().Validate(new RegistroTeste { Valor = DataInvalida, Outro = "S" }).Errors;

        Assert.Empty(erros);
    }

    [Fact]
    public void WhenComCurrentValidator_CondicionaSoARegraAoLado()
    {
        var erros = new WhenSoNaRegraAoLado().Validate(new RegistroTeste { Valor = DataInvalida, Outro = "S" }).Errors;

        Assert.Equal("DataInvalida", Assert.Single(erros).ErrorCode);
    }

    [Fact]
    public void WhenComCurrentValidator_AplicaARegraQuandoACondicaoVale()
    {
        var erros = new WhenSoNaRegraAoLado().Validate(new RegistroTeste { Valor = "", Outro = "N" }).Errors;

        Assert.Equal("CampoObrigatorio", Assert.Single(erros).ErrorCode);
    }

    [Fact]
    public void RegraNoObjetoInteiro_SaiSemNomeDeCampo()
    {
        // PropertyName é o que vira NomeCampo no ErroValidacaoLayout: em branco no relatório e
        // chave vazia no ErrosPorCampo do resumo.
        var validador = new InlineValidator<RegistroTeste>();
        validador.RuleFor(registro => registro).Must(_ => false).WithErrorCode("Combinacao");

        var erro = Assert.Single(validador.Validate(new RegistroTeste()).Errors);

        Assert.Equal(string.Empty, erro.PropertyName);
    }

    [Fact]
    public void RegraNoObjetoInteiro_ComWithName_SaiComONomeDoCampo()
    {
        var validador = new InlineValidator<RegistroTeste>();
        validador.RuleFor(registro => registro).Must(_ => false).WithErrorCode("Combinacao")
            .WithName(nameof(RegistroTeste.Valor));

        var erro = Assert.Single(validador.Validate(new RegistroTeste()).Errors);

        Assert.Equal(nameof(RegistroTeste.Valor), erro.PropertyName);
    }
}
