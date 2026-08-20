namespace LayoutValidator.Api.Regras;

public enum TipoParametro
{
    Inteiro,
    Decimal,
    Texto,
    ListaDeTexto
}

public sealed record ParametroEsperado(string Nome, TipoParametro Tipo, bool Obrigatorio);
