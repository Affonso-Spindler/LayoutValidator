using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Endpoints;
using LayoutValidator.Api.Regras;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.AddDbContext<ApiDbContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));

var app = builder.Build();

// Backstop de processo: qualquer exceção não tratada por um handler específico (fora as
// guardas explícitas de corpo nulo/parcial nos endpoints) ainda deve virar um 500 com corpo
// JSON, não uma resposta vazia ou (fora de Development) um stack trace cru.
app.UseExceptionHandler(tratador => tratador.Run(async contexto =>
{
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
    contexto.Response.ContentType = "application/json";
    await contexto.Response.WriteAsJsonAsync(new { erro = "Erro interno inesperado." });
}));

using (var escopoDeInicializacao = app.Services.CreateScope())
{
    escopoDeInicializacao.ServiceProvider.GetRequiredService<ApiDbContext>().Database.Migrate();
}

app.MapLayoutsEndpoints();
app.MapRegrasEndpoints();
app.MapValidacaoEndpoints();

app.Run();

public partial class Program;
