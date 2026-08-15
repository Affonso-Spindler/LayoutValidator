namespace LayoutValidator.Tests.Modelos;

public sealed record RegistroTeste
{
    public required string Nome { get; init; }

    public required int Idade { get; init; }

    public required DateTime DataNascimento { get; init; }
}
