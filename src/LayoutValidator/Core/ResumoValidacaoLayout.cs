namespace LayoutValidator.Core;

public sealed class ResumoValidacaoLayout
{
    private readonly Dictionary<string, int> _errosPorRegra = new();
    private readonly Dictionary<string, int> _errosPorCampo = new();

    public int TotalRegistros { get; private set; }

    public int RegistrosValidos { get; private set; }

    public int RegistrosInvalidos { get; private set; }

    public IReadOnlyDictionary<string, int> ErrosPorRegra => _errosPorRegra;

    public IReadOnlyDictionary<string, int> ErrosPorCampo => _errosPorCampo;

    public void Registrar<T>(ResultadoValidacaoRegistro<T> resultado)
    {
        TotalRegistros++;

        if (resultado is RegistroValido<T>)
        {
            RegistrosValidos++;
            return;
        }

        if (resultado is not RegistroInvalido<T> invalido)
            return;

        RegistrosInvalidos++;

        foreach (var erro in invalido.Erros)
        {
            _errosPorRegra[erro.NomeRegra] = _errosPorRegra.GetValueOrDefault(erro.NomeRegra) + 1;
            _errosPorCampo[erro.NomeCampo] = _errosPorCampo.GetValueOrDefault(erro.NomeCampo) + 1;
        }
    }
}
