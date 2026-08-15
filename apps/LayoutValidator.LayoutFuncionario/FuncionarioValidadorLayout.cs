using System.Globalization;
using CsvHelper.Configuration;
using FluentValidation;
using LayoutValidator.Core;

namespace LayoutValidator.LayoutFuncionario;

public sealed class FuncionarioValidadorLayout : IValidadorLayout<Funcionario>
{
    private readonly IValidator<FuncionarioRaw> _validador;
    private readonly ILayoutMapper<FuncionarioRaw, Funcionario> _mapper;

    public FuncionarioValidadorLayout(IValidator<FuncionarioRaw> validador, ILayoutMapper<FuncionarioRaw, Funcionario> mapper)
    {
        _validador = validador;
        _mapper = mapper;
    }

    public IEnumerable<ResultadoValidacaoRegistro<Funcionario>> Validar(TextReader leitor)
    {
        var configuracaoCsv = new CsvConfiguration(CultureInfo.InvariantCulture);
        return LayoutValidationEngine.Validar(leitor, configuracaoCsv, _validador, _mapper);
    }
}
