namespace LayoutValidator.Api.Regras;

public interface ICatalogoDeRegras
{
    bool Existe(string chave);
    RegraCadastrada Obter(string chave);
    IReadOnlyList<RegraCadastrada> Todas { get; }
}
