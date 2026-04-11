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

        var list = JsonSerializer.Deserialize<ExerciseSearchResponse>(
            await res.Content.ReadAsStringAsync(), JsonOpts);

        Assert.NotNull(list);
        Assert.Contains(list.Items, e => e.Name == "Bench Press");
    }

    [Fact]
    public async Task AdminExerciseCrud_CreateUpdateDelete_Succeeds()
    {
        var client = AdminClient();

        // Create
        var createRes = await client.PostAsJsonAsync(
            "/api/exercises",
            new
            {
                name = "Pull-up",
                slug = "pull-up",
                description = "Upper-body pull",
                category = "Pull",
                bodyRegion = "Upper Body",
                movementPattern = "Vertical Pull",
                primaryMuscleGroup = "Back",
                primaryEquipment = "Bodyweight",
                secondaryEquipment = "Pull-Up Bar",
                difficultyLevel = "Intermediate",
                trainingStyle = "Strength",
                isUnilateral = false,
                aliases = new[] { "Pull Up", "Strict Pull-up" },
                secondaryMuscleGroups = new[] { "Biceps", "Forearms" }
            });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = JsonSerializer.Deserialize<ExerciseDto>(
            await createRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(created);
        Assert.Equal("Pull-up", created.Name);
        Assert.Equal("pull-up", created.Slug);
        Assert.Equal("Upper-body pull", created.Description);
        Assert.Equal("Pull", created.Category);
        Assert.Contains("Biceps", created.SecondaryMuscleGroups);

        // Update
        var updateRes = await client.PutAsJsonAsync(
            $"/api/exercises/{created.Id}",
            new
            {
                name = "Chin-up",
                slug = "chin-up",
                description = "Upper-body pull (supinated)",
                category = "Pull",
                bodyRegion = "Upper Body",
                movementPattern = "Vertical Pull",
                primaryMuscleGroup = "Back",
                primaryEquipment = "Bodyweight",
                secondaryEquipment = "Pull-Up Bar",
                difficultyLevel = "Intermediate",
                trainingStyle = "Strength",
                isUnilateral = false,
                isArchived = false,
                aliases = new[] { "Chin Up" },
                secondaryMuscleGroups = new[] { "Biceps" }
            });
        updateRes.EnsureSuccessStatusCode();

        var updated = JsonSerializer.Deserialize<ExerciseDto>(
            await updateRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(updated);
        Assert.Equal("Chin-up", updated.Name);
        Assert.Equal("chin-up", updated.Slug);
        Assert.Single(updated.Aliases);

        // Delete
        var deleteRes = await client.DeleteAsync($"/api/exercises/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Verify gone
        var allRes = await client.GetAsync("/api/exercises");
        var all = JsonSerializer.Deserialize<ExerciseSearchResponse>(
            await allRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.DoesNotContain(all!.Items, e => e.Id == created.Id);
    }

    [Fact]
    public async Task DeleteExercise_WhenReferenced_ArchivesInstead()
    {
        var client = AdminClient();

        var workoutRes = await ClientFor("alice").PostAsync("/api/workouts", content: null);
        workoutRes.EnsureSuccessStatusCode();
        var workout = JsonSerializer.Deserialize<WorkoutSummaryDto>(
            await workoutRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(workout);

        var addExerciseRes = await ClientFor("alice").PostAsJsonAsync(
            $"/api/workouts/{workout!.Id}/exercises",
            new { exerciseId = _factory.ExerciseId });
        addExerciseRes.EnsureSuccessStatusCode();

        var deleteRes = await client.DeleteAsync($"/api/exercises/{_factory.ExerciseId}");
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);

        var archived = JsonSerializer.Deserialize<DeleteExerciseResult>(
            await deleteRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.NotNull(archived);
        Assert.True(archived.Archived);
        Assert.True(archived.Exercise.IsArchived);

        var activeListRes = await client.GetAsync("/api/exercises");
        activeListRes.EnsureSuccessStatusCode();
        var activeList = JsonSerializer.Deserialize<ExerciseSearchResponse>(
            await activeListRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.DoesNotContain(activeList!.Items, e => e.Id == _factory.ExerciseId);

        var fullListRes = await client.GetAsync("/api/exercises?includeArchived=true");
        fullListRes.EnsureSuccessStatusCode();
        var fullList = JsonSerializer.Deserialize<ExerciseSearchResponse>(
            await fullListRes.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Contains(fullList!.Items, e => e.Id == _factory.ExerciseId && e.IsArchived);
    }

    [Fact]
    public async Task GetExercises_WithSearchFiltersAndPaging_ReturnsMatchingPage()
    {
        var client = AdminClient();

        await client.PostAsJsonAsync("/api/exercises", new
        {
            name = "Cable Row",
            slug = "cable-row",
            category = "Back",
            bodyRegion = "Upper Body",
            movementPattern = "Horizontal Pull",
            primaryMuscleGroup = "Back",
            primaryEquipment = "Cable",
            difficultyLevel = "Beginner",
            trainingStyle = "Hypertrophy",
            aliases = new[] { "Seated Row" },
            secondaryMuscleGroups = new[] { "Biceps" }
        });

        await client.PostAsJsonAsync("/api/exercises", new
        {
            name = "Cable Fly",
            slug = "cable-fly",
            category = "Chest",
            bodyRegion = "Upper Body",
            movementPattern = "Horizontal Push",
            primaryMuscleGroup = "Chest",
            primaryEquipment = "Cable",
            difficultyLevel = "Beginner",
            trainingStyle = "Hypertrophy"
        });

        var res = await client.GetAsync("/api/exercises?search=row&equipment=Cable&movementPattern=Horizontal Pull&page=1&pageSize=1");
        res.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<ExerciseSearchResponse>(
            await res.Content.ReadAsStringAsync(), JsonOpts);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.True(result.TotalCount >= 1);
        Assert.Single(result.Items);
        Assert.Equal("Cable Row", result.Items[0].Name);
        Assert.False(result.HasMore);
    }

    private HttpClient ClientFor(string username, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenHelper.GenerateToken(username, roles));
        return client;
    }

    private record ExerciseDto(
        int Id,
        string Slug,
        string Name,
        string? Description,
        string Category,
        string BodyRegion,
        string MovementPattern,
        string PrimaryMuscleGroup,
        string? PrimaryEquipment,
        string? SecondaryEquipment,
        string DifficultyLevel,
        string TrainingStyle,
        bool IsUnilateral,
        bool IsArchived,
        string SearchTerms,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        List<string> Aliases,
        List<string> SecondaryMuscleGroups);

    private record ExerciseSearchResponse(
        List<ExerciseDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        bool HasMore);

    private record DeleteExerciseResult(bool Archived, string Message, ExerciseDto Exercise);
    private record WorkoutSummaryDto(int Id, DateTime StartedAt, DateTime? CompletedAt, int? DurationSeconds, int ExerciseCount);
}
