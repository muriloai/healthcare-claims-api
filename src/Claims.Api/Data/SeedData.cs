using Claims.Api.Domain;
using Microsoft.EntityFrameworkCore;
namespace Claims.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ClaimsDbContext db, CancellationToken ct = default)
    {
        if (!await db.Beneficiarios.AnyAsync(ct))
            db.Beneficiarios.Add(
                new Beneficiario
                {
                    Id = 1,
                    NomeFicticio = "Beneficiário Exemplo",
                    Carteirinha = "BEN-0001",
                    Ativo = true,
                    InicioVigencia = new DateOnly(2026, 1, 1),
                    FimCarencia = new DateOnly(2026, 2, 1)
                });
        if (!await db.Prestadores.AnyAsync(ct))
            db.Prestadores.Add(
                new Prestador
                {
                    Id = 1,
                    NomeFicticio = "Clínica Exemplo",
                    CredenciadoAtivo = true
                });
        await db.SaveChangesAsync(ct);
    }
}
