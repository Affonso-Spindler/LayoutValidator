namespace LayoutValidator.Core;

public abstract record ResultadoValidacaoRegistro<T>
{
    public required int NumeroLinha { get; init; }
}

public sealed record RegistroValido<T> : ResultadoValidacaoRegistro<T>
{
    public required T Registro { get; init; }
}

public sealed record RegistroInvalido<T> : ResultadoValidacaoRegistro<T>
{
    public required IReadOnlyDictionary<string, string> ValoresRaw { get; init; }

    public required IReadOnlyList<ErroValidacaoLayout> Erros { get; init; }
}
