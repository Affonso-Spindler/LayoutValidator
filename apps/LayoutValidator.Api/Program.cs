using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Endpoints;
using LayoutValidator.Api.Regras;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.AddDbContext<ApiDbContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

using (var escopoDeInicializacao = app.Services.CreateScope())
{
    escopoDeInicializacao.ServiceProvider.GetRequiredService<ApiDbContext>().Database.Migrate();
}

app.MapLayoutsEndpoints();
app.MapRegrasEndpoints();
app.MapValidacaoEndpoints();

app.Run();

public partial class Program;
