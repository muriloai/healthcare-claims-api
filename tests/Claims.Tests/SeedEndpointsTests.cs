using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Claims.Tests;

public sealed class SeedEndpointsTests : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient client;
    public SeedEndpointsTests(TestApplicationFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task Beneficiarios_returns_seed_data()
    {
        var response = await client.GetAsync("/beneficiarios", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Beneficiário Exemplo", body);
        Assert.Contains("BEN-0001", body);
    }

    [Fact]
    public async Task Prestadores_returns_seed_data()
    {
        var response = await client.GetAsync("/prestadores", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Contains("Clínica Exemplo", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"claims-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Claims"] = $"Data Source={databasePath};Default Timeout=5" }));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
        {
            try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
        }
    }
}
