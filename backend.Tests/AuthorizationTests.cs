using System.Net;
using System.Net.Http.Headers;

namespace ToroFitDreaming4.Tests;

/// <summary>
/// Covers Task 7.4: authorization and ownership checks.
/// - Unauthenticated requests → 401
/// - Regular user on admin-only endpoints → 403
/// - User accessing another user's workout resources → 403
/// </summary>
public class AuthorizationTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;

    public AuthorizationTests(TestFactory factory) => _factory = factory;

    private HttpClient ClientFor(string username, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenHelper.GenerateToken(username, roles));
        return client;
    }

    // ── Unauthenticated ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetExercises_WithoutToken_Returns401()
    {
        var res = await _factory.CreateClient().GetAsync("/api/exercises");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetWorkouts_WithoutToken_Returns401()
    {
        var res = await _factory.CreateClient().GetAsync("/api/workouts");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── Non-admin on admin endpoints → 403 ────────────────────────────────────

    [Fact]
    public async Task PostExercise_AsRegularUser_Returns403()
    {
        var res = await ClientFor("alice")
            .PostAsJsonAsync("/api/exercises", new { name = "Squat" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task PutExercise_AsRegularUser_Returns403()
    {
        var res = await ClientFor("alice")
            .PutAsJsonAsync($"/api/exercises/{_factory.ExerciseId}", new { name = "Updated" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteExercise_AsRegularUser_Returns403()
    {
        var res = await ClientFor("alice")
            .DeleteAsync($"/api/exercises/{_factory.ExerciseId}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── Cross-user workout ownership → 403 ────────────────────────────────────

    [Fact]
    public async Task GetWorkout_OwnedByOtherUser_Returns403()
    {
        // Alice tries to read Bob's workout
        var res = await ClientFor("alice")
            .GetAsync($"/api/workouts/{_factory.BobWorkoutId}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AddExerciseToWorkout_OwnedByOtherUser_Returns403()
    {
        var res = await ClientFor("alice")
            .PostAsJsonAsync(
                $"/api/workouts/{_factory.BobWorkoutId}/exercises",
                new { exerciseId = _factory.ExerciseId });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CompleteWorkout_OwnedByOtherUser_Returns403()
    {
        var req = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/workouts/{_factory.BobWorkoutId}/complete");
        var res = await ClientFor("alice").SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
