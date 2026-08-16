using System.Reflection;
using CsvHelper;
using FluentValidation;

namespace LayoutValidator.Core;

public static class LayoutValidationEngine
{
    public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
        TextReader leitor,
        OpcoesLayout opcoes,
        IValidator<TRaw> validador,
        ILayoutMapper<TRaw, T> mapper)
        where TRaw : class, new()
    {
        var configuracaoCsv = opcoes.ParaConfiguracaoCsv();
        var nomeDoLayout = typeof(TRaw).Name;

        using var csv = new CsvReader(leitor, configuracaoCsv);

        // Nomes usados como chave em ValoresRaw e como contagem de colunas esperada. Com
        // cabeçalho vêm do arquivo; sem cabeçalho, das propriedades do Raw Model — que é o
        // contrato posicional nesse modo.
        string[] nomesDeColuna;

        if (opcoes.Cabecalho == ModoCabecalho.Presente)
        {
            if (!csv.Read())
                yield break;

            csv.ReadHeader();
            nomesDeColuna = csv.HeaderRecord ?? Array.Empty<string>();

            // Pergunta ao próprio CsvHelper se ele consegue localizar cada coluna do layout,
            // em vez de reimplementar a regra de casamento dele aqui.
            var colunasCasadas = PropriedadesDeTexto<TRaw>.Nomes
                .Count(nome => csv.GetFieldIndex(nome, isTryGet: true) >= 0);

            if (colunasCasadas == 0)
                throw LayoutIncompativelException.NenhumaColunaCasou(nomesDeColuna, opcoes.Delimitador, nomeDoLayout);
        }
        else
        {
            // PresenteIgnorado: consome e descarta a linha de cabeçalho antes do laço.
            if (opcoes.Cabecalho == ModoCabecalho.PresenteIgnorado && !csv.Read())
                yield break;

            nomesDeColuna = PropriedadesDeTexto<TRaw>.Nomes;
        }

        var colunasEsperadas = nomesDeColuna.Length;
        var primeiraLinhaDeDados = true;

        while (csv.Read())
        {
            var numeroLinha = csv.Parser.Row;
            var camposRaw = csv.Parser.Record ?? Array.Empty<string>();

            // Sem cabeçalho não há o que comparar por nome, então o sinal de delimitador
            // errado é a linha inteira ter virado uma coluna só.
            if (primeiraLinhaDeDados)
            {
                primeiraLinhaDeDados = false;

                if (opcoes.Cabecalho != ModoCabecalho.Presente && camposRaw.Length == 1 && colunasEsperadas > 1)
                    throw LayoutIncompativelException.ColunaUnicaInesperada(camposRaw, opcoes.Delimitador, nomeDoLayout, colunasEsperadas);
            }

            if (camposRaw.Length != colunasEsperadas)
            {
                yield return new RegistroInvalido<T>
                {
                    NumeroLinha = numeroLinha,
                    ValoresRaw = ExtrairValoresRawDaLinha(nomesDeColuna, camposRaw),
                    Erros = new[]
                    {
                        new ErroValidacaoLayout
                        {
                            NumeroLinha = numeroLinha,
                            NomeCampo = "(linha)",
                            ValorRaw = string.Join(configuracaoCsv.Delimiter, camposRaw),
                            NomeRegra = "EstruturaDeColunas",
                            Mensagem = $"Linha com {camposRaw.Length} coluna(s), esperado {colunasEsperadas}."
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

    /// <summary>
    /// As propriedades <c>string</c> do Raw Model, na ordem de declaração — que é a ordem que
    /// o CsvHelper usa pra casar por posição quando não há cabeçalho.
    /// Classe genérica estática: a reflection roda uma vez por tipo fechado, não por linha.
    /// </summary>
    private static class PropriedadesDeTexto<TRaw>
    {
        public static readonly PropertyInfo[] Todas = typeof(TRaw)
            .GetProperties()
            .Where(propriedade => propriedade.PropertyType == typeof(string))
            .ToArray();

        public static readonly string[] Nomes = Todas.Select(propriedade => propriedade.Name).ToArray();
    }

    private static IReadOnlyDictionary<string, string> ExtrairValoresRaw<TRaw>(TRaw raw)
    {
        return PropriedadesDeTexto<TRaw>.Todas
            .ToDictionary(propriedade => propriedade.Name, propriedade => (string?)propriedade.GetValue(raw) ?? string.Empty);
    }

    private static IReadOnlyDictionary<string, string> ExtrairValoresRawDaLinha(string[] nomesDeColuna, string[] campos)
    {
        return nomesDeColuna
            .Zip(campos, (nome, valor) => (nome, valor))
            .ToDictionary(par => par.nome, par => par.valor);
    }
}
