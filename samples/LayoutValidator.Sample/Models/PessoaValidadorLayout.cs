using System.Globalization;
using CsvHelper.Configuration;
using FluentValidation;
using LayoutValidator.Core;

namespace LayoutValidator.Sample.Models;

public sealed class PessoaValidadorLayout : IValidadorLayout<Pessoa>
{
    private readonly IValidator<PessoaRaw> _validador;
    private readonly ILayoutMapper<PessoaRaw, Pessoa> _mapper;

    public PessoaValidadorLayout(IValidator<PessoaRaw> validador, ILayoutMapper<PessoaRaw, Pessoa> mapper)
    {
        _validador = validador;
        _mapper = mapper;
    }

    public IEnumerable<ResultadoValidacaoRegistro<Pessoa>> Validar(TextReader leitor)
    {
        var configuracaoCsv = new CsvConfiguration(CultureInfo.InvariantCulture);
        return LayoutValidationEngine.Validar(leitor, configuracaoCsv, _validador, _mapper);
    }
}
