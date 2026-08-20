namespace LayoutValidator.Api.Validacao;

public sealed record ErroDeCampo(string Campo, string ValorRaw, string Regra, string Mensagem);
