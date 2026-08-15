namespace LayoutValidator.Sample.Models;

public sealed record Pessoa
{
    public required string Nome { get; init; }

    public required int Idade { get; init; }

    public required DateTime DataNascimento { get; init; }

    public required string Cpf { get; init; }
}
