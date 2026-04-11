using System.Net.Http.Headers;
using System.Text.Json;

namespace ToroFitDreaming4.Tests;

public class ExerciseAdminStatusTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ExerciseAdminStatusTests(TestFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminCatalogStatus_AfterReimport_ReturnsLastImportSummary()
    {
        var client = AdminClient();

        var reimportRes = await client.PostAsync("/api/admin/exercises/reimport", content: null);
        reimportRes.EnsureSuccessStatusCode();

        var status = JsonSerializer.Deserialize<ExerciseCatalogStatusDto>(
            await reimportRes.Content.ReadAsStringAsync(),
            JsonOpts);

        Assert.NotNull(status);
        Assert.True(status.CatalogFileExists);
        Assert.Equal("v1", status.CurrentCatalogVersion);
        Assert.NotNull(status.LastImport);
        Assert.Equal("Succeeded", status.LastImport.Status);
        Assert.Equal("admin", status.LastImport.TriggeredBy);
        Assert.True(status.LastImport.TotalExercises >= 150);

        var getStatusRes = await client.GetAsync("/api/admin/exercises/catalog-status");
        getStatusRes.EnsureSuccessStatusCode();

        var fetchedStatus = JsonSerializer.Deserialize<ExerciseCatalogStatusDto>(
            await getStatusRes.Content.ReadAsStringAsync(),
            JsonOpts);

        Assert.NotNull(fetchedStatus?.LastImport);
        Assert.Equal(status.LastImport.Id, fetchedStatus.LastImport.Id);
    }

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenHelper.GenerateToken("admin", "Admin"));
        return client;
    }

    private record ExerciseCatalogStatusDto(
        string CatalogPath,
        bool CatalogFileExists,
        string? CurrentCatalogVersion,
        string? CatalogMessage,
        ExerciseImportBatchDto? LastImport);

    private record ExerciseImportBatchDto(
        int Id,
        string? CatalogVersion,
        string CatalogPath,
        string Status,
        string TriggeredBy,
        int? TotalExercises,
        int? Created,
        int? Updated,
        int? Unchanged,
        string? Message,
        DateTime ImportedAtUtc);
}
