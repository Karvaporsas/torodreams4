using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Exercises;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Tests;

public class ExerciseBackfillTests
{
    [Fact]
    public async Task BackfillAsync_FillsLegacyMetadataAndGeneratesUniqueSlugs()
    {
        await using var db = CreateDb();
        db.Exercises.AddRange(
            new Exercise
            {
                Name = " Bench Press ",
                Description = " Flat barbell press ",
                Slug = "",
                Category = "",
                BodyRegion = "",
                MovementPattern = "",
                PrimaryMuscleGroup = "",
                DifficultyLevel = "",
                TrainingStyle = "",
                SearchTerms = "",
                CreatedAtUtc = default,
                UpdatedAtUtc = default
            },
            new Exercise
            {
                Name = "Bench Press",
                Slug = "",
                Category = "",
                BodyRegion = "",
                MovementPattern = "",
                PrimaryMuscleGroup = "",
                DifficultyLevel = "",
                TrainingStyle = "",
                SearchTerms = ""
            });

        await db.SaveChangesAsync();

        var service = new ExerciseCatalogBackfillService(db);
        var result = await service.BackfillAsync();

        Assert.Equal(2, result.TotalExercises);
        Assert.Equal(2, result.Updated);
        Assert.Equal(0, result.Unchanged);

        var exercises = await db.Exercises
            .OrderBy(exercise => exercise.Id)
            .ToListAsync();

        Assert.Equal("Bench Press", exercises[0].Name);
        Assert.Equal("bench-press", exercises[0].Slug);
        Assert.Equal($"bench-press-{exercises[1].Id}", exercises[1].Slug);
        Assert.Equal(ExerciseCatalogDefaults.Category, exercises[0].Category);
        Assert.Equal(ExerciseCatalogDefaults.BodyRegion, exercises[0].BodyRegion);
        Assert.Equal(ExerciseCatalogDefaults.MovementPattern, exercises[0].MovementPattern);
        Assert.Equal(ExerciseCatalogDefaults.PrimaryMuscleGroup, exercises[0].PrimaryMuscleGroup);
        Assert.Equal(ExerciseCatalogDefaults.DifficultyLevel, exercises[0].DifficultyLevel);
        Assert.Equal(ExerciseCatalogDefaults.TrainingStyle, exercises[0].TrainingStyle);
        Assert.Contains("bench press", exercises[0].SearchTerms);
        Assert.Contains("flat barbell press", exercises[0].SearchTerms);
        Assert.NotEqual(default, exercises[0].CreatedAtUtc);
        Assert.True(exercises[0].UpdatedAtUtc >= exercises[0].CreatedAtUtc);
    }

    [Fact]
    public async Task BackfillAsync_LeavesCompleteExerciseUnchanged()
    {
        await using var db = CreateDb();
        var createdAtUtc = DateTime.UtcNow.AddDays(-2);
        var updatedAtUtc = DateTime.UtcNow.AddDays(-1);

        db.Exercises.Add(new Exercise
        {
            Name = "Pull-Up",
            Description = "Strict vertical pull",
            Slug = "pull-up",
            Category = "Pull",
            BodyRegion = "Upper Body",
            MovementPattern = "Vertical Pull",
            PrimaryMuscleGroup = "Back",
            PrimaryEquipment = "Bodyweight",
            DifficultyLevel = "Intermediate",
            TrainingStyle = "Strength",
            SearchTerms = "pull-up strict vertical pull pull upper body vertical pull back bodyweight intermediate strength",
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        });

        await db.SaveChangesAsync();

        var service = new ExerciseCatalogBackfillService(db);
        var result = await service.BackfillAsync();

        Assert.Equal(0, result.Updated);
        Assert.Equal(1, result.Unchanged);

        var exercise = await db.Exercises.SingleAsync();
        Assert.Equal("pull-up", exercise.Slug);
        Assert.Equal(createdAtUtc, exercise.CreatedAtUtc);
        Assert.Equal(updatedAtUtc, exercise.UpdatedAtUtc);
        Assert.Equal("pull-up strict vertical pull pull upper body vertical pull back bodyweight intermediate strength", exercise.SearchTerms);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ExerciseBackfillTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
