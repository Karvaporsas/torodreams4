using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Tests;

/// <summary>
/// Shared test server factory. Creates an in-memory database, seeds test data,
/// and exposes seeded entity IDs for use in tests.
/// </summary>
public class TestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtSecret = "test-secret-key-must-be-at-least-32-characters!";
    public const string TestJwtIssuer = "ToroFitDreaming4";
    public const string TestJwtAudience = "ToroFitDreaming4";

    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    // Seeded entity IDs accessible to test classes
    public int AliceId { get; private set; }
    public int BobId { get; private set; }
    public int AdminUserId { get; private set; }
    public int ExerciseId { get; private set; }
    public int BobWorkoutId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Register in-memory DbContext with a unique name per fixture instance
        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed users
        var alice = new User { Username = "alice", PasswordHash = BCrypt.Net.BCrypt.HashPassword("alice123") };
        var bob = new User { Username = "bob", PasswordHash = BCrypt.Net.BCrypt.HashPassword("bob123") };
        var admin = new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123") };
        db.Users.AddRange(alice, bob, admin);
        await db.SaveChangesAsync();

        AliceId = alice.Id;
        BobId = bob.Id;
        AdminUserId = admin.Id;

        // Give admin the Admin role
        db.UserRoles.Add(new UserRole { UserId = AdminUserId, Role = "Admin" });

        // Seed one exercise
        var exercise = new Exercise { Name = "Bench Press", Description = "Chest press on a flat bench" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        ExerciseId = exercise.Id;

        // Seed a completed workout owned by Bob (used in ownership/authorization tests)
        var bobWorkout = new Workout
        {
            UserId = BobId,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            DurationSeconds = 3600,
        };
        db.Workouts.Add(bobWorkout);
        await db.SaveChangesAsync();

        BobWorkoutId = bobWorkout.Id;
    }

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();
}
