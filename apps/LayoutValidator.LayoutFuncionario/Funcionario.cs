namespace LayoutValidator.LayoutFuncionario;

public sealed record Funcionario
{
    public required int MatriculaId { get; init; }

    public required string Nome { get; init; }

    public required string Cpf { get; init; }

    public required string Rg { get; init; }

    public required DateTime DataNascimento { get; init; }

    public required string Email { get; init; }

    public required string Telefone { get; init; }

    public required string Cargo { get; init; }

    public required string Departamento { get; init; }

    public required decimal Salario { get; init; }

    public required DateTime DataAdmissao { get; init; }

    public DateTime? DataDemissao { get; init; }

    public required bool Ativo { get; init; }

    public required string Cep { get; init; }

    public required string Endereco { get; init; }

    public required int NumeroEndereco { get; init; }

    public string? Complemento { get; init; }

    public required string Bairro { get; init; }

    public required string Cidade { get; init; }

    public required string Uf { get; init; }

    public required int CargaHoraria { get; init; }

    public decimal PercentualComissao { get; init; }
}
