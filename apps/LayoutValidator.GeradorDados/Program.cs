using System.Globalization;

var quantidadeLinhas = args.Length > 0 ? int.Parse(args[0]) : 1_000_000;
var caminhoSaida = args.Length > 1 ? args[1] : Path.Combine("..", "..", "dados-teste", "funcionarios_1000000.csv");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminhoSaida))!);

var aleatorio = new Random(42);

string[] cabecalho =
{
    "MatriculaId", "Nome", "Cpf", "Rg", "DataNascimento", "Email", "Telefone", "Cargo",
    "Departamento", "Salario", "DataAdmissao", "DataDemissao", "Ativo", "Cep", "Endereco",
    "NumeroEndereco", "Complemento", "Bairro", "Cidade", "Uf", "CargaHoraria", "PercentualComissao"
};

string[] primeirosNomes = { "Maria", "João", "Ana", "Pedro", "Carlos", "Fernanda", "Juliana", "Marcos", "Patrícia", "Rafael", "Camila", "Bruno", "Aline", "Diego", "Larissa", "Gustavo", "Vanessa", "Thiago", "Beatriz", "Rodrigo" };
string[] sobrenomes = { "Silva", "Souza", "Costa", "Lima", "Alves", "Pereira", "Ferreira", "Rodrigues", "Almeida", "Nascimento", "Carvalho", "Gomes", "Martins", "Araújo", "Barbosa" };
string[] cargos = { "Analista", "Gerente", "Assistente", "Coordenador", "Diretor", "Supervisor", "Estagiário", "Técnico" };
string[] departamentos = { "Financeiro", "TI", "RH", "Comercial", "Operações", "Marketing", "Jurídico", "Logística" };
string[] ruas = { "Rua das Flores", "Avenida Brasil", "Rua Sete de Setembro", "Avenida Paulista", "Rua XV de Novembro", "Rua das Palmeiras", "Avenida Getúlio Vargas" };
string[] bairros = { "Centro", "Jardim América", "Vila Nova", "Boa Vista", "Santa Cecília", "Cidade Alta", "Jardim Europa" };
string[] cidades = { "São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba", "Porto Alegre", "Salvador", "Recife", "Fortaleza", "Campinas", "Goiânia" };
string[] ufs = { "SP", "RJ", "MG", "PR", "RS", "BA", "PE", "CE", "GO", "SC" };
string[] camposCorrompiveis =
{
    "MatriculaId", "Nome", "Cpf", "Rg", "DataNascimento", "Email", "Telefone", "Cargo",
    "Departamento", "Salario", "DataAdmissao", "Ativo", "Cep", "Endereco",
    "NumeroEndereco", "Bairro", "Cidade", "Uf", "CargaHoraria", "PercentualComissao"
};

var totalValidas = 0;
var totalUmErro = 0;
var totalDoisErros = 0;
var totalEstruturais = 0;

using (var escritor = new StreamWriter(caminhoSaida, append: false))
{
    escritor.WriteLine(string.Join(',', cabecalho));

    for (var i = 1; i <= quantidadeLinhas; i++)
    {
        var campos = GerarLinhaValida(i);
        var sorteio = aleatorio.NextDouble();

        if (sorteio < 0.85)
        {
            totalValidas++;
        }
        else if (sorteio < 0.97)
        {
            CorromperCampo(campos);
            totalUmErro++;
        }
        else if (sorteio < 0.995)
        {
            CorromperCampo(campos);
            CorromperCampo(campos);
            totalDoisErros++;
        }
        else
        {
            escritor.WriteLine(string.Join(',', campos.Take(aleatorio.Next(5, 20)).Select(EscaparCampoCsv)));
            totalEstruturais++;
            continue;
        }

        escritor.WriteLine(string.Join(',', campos.Select(EscaparCampoCsv)));
    }
}

Console.WriteLine($"Arquivo gerado em: {Path.GetFullPath(caminhoSaida)}");
Console.WriteLine($"Total de linhas:        {quantidadeLinhas}");
Console.WriteLine($"  Válidas:              {totalValidas}");
Console.WriteLine($"  Com 1 erro:           {totalUmErro}");
Console.WriteLine($"  Com 2 erros:          {totalDoisErros}");
Console.WriteLine($"  Estruturais (colunas):{totalEstruturais}");

