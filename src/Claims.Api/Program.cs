using Claims.Api.Data;
using Claims.Api.Features.Guias;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Usa a connection string configurada ou mantém o SQLite local em App_Data.
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var defaultConnectionString = $"Data Source={Path.Combine(dataDirectory, "claims.db")}";

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Claims") ?? defaultConnectionString));

// Habilita gerar documento OpenAPI.
builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migrations.
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await database.Database.MigrateAsync();

    // Aplica dados seed durante desenvolvimento.
    if (app.Environment.IsDevelopment())
    {
        await SeedData.InitializeAsync(database);
    }
}

// Disponíveis durante desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

#region Endpoints
app.MapGet("/beneficiarios", async (ClaimsDbContext db, CancellationToken ct) =>
Results.Ok(await db.Beneficiarios.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)));

app.MapGet("/prestadores", async (ClaimsDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Prestadores.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)));

app.MapGuiaEndpoints();
#endregion

app.Run();

/// <summary>
/// Liberado para testes de integração com WebApplicationFactory.
/// </summary>
public partial class Program { }
