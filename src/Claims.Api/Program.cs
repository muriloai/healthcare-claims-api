using Claims.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ClaimsDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Claims") ?? "Data Source=claims.db"));
builder.Services.AddOpenApi();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await database.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await SeedData.InitializeAsync(database);
}

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/beneficiarios", async (ClaimsDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Beneficiarios.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)));

app.MapGet("/prestadores", async (ClaimsDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Prestadores.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)));
app.Run();
public partial class Program { }
