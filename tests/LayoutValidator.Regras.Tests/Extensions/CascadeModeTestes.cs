using FluentValidation;

namespace LayoutValidator.Regras.Tests.Extensions;

/// <summary>
/// Documenta executavelmente por que todo validador de layout declara
/// <c>RuleLevelCascadeMode = CascadeMode.Stop</c>.
///
/// O default do FluentValidation é <c>Continue</c>: encadear duas regras no mesmo campo faz
/// as duas rodarem mesmo depois de uma falhar, e uma célula ruim vira dois erros — duas
/// linhas no relatório e contagem dobrada no <c>ErrosPorRegra</c> do resumo.
/// </summary>
public class CascadeModeTestes
{
    private sealed class ValidadorSemStop : AbstractValidator<RegistroTeste>
    {
        public ValidadorSemStop() => RuleFor(registro => registro.Valor).Inteiro().InteiroEntre(1, 60);
    }

    private sealed class ValidadorComStop : AbstractValidator<RegistroTeste>
    {
        public ValidadorComStop()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(registro => registro.Valor).Inteiro().InteiroEntre(1, 60);
        }
    }

    private sealed class ValidadorDeDoisCampos : AbstractValidator<RegistroTeste>
    {
        public ValidadorDeDoisCampos()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(registro => registro.Valor).Inteiro().InteiroEntre(1, 60);
            RuleFor(registro => registro.Outro).Uf();
        }
    }

    [Fact]
    public void SemStop_UmaCelulaRuimProduzDoisErros()
    {
        var erros = new ValidadorSemStop().Validate(new RegistroTeste { Valor = "abc" }).Errors;

        Assert.Equal(2, erros.Count);
        Assert.Equal(new[] { "InteiroInvalido", "InteiroForaDoIntervalo" }, erros.Select(erro => erro.ErrorCode));
    }

    [Fact]
    public void ComStop_AMesmaCelulaProduzUmErroSo()
    {
        var erros = new ValidadorComStop().Validate(new RegistroTeste { Valor = "abc" }).Errors;

        Assert.Single(erros);
        Assert.Equal("InteiroInvalido", erros[0].ErrorCode);
    }

    [Fact]
    public void ComStop_OsDemaisCamposContinuamSendoValidados()
    {
        // Stop é no nível da regra, não da classe: para dentro de um campo, segue nos outros.
        // Se isso quebrar, a primeira coluna ruim passaria a esconder o resto da linha.
        var erros = new ValidadorDeDoisCampos()
            .Validate(new RegistroTeste { Valor = "abc", Outro = "CC" })
            .Errors;

        Assert.Equal(2, erros.Count);
        Assert.Contains(erros, erro => erro.PropertyName == nameof(RegistroTeste.Valor) && erro.ErrorCode == "InteiroInvalido");
        Assert.Contains(erros, erro => erro.PropertyName == nameof(RegistroTeste.Outro) && erro.ErrorCode == "UfInvalida");
    }
}
