using FluentValidation;

namespace LayoutValidator.Core;

/// <summary>
/// Base das fachadas de layout: guarda o validador e o mapper, expõe o formato do arquivo e
/// delega pro <see cref="LayoutValidationEngine"/>.
///
/// Existe porque a fachada de todo layout é o mesmo cerimonial — dois campos, um construtor
/// que só atribui, e um <c>Validar</c> que repassa. O layout concreto fica só com o que é
/// dele: o par <c>TRaw</c>/<c>T</c> e, quando foge do padrão, as <see cref="Opcoes"/>.
///
/// Herdar daqui é conveniência, não obrigação: <see cref="IValidadorLayout{T}"/> continua
/// sendo a interface pública, e quem precisar de algo fora do comum implementa ela direto.
/// </summary>
public abstract class ValidadorLayoutBase<TRaw, T> : IValidadorLayout<T>
    where TRaw : class, new()
{
    private readonly IValidator<TRaw> _validador;
    private readonly ILayoutMapper<TRaw, T> _mapper;

    protected ValidadorLayoutBase(IValidator<TRaw> validador, ILayoutMapper<TRaw, T> mapper)
    {
        _validador = validador;
        _mapper = mapper;
    }

    /// <summary>
    /// O formato do arquivo que este layout descreve. O padrão é delimitador <c>;</c> com
    /// cabeçalho; sobrescreva quando o arquivo do seu layout for diferente.
    /// </summary>
    protected virtual OpcoesLayout Opcoes { get; } = new();

    public IEnumerable<ResultadoValidacaoRegistro<T>> Validar(TextReader leitor) =>
        LayoutValidationEngine.Validar(leitor, Opcoes, _validador, _mapper);
}
