using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ToroFitDreaming4.Tests;

/// <summary>
/// Covers Tasks 7.2 and 7.3: full workout lifecycle — start, add exercise,
/// add set, check set done, complete — then verify it appears in history.
/// All steps run in a single test to maintain sequential state.
/// </summary>
public class WorkoutFlowTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WorkoutFlowTests(TestFactory factory) => _factory = factory;

    private HttpClient AliceClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenHelper.GenerateToken("alice"));
        return client;
    }

    private static async Task<T> ReadJson<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(json, JsonOpts);
        Assert.NotNull(result);
        return result!;
    }

    private static HttpRequestMessage PatchJson(string url, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, url);
        req.Content = JsonContent.Create(body);
        return req;
    }

    // Task 7.2: Start workout, add exercise, add set, toggle done, complete.
    // Task 7.3: Confirm completed workout appears in history with correct fields.
    [Fact]
    public async Task WorkoutLifecycle_FullFlow_Succeeds()
    {
        var client = AliceClient();

        // ── Task 7.2: Start a new workout ─────────────────────────────────────
        var startRes = await client.PostAsync("/api/workouts", null);
        Assert.Equal(HttpStatusCode.Created, startRes.StatusCode);

        var workout = await ReadJson<WorkoutSummaryDto>(startRes);
        Assert.True(workout.Id > 0);
        Assert.Null(workout.CompletedAt);

        // ── Task 7.2: Add an exercise ─────────────────────────────────────────
        var addExRes = await client.PostAsJsonAsync(
            $"/api/workouts/{workout.Id}/exercises",
            new { exerciseId = _factory.ExerciseId });
        Assert.Equal(HttpStatusCode.Created, addExRes.StatusCode);

        var we = await ReadJson<WorkoutExerciseDto>(addExRes);
        Assert.Equal("Bench Press", we.ExerciseName);

        // ── Task 7.2: Add a set (80 kg × 10 reps) ────────────────────────────
        var addSetRes = await client.PostAsJsonAsync(
            $"/api/workouts/{workout.Id}/exercises/{we.Id}/sets",
            new { weightKg = 80.0, reps = 10 });
        Assert.Equal(HttpStatusCode.Created, addSetRes.StatusCode);

        var set = await ReadJson<WorkoutSetDto>(addSetRes);
        Assert.Equal(80.0m, set.WeightKg);
        Assert.Equal(10, set.Reps);
        Assert.False(set.IsDone);

        // ── Task 7.2: Check individual set as done ────────────────────────────
        var toggleRes = await client.SendAsync(
            PatchJson($"/api/workouts/sets/{set.Id}", new { isDone = true }));
        toggleRes.EnsureSuccessStatusCode();

        var updatedSet = await ReadJson<WorkoutSetDto>(toggleRes);
        Assert.True(updatedSet.IsDone);

        // ── Task 7.2: Verify live timer is running (startedAt in the past) ────
        var detailRes = await client.GetAsync($"/api/workouts/{workout.Id}");
        detailRes.EnsureSuccessStatusCode();
        var detail = await ReadJson<WorkoutDetailDto>(detailRes);
        Assert.True(DateTime.Parse(detail.StartedAt).ToUniversalTime() <= DateTime.UtcNow);

        // ── Task 7.2: Complete the workout ────────────────────────────────────
        var completeRes = await client.SendAsync(
            PatchJson($"/api/workouts/{workout.Id}/complete", new { }));
        completeRes.EnsureSuccessStatusCode();

        var completed = await ReadJson<WorkoutSummaryDto>(completeRes);
        Assert.NotNull(completed.CompletedAt);
        Assert.NotNull(completed.DurationSeconds);
        Assert.True(completed.DurationSeconds >= 0, "DurationSeconds should be non-negative");

        // ── Task 7.3: Completed workout appears in history ────────────────────
        var historyRes = await client.GetAsync("/api/workouts");
        historyRes.EnsureSuccessStatusCode();

        var history = await ReadJson<List<WorkoutSummaryDto>>(historyRes);
        var found = history.FirstOrDefault(w => w.Id == workout.Id);

        Assert.NotNull(found);
        Assert.NotNull(found!.CompletedAt);
        Assert.NotNull(found.DurationSeconds);
        Assert.Equal(1, found.ExerciseCount);
    }

    [Fact]
    public async Task GetWorkouts_ReturnsOnlyOwnWorkouts()
    {
        // Alice's list should NOT contain Bob's workout (BobWorkoutId is seeded)
        var res = await AliceClient().GetAsync("/api/workouts");
        res.EnsureSuccessStatusCode();

        var history = await ReadJson<List<WorkoutSummaryDto>>(res);
        Assert.DoesNotContain(history, w => w.Id == _factory.BobWorkoutId);
    }

    [Fact]
    public async Task DeleteSet_RemovesSetFromWorkout()
    {
        var client = AliceClient();

        // Create a workout, add exercise, add set, then delete it
        var w = await ReadJson<WorkoutSummaryDto>(
            await client.PostAsync("/api/workouts", null));

        var we = await ReadJson<WorkoutExerciseDto>(
            await client.PostAsJsonAsync(
                $"/api/workouts/{w.Id}/exercises",
                new { exerciseId = _factory.ExerciseId }));

        var set = await ReadJson<WorkoutSetDto>(
            await client.PostAsJsonAsync(
                $"/api/workouts/{w.Id}/exercises/{we.Id}/sets",
                new { weightKg = 60.0, reps = 8 }));

        var deleteRes = await client.DeleteAsync($"/api/workouts/sets/{set.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Verify set is gone from workout detail
        var detail = await ReadJson<WorkoutDetailDto>(
            await client.GetAsync($"/api/workouts/{w.Id}"));
        var exercise = detail.Exercises.First(e => e.Id == we.Id);
        Assert.DoesNotContain(exercise.Sets, s => s.Id == set.Id);
    }

    private record WorkoutSummaryDto(int Id, string StartedAt, string? CompletedAt, int? DurationSeconds, int ExerciseCount);
    private record WorkoutDetailDto(int Id, string StartedAt, string? CompletedAt, int? DurationSeconds, List<WorkoutExerciseDto> Exercises);
    private record WorkoutExerciseDto(int Id, int ExerciseId, string ExerciseName, int Order, List<WorkoutSetDto> Sets);
    private record WorkoutSetDto(int Id, int SetNumber, decimal WeightKg, int Reps, bool IsDone);
}
