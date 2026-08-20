namespace LayoutValidator.Api.Modelos;

public sealed class RegraCampoCadastrada
{
    public int Id { get; set; }
    public int CampoId { get; set; }
    public required string ChaveRegra { get; set; }
    public string? ParametrosJson { get; set; }
    public int Ordem { get; set; }
}
