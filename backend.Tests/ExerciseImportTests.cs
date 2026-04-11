using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Exercises;

namespace ToroFitDreaming4.Tests;

public class ExerciseImportTests
{
    [Fact]
    public async Task ImportAsync_IsIdempotentAndUpdatesBySlug()
    {
        await using var db = CreateDb();
        var importer = new ExerciseCatalogImportService(db);

        var original = new ExerciseCatalogFile(
            "v1",
            [
                new ExerciseCatalogItem(
                    "bench-press",
                    "Bench Press",
                    "Flat barbell press",
                    "Chest",
                    "Upper Body",
                    "Horizontal Push",
                    "Chest",
                    "Barbell",
                    null,
                    "Intermediate",
                    "Strength",
                    false,
                    false,
                    ["Flat Bench"],
                    ["Triceps"])
            ]);

        var first = await importer.ImportAsync(original);
        Assert.Equal(1, first.Created);
        Assert.Equal(0, first.Updated);

        var second = await importer.ImportAsync(original);
        Assert.Equal(0, second.Created);
        Assert.Equal(0, second.Updated);
        Assert.Equal(1, second.Unchanged);

        var updatedCatalog = original with
        {
            Version = "v2",
            Exercises =
            [
                original.Exercises[0] with
                {
                    Description = "Competition-style flat barbell press",
                    SecondaryMuscleGroups = ["Front Delts", "Triceps"]
                }
            ]
        };

        var third = await importer.ImportAsync(updatedCatalog);
        Assert.Equal(0, third.Created);
        Assert.Equal(1, third.Updated);

        var exercise = await db.Exercises
            .Include(e => e.SecondaryMuscles)
            .SingleAsync(e => e.Slug == "bench-press");

        Assert.Equal("Competition-style flat barbell press", exercise.Description);
        Assert.Equal(["Front Delts", "Triceps"], exercise.SecondaryMuscles.Select(m => m.MuscleGroup).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task ImportAsync_WithDuplicateSlug_ThrowsValidationException()
    {
        await using var db = CreateDb();
        var importer = new ExerciseCatalogImportService(db);
        var catalog = new ExerciseCatalogFile(
            "v1",
            [
                CreateItem("Bench Press", "bench-press"),
                CreateItem("Bench Press Variation", "bench-press")
            ]);

        var ex = await Assert.ThrowsAsync<ExerciseCatalogValidationException>(() => importer.ImportAsync(catalog));
        Assert.Contains("duplicate slug", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportAsync_WithMalformedAlias_ThrowsValidationException()
    {
        await using var db = CreateDb();
        var importer = new ExerciseCatalogImportService(db);
        var catalog = new ExerciseCatalogFile(
            "v1",
            [
                CreateItem("Pull-Up", "pull-up") with
                {
                    Aliases = ["Pull Up", " "]
                }
            ]);

        var ex = await Assert.ThrowsAsync<ExerciseCatalogValidationException>(() => importer.ImportAsync(catalog));
        Assert.Contains("alias entries", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DefaultCatalogFile_ImportsSuccessfully()
    {
        await using var db = CreateDb();
        var importer = new ExerciseCatalogImportService(db);
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "backend", "SeedData", "exercise-catalog.v1.json"));

        var result = await importer.ImportFromFileAsync(path);

        Assert.Equal("v1", result.Version);
        Assert.True(result.TotalExercises >= 150);
        Assert.True(result.Created >= 150);
        Assert.True(await db.Exercises.AnyAsync(e => e.Slug == "bench-press"));
        Assert.True(await db.Exercises.AnyAsync(e => e.Slug == "assault-bike-sprint"));
    }

    private static ExerciseCatalogItem CreateItem(string name, string slug) =>
        new(
            slug,
            name,
            null,
            "Chest",
            "Upper Body",
            "Horizontal Push",
            "Chest",
            "Barbell",
            null,
            "Intermediate",
            "Strength",
            false,
            false,
            [],
            []);

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ExerciseImportTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
