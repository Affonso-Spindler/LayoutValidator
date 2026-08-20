using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LayoutValidator.Api.Dados;

public sealed class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite("Data Source=layoutvalidator.db")
            .Options;

        return new ApiDbContext(opcoes);
    }
}
