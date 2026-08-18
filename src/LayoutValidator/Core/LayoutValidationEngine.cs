using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
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
                yield return RegistroEstruturaInvalida<T>(numeroLinha, nomesDeColuna, camposRaw, configuracaoCsv.Delimiter, colunasEsperadas);
                continue;
            }

            var raw = csv.GetRecord<TRaw>()!;
            yield return ValidarLinha(raw, numeroLinha, validador, mapper);
        }
    }

    /// <summary>
    /// Valida dados que já chegam como valores de texto separados por linha — sem arquivo, sem
    /// cabeçalho, sem <see cref="OpcoesLayout"/> e sem precisar de uma fachada de layout. Cada
    /// item de <paramref name="linhas"/> é uma linha, casada por <b>posição</b> com as
    /// propriedades <c>string</c> do Raw Model, na ordem em que foram declaradas — a mesma
    /// convenção que <see cref="ModoCabecalho.Ausente"/> já usa no caminho de arquivo. Não há
    /// conceito de cabeçalho aqui: todo item de <paramref name="linhas"/> é tratado como dado.
    ///
    /// Quem monta <paramref name="linhas"/> (ex.: a partir de um <c>DataTable</c> com o retorno
    /// de uma consulta) é responsável por converter e formatar cada valor do jeito que o
    /// <paramref name="validador"/>/<paramref name="mapper"/> esperam — a engine não faz nenhuma
    /// conversão de tipo nem tentativa de adivinhar formato.
    /// </summary>
    public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
        IEnumerable<IReadOnlyList<string>> linhas,
        IValidator<TRaw> validador,
        ILayoutMapper<TRaw, T> mapper)
        where TRaw : class, new()
    {
        var nomesDeColuna = PropriedadesDeTexto<TRaw>.Nomes;
        var colunasEsperadas = nomesDeColuna.Length;
        var numeroLinha = 0;

        foreach (var valores in linhas)
        {
            numeroLinha++;

            if (valores.Count != colunasEsperadas)
            {
                yield return RegistroEstruturaInvalida<T>(numeroLinha, nomesDeColuna, valores.ToArray(), ", ", colunasEsperadas);
                continue;
            }

            var raw = ConstruirRawPosicional<TRaw>(valores);
            yield return ValidarLinha(raw, numeroLinha, validador, mapper);
        }
    }

    /// <summary>
    /// Valida dados que já chegam como uma linha de texto delimitada por linha — sem arquivo e
    /// sem precisar de uma fachada de layout. Cada item de <paramref name="linhas"/> é quebrado
    /// em campos pelo <see cref="OpcoesLayout.Delimitador"/> de <paramref name="opcoes"/> (com o
    /// mesmo parser do caminho de arquivo, então aspas e escaping são tratados igual) e casado
    /// por <b>posição</b>, mesma convenção do overload que recebe
    /// <c>IEnumerable&lt;IReadOnlyList&lt;string&gt;&gt;</c>.
    ///
    /// <see cref="OpcoesLayout.Cabecalho"/> é <b>ignorado</b> aqui — este caminho nunca tem
    /// cabeçalho, todo item de <paramref name="linhas"/> é dado. Reusar aqui o mesmo <see
    /// cref="OpcoesLayout"/> de uma eventual fachada do layout (arquivo) é o jeito de garantir
    /// que os dois caminhos usam o mesmo delimitador sem duplicar o valor — só o
    /// <c>Delimitador</c> é lido.
    /// </summary>
    public static IEnumerable<ResultadoValidacaoRegistro<T>> Validar<TRaw, T>(
        IEnumerable<string> linhas,
        OpcoesLayout opcoes,
        IValidator<TRaw> validador,
        ILayoutMapper<TRaw, T> mapper)
        where TRaw : class, new()
    {
        var nomesDeColuna = PropriedadesDeTexto<TRaw>.Nomes;
        var colunasEsperadas = nomesDeColuna.Length;
        var configuracaoLinha = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = opcoes.Delimitador,
            HasHeaderRecord = false
        };
        var numeroLinha = 0;

        foreach (var linha in linhas)
        {
            numeroLinha++;

            using var leitorDeLinha = new StringReader(linha);
            using var parser = new CsvParser(leitorDeLinha, configuracaoLinha);

            var campos = parser.Read() ? (parser.Record ?? Array.Empty<string>()) : Array.Empty<string>();

            yield return campos.Length == colunasEsperadas
                ? ValidarLinha(ConstruirRawPosicional<TRaw>(campos), numeroLinha, validador, mapper)
                : RegistroEstruturaInvalida<T>(numeroLinha, nomesDeColuna, campos, opcoes.Delimitador, colunasEsperadas);
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

    /// <summary>
    /// Constrói um <typeparamref name="TRaw"/> a partir de valores já na ordem posicional das
    /// propriedades <c>string</c> do Raw Model — reusado pelos caminhos que não têm cabeçalho
    /// (sem arquivo, ou <see cref="ModoCabecalho.Ausente"/>).
    /// </summary>
    private static TRaw ConstruirRawPosicional<TRaw>(IReadOnlyList<string> valores)
        where TRaw : class, new()
    {
        var raw = new TRaw();
        var propriedades = PropriedadesDeTexto<TRaw>.Todas;

        for (var i = 0; i < propriedades.Length; i++)
            propriedades[i].SetValue(raw, valores[i]);

        return raw;
    }

    /// <summary>
    /// O passo agnóstico de fonte, comum aos três caminhos de entrada: valida o Raw Model já
    /// montado e, se passar, mapeia pro tipo final; senão monta o <see cref="RegistroInvalido{T}"/>
    /// a partir dos erros do FluentValidation.
    /// </summary>
    private static ResultadoValidacaoRegistro<T> ValidarLinha<TRaw, T>(
        TRaw raw, int numeroLinha, IValidator<TRaw> validador, ILayoutMapper<TRaw, T> mapper)
    {
        var resultado = validador.Validate(raw);

        if (resultado.IsValid)
        {
            return new RegistroValido<T>
            {
                NumeroLinha = numeroLinha,
                Registro = mapper.Map(raw)
            };
        }

        return new RegistroInvalido<T>
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

    /// <summary>
    /// Uma linha cuja contagem de campos não bate com o número de propriedades do Raw Model —
    /// mesmo tratamento nos três caminhos de entrada: vira <see cref="RegistroInvalido{T}"/> com
    /// <c>NomeRegra = "EstruturaDeColunas"</c>, sem interromper a leitura das demais linhas.
    /// </summary>
    private static RegistroInvalido<T> RegistroEstruturaInvalida<T>(
        int numeroLinha, string[] nomesDeColuna, string[] campos, string separadorParaMensagem, int colunasEsperadas)
    {
        return new RegistroInvalido<T>
        {
            NumeroLinha = numeroLinha,
            ValoresRaw = ExtrairValoresRawDaLinha(nomesDeColuna, campos),
            Erros = new[]
            {
                new ErroValidacaoLayout
                {
                    NumeroLinha = numeroLinha,
                    NomeCampo = "(linha)",
                    ValorRaw = string.Join(separadorParaMensagem, campos),
                    NomeRegra = "EstruturaDeColunas",
                    Mensagem = $"Linha com {campos.Length} coluna(s), esperado {colunasEsperadas}."
                }
            }
        };
    }
}
