namespace LayoutValidator.LayoutFuncionario;

/// <summary>
/// Representação "crua" do layout de Funcionário: todas as propriedades como string,
/// mapeadas 1:1 nas colunas do CSV pelo CsvHelper. Ver LayoutValidator.Core.LayoutValidationEngine
/// para o porquê de não ter tipos já convertidos aqui.
/// </summary>
public sealed class FuncionarioRaw
{
    public string MatriculaId { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Cpf { get; set; } = string.Empty;

    public string Rg { get; set; } = string.Empty;

    public string DataNascimento { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;

    public string Cargo { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Salario { get; set; } = string.Empty;

    public string DataAdmissao { get; set; } = string.Empty;

    public string DataDemissao { get; set; } = string.Empty;

    public string Ativo { get; set; } = string.Empty;

    public string Cep { get; set; } = string.Empty;

    public string Endereco { get; set; } = string.Empty;

    public string NumeroEndereco { get; set; } = string.Empty;

    public string Complemento { get; set; } = string.Empty;

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;

    public string CargaHoraria { get; set; } = string.Empty;

    public string PercentualComissao { get; set; } = string.Empty;
}
