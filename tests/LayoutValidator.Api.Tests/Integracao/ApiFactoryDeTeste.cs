using LayoutValidator.Api.Dados;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LayoutValidator.Api.Tests.Integracao;

public sealed class ApiFactoryDeTeste : WebApplicationFactory<Program>
{
    private readonly string _arquivoDeBanco = Path.Combine(Path.GetTempPath(), $"layoutvalidator-teste-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApiDbContext>>();
            services.AddDbContext<ApiDbContext>(opcoes => opcoes.UseSqlite($"Data Source={_arquivoDeBanco}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // No Windows, o Microsoft.Data.Sqlite faz pooling de conexões: mesmo depois do
        // WebApplicationFactory ser descartado, o handle do arquivo pode continuar aberto
        // e o File.Delete abaixo falharia com IOException. Limpar o pool libera o handle.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_arquivoDeBanco))
            File.Delete(_arquivoDeBanco);
    }
}
