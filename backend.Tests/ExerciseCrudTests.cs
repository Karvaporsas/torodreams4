using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ToroFitDreaming4.Tests;

/// <summary>
/// Covers Task 7.1: admin can create, view, edit, and delete exercises via the API.
/// </summary>
public class ExerciseCrudTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ExerciseCrudTests(TestFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenHelper.GenerateToken("admin", "Admin"));
        return client;
    }

    [Fact]
    public async Task GetExercises_AsAdmin_ReturnsSeededExercise()
    {
        var res = await AdminClient().GetAsync("/api/exercises");
        res.EnsureSuccessStatusCode();

        var list = JsonSerializer.Deserialize<List<ExerciseDto>>(
            await res.Content.ReadAsStringAsync(), JsonOpts);

        Assert.NotNull(list);
        Assert.Contains(list, e => e.Name == "Bench Press");
    }

    [Fact]
    public async Task AdminExerciseCrud_CreateUpdateDelete_Succeeds()
    {
        var client = AdminClient();

        // Create
        var createRes = await client.PostAsJsonAsync(
            "/api/exercises",
            new { name = "Pull-up", description = "Upper-body pull" });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = JsonSerializer.Deserialize<ExerciseDto>(
            await createRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(created);
        Assert.Equal("Pull-up", created.Name);
        Assert.Equal("Upper-body pull", created.Description);

        // Update
        var updateRes = await client.PutAsJsonAsync(
            $"/api/exercises/{created.Id}",
            new { name = "Chin-up", description = "Upper-body pull (supinated)" });
        updateRes.EnsureSuccessStatusCode();

        var updated = JsonSerializer.Deserialize<ExerciseDto>(
            await updateRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(updated);
        Assert.Equal("Chin-up", updated.Name);

        // Delete
        var deleteRes = await client.DeleteAsync($"/api/exercises/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Verify gone
        var allRes = await client.GetAsync("/api/exercises");
        var all = JsonSerializer.Deserialize<List<ExerciseDto>>(
            await allRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.DoesNotContain(all!, e => e.Id == created.Id);
    }

    private record ExerciseDto(int Id, string Name, string? Description);
}
