using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Exercises;

public sealed class ExerciseCatalogBackfillService(AppDbContext db)
{
    public async Task<ExerciseCatalogBackfillResult> BackfillAsync(CancellationToken cancellationToken = default)
    {
        var exercises = await db.Exercises
            .Include(exercise => exercise.Aliases)
            .Include(exercise => exercise.SecondaryMuscles)
            .OrderBy(exercise => exercise.Id)
            .ToListAsync(cancellationToken);

        var usedSlugs = exercises
            .Select(exercise => exercise.Slug?.Trim())
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Select(slug => slug!)
            .ToHashSet(StringComparer.Ordinal);

        var updated = 0;
        var unchanged = 0;
        var now = DateTime.UtcNow;

        foreach (var exercise in exercises)
        {
            var normalized = ExerciseCatalogMapper.Normalize(
                exercise.Name,
                null,
                exercise.Description,
                exercise.Category,
                exercise.BodyRegion,
                exercise.MovementPattern,
                exercise.PrimaryMuscleGroup,
                exercise.PrimaryEquipment,
                exercise.SecondaryEquipment,
                exercise.DifficultyLevel,
                exercise.TrainingStyle,
                exercise.IsUnilateral,
                exercise.IsArchived,
                exercise.Aliases.Select(alias => alias.Alias).ToArray(),
                exercise.SecondaryMuscles.Select(muscle => muscle.MuscleGroup).ToArray());

            var createdAtUtc = exercise.CreatedAtUtc == default ? now : exercise.CreatedAtUtc;
            var updatedAtUtc = exercise.UpdatedAtUtc == default
                ? createdAtUtc
                : exercise.UpdatedAtUtc < createdAtUtc
                    ? createdAtUtc
                    : exercise.UpdatedAtUtc;

            var resolvedSlug = ResolveSlug(exercise, normalized.Name, usedSlugs);

            var changed =
                !string.Equals(exercise.Name, normalized.Name, StringComparison.Ordinal) ||
                !string.Equals(exercise.Description, normalized.Description, StringComparison.Ordinal) ||
                !string.Equals(exercise.Category, normalized.Category, StringComparison.Ordinal) ||
                !string.Equals(exercise.BodyRegion, normalized.BodyRegion, StringComparison.Ordinal) ||
                !string.Equals(exercise.MovementPattern, normalized.MovementPattern, StringComparison.Ordinal) ||
                !string.Equals(exercise.PrimaryMuscleGroup, normalized.PrimaryMuscleGroup, StringComparison.Ordinal) ||
                !string.Equals(exercise.PrimaryEquipment, normalized.PrimaryEquipment, StringComparison.Ordinal) ||
                !string.Equals(exercise.SecondaryEquipment, normalized.SecondaryEquipment, StringComparison.Ordinal) ||
                !string.Equals(exercise.DifficultyLevel, normalized.DifficultyLevel, StringComparison.Ordinal) ||
                !string.Equals(exercise.TrainingStyle, normalized.TrainingStyle, StringComparison.Ordinal) ||
                !string.Equals(exercise.Slug, resolvedSlug, StringComparison.Ordinal) ||
                exercise.CreatedAtUtc != createdAtUtc ||
                exercise.UpdatedAtUtc != updatedAtUtc;

            exercise.Name = normalized.Name;
            exercise.Description = normalized.Description;
            exercise.Category = normalized.Category;
            exercise.BodyRegion = normalized.BodyRegion;
            exercise.MovementPattern = normalized.MovementPattern;
            exercise.PrimaryMuscleGroup = normalized.PrimaryMuscleGroup;
            exercise.PrimaryEquipment = normalized.PrimaryEquipment;
            exercise.SecondaryEquipment = normalized.SecondaryEquipment;
            exercise.DifficultyLevel = normalized.DifficultyLevel;
            exercise.TrainingStyle = normalized.TrainingStyle;
            exercise.Slug = resolvedSlug;
            exercise.CreatedAtUtc = createdAtUtc;
            exercise.UpdatedAtUtc = updatedAtUtc;

            var rebuiltSearchTerms = ExerciseCatalogMapper.BuildSearchTerms(exercise);
            if (!string.Equals(exercise.SearchTerms, rebuiltSearchTerms, StringComparison.Ordinal))
            {
                exercise.SearchTerms = rebuiltSearchTerms;
                changed = true;
            }

            if (changed)
            {
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new ExerciseCatalogBackfillResult(exercises.Count, updated, unchanged);
    }

    private static string ResolveSlug(Exercise exercise, string normalizedName, HashSet<string> usedSlugs)
    {
        var existingSlug = exercise.Slug?.Trim();
        if (!string.IsNullOrWhiteSpace(existingSlug))
        {
            return existingSlug;
        }

        var baseSlug = ExerciseCatalogMapper.Slugify(normalizedName);
        var candidate = baseSlug;

        if (usedSlugs.Contains(candidate))
        {
            candidate = $"{baseSlug}-{exercise.Id}";
        }

        while (!usedSlugs.Add(candidate))
        {
            candidate = $"{baseSlug}-{exercise.Id}-{usedSlugs.Count + 1}";
        }

        return candidate;
    }
}

public sealed record ExerciseCatalogBackfillResult(
    int TotalExercises,
    int Updated,
    int Unchanged);
