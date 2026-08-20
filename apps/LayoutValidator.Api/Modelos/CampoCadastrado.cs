namespace LayoutValidator.Api.Modelos;

public sealed class CampoCadastrado
{
    public int Id { get; set; }
    public int LayoutId { get; set; }
    public required string Nome { get; set; }
    public int Ordem { get; set; }
    public List<RegraCampoCadastrada> Regras { get; set; } = new();
}
