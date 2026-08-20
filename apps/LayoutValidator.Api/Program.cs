using LayoutValidator.Api.Regras;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogoDeRegras, CatalogoDeRegras>();
builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

app.Run();

public partial class Program;
