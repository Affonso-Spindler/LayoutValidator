namespace LayoutValidator.Api.Contratos;

public sealed record ValidarRequest(string Linha);
public sealed record ErroDeCampoResponse(string Campo, string ValorRaw, string Regra, string Mensagem);
public sealed record ValidarResponse(bool Aderente, IReadOnlyList<ErroDeCampoResponse> Erros);
