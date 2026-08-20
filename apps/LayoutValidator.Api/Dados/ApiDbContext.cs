using LayoutValidator.Api.Modelos;
using Microsoft.EntityFrameworkCore;

namespace LayoutValidator.Api.Dados;

public sealed class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> opcoes) : base(opcoes) { }

    public DbSet<LayoutCadastrado> Layouts => Set<LayoutCadastrado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LayoutCadastrado>(layout =>
        {
            layout.Property(l => l.Codigo).HasMaxLength(20).IsRequired();
            layout.HasIndex(l => l.Codigo).IsUnique();
            layout.Property(l => l.Nome).IsRequired();
            layout.Property(l => l.Delimitador).IsRequired();

            // FK obrigatória (int, não anulável): limpar Campos da coleção apaga os órfãos
            // automaticamente no SaveChanges, sem precisar de OnDelete explícito adicional.
            layout.HasMany(l => l.Campos)
                .WithOne()
                .HasForeignKey(c => c.LayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampoCadastrado>(campo =>
        {
            campo.Property(c => c.Nome).IsRequired();

            campo.HasMany(c => c.Regras)
                .WithOne()
                .HasForeignKey(r => r.CampoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegraCampoCadastrada>(regra =>
        {
            regra.Property(r => r.ChaveRegra).IsRequired();
        });
    }
}
