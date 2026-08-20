using LayoutValidator.Api.Dados;
using LayoutValidator.Api.Modelos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Tests.Dados;

public class ApiDbContextTestes : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly ApiDbContext _db;

    public ApiDbContextTestes()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var opcoes = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(_conexao).Options;
        _db = new ApiDbContext(opcoes);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexao.Dispose();
    }

    [Fact]
    public async Task SalvaLayoutComCamposERegrasAninhados()
    {
        _db.Layouts.Add(new LayoutCadastrado
        {
            Codigo = "PESSOA1",
            Nome = "Pessoa",
            Delimitador = ";",
            Campos =
            {
                new CampoCadastrado
                {
                    Nome = "Cpf",
                    Ordem = 0,
                    Regras = { new RegraCampoCadastrada { ChaveRegra = "Cpf", Ordem = 0 } }
                }
            }
        });

        await _db.SaveChangesAsync();

        var salvo = await _db.Layouts
            .Include(l => l.Campos).ThenInclude(c => c.Regras)
            .FirstAsync(l => l.Codigo == "PESSOA1");

        Assert.Single(salvo.Campos);
        Assert.Single(salvo.Campos[0].Regras);
        Assert.Equal("Cpf", salvo.Campos[0].Regras[0].ChaveRegra);
    }

    [Fact]
    public async Task Codigo_NaoAceitaDuplicado()
    {
        _db.Layouts.Add(new LayoutCadastrado { Codigo = "DUP1", Nome = "A", Delimitador = ";" });
        await _db.SaveChangesAsync();

        _db.Layouts.Add(new LayoutCadastrado { Codigo = "DUP1", Nome = "B", Delimitador = ";" });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task LimparColecaoDeCampos_ApagaOsCamposOrfaosAoSalvar()
    {
        var layout = new LayoutCadastrado
        {
            Codigo = "ORFAO1",
            Nome = "Teste",
            Delimitador = ";",
            Campos = { new CampoCadastrado { Nome = "X", Ordem = 0 } }
        };
        _db.Layouts.Add(layout);
        await _db.SaveChangesAsync();

        layout.Campos.Clear();
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.Set<CampoCadastrado>().ToListAsync());
    }
}
