using System.Globalization;
using LayoutValidator.Core;

namespace LayoutValidator.LayoutFuncionario;

public sealed class FuncionarioMapper : ILayoutMapper<FuncionarioRaw, Funcionario>
{
    public Funcionario Map(FuncionarioRaw raw) => new()
    {
        MatriculaId = int.Parse(raw.MatriculaId),
        Nome = raw.Nome,
        Cpf = raw.Cpf,
        Rg = raw.Rg,
        DataNascimento = ParseData(raw.DataNascimento),
        Email = raw.Email,
        Telefone = raw.Telefone,
        Cargo = raw.Cargo,
        Departamento = raw.Departamento,
        Salario = ParseDecimal(raw.Salario),
        DataAdmissao = ParseData(raw.DataAdmissao),
        DataDemissao = string.IsNullOrEmpty(raw.DataDemissao) ? null : ParseData(raw.DataDemissao),
        Ativo = raw.Ativo == "S",
        Cep = raw.Cep,
        Endereco = raw.Endereco,
        NumeroEndereco = int.Parse(raw.NumeroEndereco),
        Complemento = string.IsNullOrEmpty(raw.Complemento) ? null : raw.Complemento,
        Bairro = raw.Bairro,
        Cidade = raw.Cidade,
        Uf = raw.Uf,
        CargaHoraria = int.Parse(raw.CargaHoraria),
        PercentualComissao = string.IsNullOrEmpty(raw.PercentualComissao) ? 0m : ParseDecimal(raw.PercentualComissao)
    };

    private static DateTime ParseData(string valor) =>
        DateTime.ParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string valor) =>
        decimal.Parse(valor.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
}
