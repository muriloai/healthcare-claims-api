using Claims.Api.Domain;
using Microsoft.EntityFrameworkCore;
namespace Claims.Api.Data;

internal sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<Prestador> Prestadores => Set<Prestador>();
    public DbSet<GuiaConsulta> GuiasConsulta => Set<GuiaConsulta>();
    public DbSet<Lote> Lotes => Set<Lote>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity
            <Beneficiario>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Carteirinha).IsUnique();
            e.Property(x => x.NomeFicticio).HasMaxLength(160).IsRequired();
            e.Property(x => x.Carteirinha).HasMaxLength(40).IsRequired();
            e.Property(x => x.InicioVigencia).HasConversion<string>();
            e.Property(x => x.FimCarencia).HasConversion<string>();
        });
        modelBuilder.Entity
            <Prestador>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeFicticio).HasMaxLength(160).IsRequired();
        });
        modelBuilder.Entity
            <GuiaConsulta>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DataAtendimento).HasConversion<string>();
            e.HasOne<Beneficiario>().WithMany().HasForeignKey(x => x.BeneficiarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Prestador>().WithMany().HasForeignKey(x => x.PrestadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Lote>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity
            <Lote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
        });
    }
}
