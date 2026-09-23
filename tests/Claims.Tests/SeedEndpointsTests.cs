using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Claims.Tests;

[TestClass]
public sealed class SeedEndpointsTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Beneficiarios_returns_seed_data()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/beneficiarios", TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        StringAssert.Contains(body, "Beneficiário Exemplo");
        StringAssert.Contains(body, "BEN-0001");
    }

    [TestMethod]
    public async Task Prestadores_returns_seed_data()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/prestadores", TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        StringAssert.Contains(body, "Clínica Exemplo");
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
