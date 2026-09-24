using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// MSTest libera execução em paralelo de métodos de teste da classe - Seguro pois usamos um TestApplicationFactory e cada uma usa um arquivo SQLite temporário exclusivo, sem disputa.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Claims.IntegrationTests;

internal sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    // Cada instância usa seu próprio arquivo para permitir execução paralela dos testes.
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"claims-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Força Development para aplicar migrations, carregar seed e expor o OpenAPI.
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Claims"] = $"Data Source={databasePath};Default Timeout=5" }));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Remove o banco e os arquivos auxiliares do SQLite ao final do teste.
        foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
        {
            try
            { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
        }
    }
}

[TestClass]
// Teste de API por HTTP usando banco SQLite isolado para cada cenário.
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

    [TestMethod]
    public async Task Criar_guia_returns_created_location_and_can_be_consulted()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        // A data coincide com o fim da carência do beneficiário de exemplo e deve ser aceita.
        using var response = await client.PostAsJsonAsync("/guias", ValidRequest("2026-02-01"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.AreEqual($"/guias/{id}", response.Headers.Location!.AbsolutePath);

        using var getResponse = await client.GetAsync(response.Headers.Location, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        using var result = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual(id, result.RootElement.GetProperty("id").GetGuid());
        Assert.AreEqual(1, await GetGuideCountAsync(factory));
    }

    [TestMethod]
    public async Task Criar_guia_rejects_dates_before_coverage_or_waiting_period()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        using var beforeCoverage = await client.PostAsJsonAsync("/guias", ValidRequest("2025-12-31"), TestContext.CancellationToken);
        using var beforeWaitingPeriodEnds = await client.PostAsJsonAsync("/guias", ValidRequest("2026-01-31"), TestContext.CancellationToken);

        await AssertProblemAsync(beforeCoverage, HttpStatusCode.Conflict, "coverage_not_started");
        await AssertProblemAsync(beforeWaitingPeriodEnds, HttpStatusCode.Conflict, "waiting_period");
        // Requisições rejeitadas não devem deixar guias gravadas.
        Assert.AreEqual(0, await GetGuideCountAsync(factory));
    }

    [TestMethod]
    public async Task Criar_guia_returns_not_found_for_missing_references()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        using var beneficiaryMissing = await client.PostAsJsonAsync("/guias", new { beneficiarioId = 999, prestadorId = 1, dataAtendimento = "2026-03-10" }, TestContext.CancellationToken);
        using var providerMissing = await client.PostAsJsonAsync("/guias", new { beneficiarioId = 1, prestadorId = 999, dataAtendimento = "2026-03-10" }, TestContext.CancellationToken);

        await AssertProblemAsync(beneficiaryMissing, HttpStatusCode.NotFound, "beneficiary_not_found");
        await AssertProblemAsync(providerMissing, HttpStatusCode.NotFound, "provider_not_found");
        Assert.AreEqual(0, await GetGuideCountAsync(factory));
    }

    [TestMethod]
    public async Task Criar_guia_rejects_inactive_beneficiary_and_provider()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        // Altera os dados de exemplo para preparar separadamente cada condição de conflito.
        await SetSeedStatusAsync(factory, beneficiaryActive: false, providerActive: true);
        using var beneficiaryInactive = await client.PostAsJsonAsync("/guias", ValidRequest("2026-03-10"), TestContext.CancellationToken);
        await AssertProblemAsync(beneficiaryInactive, HttpStatusCode.Conflict, "beneficiary_inactive");

        await SetSeedStatusAsync(factory, beneficiaryActive: true, providerActive: false);
        using var providerInactive = await client.PostAsJsonAsync("/guias", ValidRequest("2026-03-10"), TestContext.CancellationToken);
        await AssertProblemAsync(providerInactive, HttpStatusCode.Conflict, "provider_inactive");
        Assert.AreEqual(0, await GetGuideCountAsync(factory));
    }

    [TestMethod]
    public async Task Criar_guia_returns_bad_request_for_invalid_input()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        using var invalidId = await client.PostAsJsonAsync("/guias", new { beneficiarioId = 0, prestadorId = 1, dataAtendimento = "2026-03-10" }, TestContext.CancellationToken);
        using var invalidJson = await client.PostAsync("/guias", new StringContent("{\"beneficiarioId\":1,\"prestadorId\":1,\"dataAtendimento\":\"not-a-date\"}", System.Text.Encoding.UTF8, "application/json"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, invalidId.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidJson.StatusCode);
        Assert.AreEqual(0, await GetGuideCountAsync(factory));
    }

    [TestMethod]
    public async Task Consultar_guia_returns_not_found_when_id_does_not_exist()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/guias/{Guid.NewGuid()}", TestContext.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "guide_not_found");
    }

    private static object ValidRequest(string date) => new { beneficiarioId = 1, prestadorId = 1, dataAtendimento = date };

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        // Confere o status HTTP e o código estável enviado no corpo ProblemDetails.
        Assert.AreEqual(status, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(code, problem.RootElement.GetProperty("code").GetString());
    }

    private static async Task<int> GetGuideCountAsync(TestApplicationFactory factory)
    {
        // Verifica diretamente o banco para confirmar que uma falha não persistiu a guia.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Claims.Api.Data.ClaimsDbContext>();
        return await db.GuiasConsulta.CountAsync();
    }

    private static async Task SetSeedStatusAsync(TestApplicationFactory factory, bool beneficiaryActive, bool providerActive)
    {
        // Ajusta os registros seed sem adicionar endpoints administrativos só para os testes.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Claims.Api.Data.ClaimsDbContext>();
        var beneficiary = await db.Beneficiarios.SingleAsync(x => x.Id == 1);
        var provider = await db.Prestadores.SingleAsync(x => x.Id == 1);
        beneficiary.Ativo = beneficiaryActive;
        provider.CredenciadoAtivo = providerActive;
        await db.SaveChangesAsync();
    }
}
