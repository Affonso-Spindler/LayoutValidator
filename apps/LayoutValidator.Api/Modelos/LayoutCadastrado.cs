namespace LayoutValidator.Api.Modelos;

public sealed class LayoutCadastrado
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nome { get; set; }
    public required string Delimitador { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public List<CampoCadastrado> Campos { get; set; } = new();
}
