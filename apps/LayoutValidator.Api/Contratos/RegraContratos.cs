namespace LayoutValidator.Api.Contratos;

public sealed record ParametroEsperadoResponse(string Nome, string Tipo, bool Obrigatorio);
public sealed record RegraDisponivelResponse(string Chave, IReadOnlyList<ParametroEsperadoResponse> ParametrosEsperados);
