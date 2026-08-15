namespace LayoutValidator.Core;

public sealed record ErroValidacaoLayout
{
    public required int NumeroLinha { get; init; }

    public required string NomeCampo { get; init; }

    public required string ValorRaw { get; init; }

    public required string NomeRegra { get; init; }

    public required string Mensagem { get; init; }
}
