var builder = WebApplication.CreateBuilder(args);

// Sem isso, a resposta sai em camelCase (padrão do ASP.NET Core) enquanto os DTOs em
// Contratos/ são PascalCase — ReadFromJsonAsync<T> nos testes de integração usa matching
// case-sensitive por padrão e preencheria tudo com null/default silenciosamente.
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

app.Run();

public partial class Program;
