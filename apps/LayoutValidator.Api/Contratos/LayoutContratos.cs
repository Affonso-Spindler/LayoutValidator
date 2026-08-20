using System.Text.Json;

namespace LayoutValidator.Api.Contratos;

public sealed record RegraCampoRequest(string ChaveRegra, JsonElement? ParametrosJson);
public sealed record CampoRequest(string Nome, IReadOnlyList<RegraCampoRequest> Regras);
public sealed record LayoutRequest(string Codigo, string Nome, string Delimitador, IReadOnlyList<CampoRequest> Campos);

public sealed record RegraCampoResponse(string ChaveRegra, JsonElement? ParametrosJson);
public sealed record CampoResponse(string Nome, int Ordem, IReadOnlyList<RegraCampoResponse> Regras);
public sealed record LayoutResponse(string Codigo, string Nome, string Delimitador, IReadOnlyList<CampoResponse> Campos);
