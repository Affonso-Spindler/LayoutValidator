namespace LayoutValidator.Api.Regras;

public sealed class CatalogoDeRegras : ICatalogoDeRegras
{
    private readonly IReadOnlyDictionary<string, RegraCadastrada> _regras;

    public CatalogoDeRegras()
    {
        var todas = RegrasDeTextoCatalogo.Construir()
            .Concat(RegrasNumericasCatalogo.Construir())
            .Concat(RegrasDeDocumentoCatalogo.Construir());

        _regras = todas.ToDictionary(regra => regra.Chave, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RegraCadastrada> Todas => _regras.Values.ToList();

    public bool Existe(string chave) => _regras.ContainsKey(chave);

    public RegraCadastrada Obter(string chave) =>
        _regras.TryGetValue(chave, out var regra)
            ? regra
            : throw new InvalidOperationException($"Regra '{chave}' não existe no catálogo.");
}
