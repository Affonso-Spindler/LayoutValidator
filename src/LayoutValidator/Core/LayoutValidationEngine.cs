using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;

namespace LayoutValidator.Core;

public static class LayoutValidationEngine
{
    public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
        TextReader leitor,
        CsvConfiguration configuracaoCsv,
        IValidator<TRaw> validador,
        ILayoutMapper<TRaw, T> mapper)
        where TRaw : class, new()
    {
        // Dado ruim ou campo ausente não deve abortar o streaming: cada linha problemática vira um RegistroInvalido.
        configuracaoCsv.BadDataFound = null;
        configuracaoCsv.MissingFieldFound = null;

        using var csv = new CsvReader(leitor, configuracaoCsv);
        csv.Read();
        csv.ReadHeader();
        var cabecalho = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var numeroLinha = csv.Parser.Row;
            var camposRaw = csv.Parser.Record ?? Array.Empty<string>();

            if (camposRaw.Length != cabecalho.Length)
            {
                yield return new RegistroInvalido<T>
                {
                    NumeroLinha = numeroLinha,
                    ValoresRaw = ExtrairValoresRawDaLinha(cabecalho, camposRaw),
                    Erros = new[]
                    {
                        new ErroValidacaoLayout
                        {
                            NumeroLinha = numeroLinha,
                            NomeCampo = "(linha)",
                            ValorRaw = string.Join(configuracaoCsv.Delimiter, camposRaw),
                            NomeRegra = "EstruturaDeColunas",
                            Mensagem = $"Linha com {camposRaw.Length} coluna(s), esperado {cabecalho.Length}."
                        }
                    }
                };
                continue;
            }

            var raw = csv.GetRecord<TRaw>()!;
            var resultado = validador.Validate(raw);

            if (resultado.IsValid)
            {
                yield return new RegistroValido<T>
                {
                    NumeroLinha = numeroLinha,
                    Registro = mapper.Map(raw)
                };
                continue;
            }

            yield return new RegistroInvalido<T>
            {
                NumeroLinha = numeroLinha,
                ValoresRaw = ExtrairValoresRaw(raw),
                Erros = resultado.Errors.Select(falha => new ErroValidacaoLayout
                {
                    NumeroLinha = numeroLinha,
                    NomeCampo = falha.PropertyName,
                    ValorRaw = falha.AttemptedValue?.ToString() ?? string.Empty,
                    NomeRegra = string.IsNullOrEmpty(falha.ErrorCode) ? falha.ErrorMessage : falha.ErrorCode,
                    Mensagem = falha.ErrorMessage
                }).ToList()
            };
        }
    }

    private static IReadOnlyDictionary<string, string> ExtrairValoresRaw<TRaw>(TRaw raw)
    {
        return typeof(TRaw)
            .GetProperties()
            .Where(propriedade => propriedade.PropertyType == typeof(string))
            .ToDictionary(propriedade => propriedade.Name, propriedade => (string?)propriedade.GetValue(raw) ?? string.Empty);
    }

    private static IReadOnlyDictionary<string, string> ExtrairValoresRawDaLinha(string[] cabecalho, string[] campos)
    {
        return cabecalho
            .Zip(campos, (nome, valor) => (nome, valor))
            .ToDictionary(par => par.nome, par => par.valor);
    }
}
