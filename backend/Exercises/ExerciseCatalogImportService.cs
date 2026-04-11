using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Exercises;

public sealed class ExerciseCatalogImportService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public async Task<ExerciseCatalogImportResult> ImportFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var catalog = await JsonSerializer.DeserializeAsync<ExerciseCatalogFile>(stream, JsonOptions, cancellationToken)
            ?? throw new ExerciseCatalogValidationException("Catalog file is empty or invalid JSON.");

        return await ImportAsync(catalog, cancellationToken);
    }

    public async Task<ExerciseCatalogImportResult> ImportAsync(ExerciseCatalogFile catalog, CancellationToken cancellationToken = default)
    {
        ExerciseCatalogValidator.Validate(catalog);

        var now = DateTime.UtcNow;
            var slugs = catalog.Exercises
                .Select(item => ExerciseCatalogMapper.Normalize(
                item.Name,
                item.Slug,
                item.Description,
                item.Category,
                item.BodyRegion,
                item.MovementPattern,
                item.PrimaryMuscleGroup,
                item.PrimaryEquipment,
                item.SecondaryEquipment,
                item.DifficultyLevel,
                item.TrainingStyle,
                item.IsUnilateral,
                item.IsArchived,
                item.Aliases ?? [],
                item.SecondaryMuscleGroups ?? []).Slug)
            .ToList();

        var existing = await db.Exercises
            .Include(e => e.Aliases)
            .Include(e => e.SecondaryMuscles)
            .Where(e => slugs.Contains(e.Slug))
            .ToDictionaryAsync(e => e.Slug, cancellationToken);

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var item in catalog.Exercises)
        {
            var normalized = ExerciseCatalogMapper.Normalize(
                item.Name,
                item.Slug,
                item.Description,
                item.Category,
                item.BodyRegion,
                item.MovementPattern,
                item.PrimaryMuscleGroup,
                item.PrimaryEquipment,
                item.SecondaryEquipment,
                item.DifficultyLevel,
                item.TrainingStyle,
                item.IsUnilateral,
                item.IsArchived,
                item.Aliases ?? [],
                item.SecondaryMuscleGroups ?? []);

            if (!existing.TryGetValue(normalized.Slug, out var exercise))
            {
                exercise = new Exercise();
                ExerciseCatalogMapper.ApplyToExercise(exercise, normalized, now, isNew: true);
                db.Exercises.Add(exercise);
                existing[normalized.Slug] = exercise;
                created++;
                continue;
            }

            if (!HasChanges(exercise, normalized))
            {
                unchanged++;
                continue;
            }

            db.ExerciseAliases.RemoveRange(exercise.Aliases);
            db.ExerciseSecondaryMuscles.RemoveRange(exercise.SecondaryMuscles);
            ExerciseCatalogMapper.ApplyToExercise(exercise, normalized, now, isNew: false);
            updated++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ExerciseCatalogImportResult(
            catalog.Version,
            catalog.Exercises.Count,
            created,
            updated,
            unchanged);
    }

    private static bool HasChanges(Exercise exercise, NormalizedExerciseRequest normalized)
    {
        if (!string.Equals(exercise.Name, normalized.Name, StringComparison.Ordinal) ||
            !string.Equals(exercise.Slug, normalized.Slug, StringComparison.Ordinal) ||
            !string.Equals(exercise.Description, normalized.Description, StringComparison.Ordinal) ||
            !string.Equals(exercise.Category, normalized.Category, StringComparison.Ordinal) ||
            !string.Equals(exercise.BodyRegion, normalized.BodyRegion, StringComparison.Ordinal) ||
            !string.Equals(exercise.MovementPattern, normalized.MovementPattern, StringComparison.Ordinal) ||
            !string.Equals(exercise.PrimaryMuscleGroup, normalized.PrimaryMuscleGroup, StringComparison.Ordinal) ||
            !string.Equals(exercise.PrimaryEquipment, normalized.PrimaryEquipment, StringComparison.Ordinal) ||
            !string.Equals(exercise.SecondaryEquipment, normalized.SecondaryEquipment, StringComparison.Ordinal) ||
            !string.Equals(exercise.DifficultyLevel, normalized.DifficultyLevel, StringComparison.Ordinal) ||
            !string.Equals(exercise.TrainingStyle, normalized.TrainingStyle, StringComparison.Ordinal) ||
            exercise.IsUnilateral != normalized.IsUnilateral ||
            exercise.IsArchived != normalized.IsArchived)
        {
            return true;
        }

        var currentAliases = exercise.Aliases.Select(alias => alias.Alias).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var nextAliases = normalized.Aliases.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!currentAliases.SequenceEqual(nextAliases, StringComparer.Ordinal))
        {
            return true;
        }

        var currentSecondaryMuscles = exercise.SecondaryMuscles.Select(muscle => muscle.MuscleGroup).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var nextSecondaryMuscles = normalized.SecondaryMuscleGroups.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        return !currentSecondaryMuscles.SequenceEqual(nextSecondaryMuscles, StringComparer.Ordinal);
    }
}

public sealed record ExerciseCatalogImportResult(
    string Version,
    int TotalExercises,
    int Created,
    int Updated,
    int Unchanged);