var caminhoResumo = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(caminhoSaida))!, "resumo_geracao.txt");
File.WriteAllLines(caminhoResumo, new[]
{
    $"Arquivo: {Path.GetFileName(caminhoSaida)}",
    $"Total de linhas: {quantidadeLinhas}",
    $"Válidas: {totalValidas}",
    $"Com 1 erro: {totalUmErro}",
    $"Com 2 erros: {totalDoisErros}",
    $"Estruturais (colunas faltando): {totalEstruturais}",
    $"Total de registros esperados como inválidos: {totalUmErro + totalDoisErros + totalEstruturais}"
});
Console.WriteLine($"Resumo gravado em: {caminhoResumo}");

string[] GerarLinhaValida(int matricula)
{
    var nome = $"{Escolher(primeirosNomes)} {Escolher(sobrenomes)}";
    var dataNascimento = DataAleatoria(1960, 2003);
    var dataAdmissao = DataAleatoria(2005, 2025);
    var ativo = aleatorio.NextDouble() < 0.9;
    var dataDemissao = ativo ? "" : DataAleatoria(2020, 2026);
    var salario = (aleatorio.Next(180000, 2500000) / 100m).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
    var percentualComissao = aleatorio.NextDouble() < 0.5
        ? ""
        : (aleatorio.Next(0, 3000) / 100m).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    return new[]
    {
        matricula.ToString(CultureInfo.InvariantCulture),
        nome,
        GerarCpf(),
        aleatorio.Next(1000000, 999999999).ToString(CultureInfo.InvariantCulture),
        dataNascimento,
        $"{nome.Replace(' ', '.').ToLowerInvariant()}{matricula}@empresa.com.br",
        $"({aleatorio.Next(11, 99)}) {aleatorio.Next(90000, 99999)}-{aleatorio.Next(1000, 9999)}",
        Escolher(cargos),
        Escolher(departamentos),
        salario,
        dataAdmissao,
        dataDemissao,
        ativo ? "S" : "N",
        $"{aleatorio.Next(10000, 99999)}-{aleatorio.Next(100, 999)}",
        Escolher(ruas),
        aleatorio.Next(1, 9999).ToString(CultureInfo.InvariantCulture),
        aleatorio.NextDouble() < 0.3 ? $"Apto {aleatorio.Next(1, 200)}" : "",
        Escolher(bairros),
        Escolher(cidades),
        Escolher(ufs),
        new[] { 20, 30, 40, 44 }[aleatorio.Next(0, 4)].ToString(CultureInfo.InvariantCulture),
        percentualComissao
    };
}

void CorromperCampo(string[] campos)
{
    var nomeCampo = Escolher(camposCorrompiveis);
    var indice = Array.IndexOf(cabecalho, nomeCampo);

    campos[indice] = nomeCampo switch
    {
        "MatriculaId" => "ABC123",
        "Nome" => "",
        "Cpf" => aleatorio.Next(0, 2) == 0 ? "123456789" : "12345678900X",
        "Rg" => "12",
        "DataNascimento" => "31/02/2000",
        "Email" => "sem-arroba-nem-ponto",
        "Telefone" => "11987654321",
        "Cargo" => "",
        "Departamento" => "",
        "Salario" => aleatorio.Next(0, 2) == 0 ? "cincomil" : "5000.00",
        "DataAdmissao" => "2020-13-40",
        "Ativo" => "X",
        "Cep" => "1234567",
        "Endereco" => "",
        "NumeroEndereco" => "abc",
        "Bairro" => "",
        "Cidade" => "",
        "Uf" => "XX",
        "CargaHoraria" => "100",
        "PercentualComissao" => "150,00",
        _ => campos[indice]
    };
}

string GerarCpf() => string.Concat(Enumerable.Range(0, 11).Select(_ => aleatorio.Next(0, 10).ToString(CultureInfo.InvariantCulture)));

string DataAleatoria(int anoInicio, int anoFim)
{
    var ano = aleatorio.Next(anoInicio, anoFim + 1);
    var mes = aleatorio.Next(1, 13);
    var dia = aleatorio.Next(1, DateTime.DaysInMonth(ano, mes) + 1);
    return new DateTime(ano, mes, dia).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}

T Escolher<T>(T[] itens) => itens[aleatorio.Next(itens.Length)];

string EscaparCampoCsv(string campo)
{
    if (campo.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        return campo;

    return "\"" + campo.Replace("\"", "\"\"") + "\"";
}
