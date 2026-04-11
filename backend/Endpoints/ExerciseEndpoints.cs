using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Endpoints;

public static class ExerciseEndpoints
{
    public static void MapExerciseEndpoints(this WebApplication app)
    {
        // GET /api/exercises — any authenticated user
        app.MapGet("/api/exercises", async (AppDbContext db, bool includeArchived = false) =>
        {
            var query = db.Exercises
                .Include(e => e.Aliases)
                .Include(e => e.SecondaryMuscles)
                .AsQueryable();

            if (!includeArchived)
            {
                query = query.Where(e => !e.IsArchived);
            }

            var exercises = await query
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Results.Ok(exercises.Select(ToDto));
        });

        // POST /api/exercises — admin only
        app.MapPost("/api/exercises", [Authorize(Policy = "AdminOnly")] async (
            CreateExerciseRequest req,
            AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            var normalized = NormalizeRequest(req);
            var slugExists = await db.Exercises.AnyAsync(e => e.Slug == normalized.Slug);
            if (slugExists)
            {
                return Results.BadRequest(new { error = "Slug must be unique." });
            }

            var now = DateTime.UtcNow;
            var exercise = new Exercise
            {
                Slug = normalized.Slug,
                Name = normalized.Name,
                Description = normalized.Description,
                Category = normalized.Category,
                BodyRegion = normalized.BodyRegion,
                MovementPattern = normalized.MovementPattern,
                PrimaryMuscleGroup = normalized.PrimaryMuscleGroup,
                PrimaryEquipment = normalized.PrimaryEquipment,
                SecondaryEquipment = normalized.SecondaryEquipment,
                DifficultyLevel = normalized.DifficultyLevel,
                TrainingStyle = normalized.TrainingStyle,
                IsUnilateral = normalized.IsUnilateral,
                IsArchived = normalized.IsArchived,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Aliases = normalized.Aliases.Select(alias => new ExerciseAlias { Alias = alias }).ToList(),
                SecondaryMuscles = normalized.SecondaryMuscleGroups
                    .Select(muscle => new ExerciseSecondaryMuscle { MuscleGroup = muscle })
                    .ToList()
            };

            exercise.SearchTerms = BuildSearchTerms(exercise);

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/exercises/{exercise.Id}",
                ToDto(exercise));
        });

        // PUT /api/exercises/{id} — admin only
        app.MapPut("/api/exercises/{id:int}", [Authorize(Policy = "AdminOnly")] async (
            int id,
            UpdateExerciseRequest req,
            AppDbContext db) =>
        {
            var exercise = await db.Exercises
                .Include(e => e.Aliases)
                .Include(e => e.SecondaryMuscles)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise is null)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            var normalized = NormalizeRequest(req);
            var slugExists = await db.Exercises.AnyAsync(e => e.Id != id && e.Slug == normalized.Slug);
            if (slugExists)
            {
                return Results.BadRequest(new { error = "Slug must be unique." });
            }

            exercise.Slug = normalized.Slug;
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
            exercise.IsUnilateral = normalized.IsUnilateral;
            exercise.IsArchived = normalized.IsArchived;
            exercise.UpdatedAtUtc = DateTime.UtcNow;

            db.ExerciseAliases.RemoveRange(exercise.Aliases);
            db.ExerciseSecondaryMuscles.RemoveRange(exercise.SecondaryMuscles);
            exercise.Aliases = normalized.Aliases.Select(alias => new ExerciseAlias { Alias = alias }).ToList();
            exercise.SecondaryMuscles = normalized.SecondaryMuscleGroups
                .Select(muscle => new ExerciseSecondaryMuscle { MuscleGroup = muscle })
                .ToList();
            exercise.SearchTerms = BuildSearchTerms(exercise);

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(exercise));
        });

        // DELETE /api/exercises/{id} — admin only
        app.MapDelete("/api/exercises/{id:int}", [Authorize(Policy = "AdminOnly")] async (
            int id,
            AppDbContext db) =>
        {
            var exercise = await db.Exercises
                .Include(e => e.Aliases)
                .Include(e => e.SecondaryMuscles)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise is null)
            {
                return Results.NotFound();
            }

            var isReferenced = await db.WorkoutExercises.AnyAsync(we => we.ExerciseId == id);
            if (isReferenced)
            {
                exercise.IsArchived = true;
                exercise.UpdatedAtUtc = DateTime.UtcNow;
                exercise.SearchTerms = BuildSearchTerms(exercise);
                await db.SaveChangesAsync();

                return Results.Ok(new DeleteExerciseResult(
                    true,
                    "Exercise is used by workouts and was archived instead of deleted.",
                    ToDto(exercise)));
            }

            db.Exercises.Remove(exercise);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    private static ExerciseDto ToDto(Exercise exercise) =>
        new(
            exercise.Id,
            exercise.Slug,
            exercise.Name,
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
            exercise.SearchTerms,
            exercise.CreatedAtUtc,
            exercise.UpdatedAtUtc,
            exercise.Aliases
                .Select(alias => alias.Alias)
                .OrderBy(alias => alias)
                .ToList(),
            exercise.SecondaryMuscles
                .Select(muscle => muscle.MuscleGroup)
                .OrderBy(muscle => muscle)
                .ToList());

    private static NormalizedExerciseRequest NormalizeRequest(CreateExerciseRequest req) =>
        NormalizeRequest(
            req.Name,
            req.Slug,
            req.Description,
            req.Category,
            req.BodyRegion,
            req.MovementPattern,
            req.PrimaryMuscleGroup,
            req.PrimaryEquipment,
            req.SecondaryEquipment,
            req.DifficultyLevel,
            req.TrainingStyle,
            req.IsUnilateral,
            req.IsArchived,
            req.Aliases,
            req.SecondaryMuscleGroups);

    private static NormalizedExerciseRequest NormalizeRequest(UpdateExerciseRequest req) =>
        NormalizeRequest(
            req.Name,
            req.Slug,
            req.Description,
            req.Category,
            req.BodyRegion,
            req.MovementPattern,
            req.PrimaryMuscleGroup,
            req.PrimaryEquipment,
            req.SecondaryEquipment,
            req.DifficultyLevel,
            req.TrainingStyle,
            req.IsUnilateral,
            req.IsArchived,
            req.Aliases,
            req.SecondaryMuscleGroups);

    private static NormalizedExerciseRequest NormalizeRequest(
        string name,
        string? slug,
        string? description,
        string? category,
        string? bodyRegion,
        string? movementPattern,
        string? primaryMuscleGroup,
        string? primaryEquipment,
        string? secondaryEquipment,
        string? difficultyLevel,
        string? trainingStyle,
        bool isUnilateral,
        bool isArchived,
        string[]? aliases,
        string[]? secondaryMuscleGroups)
    {
        var normalizedName = name.Trim();
        var normalizedPrimaryMuscle = NormalizeRequired(primaryMuscleGroup, ExerciseCatalogDefaults.PrimaryMuscleGroup);
        var normalizedAliases = NormalizeValues(aliases)
            .Where(alias => !string.Equals(alias, normalizedName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var normalizedSecondaryMuscles = NormalizeValues(secondaryMuscleGroups)
            .Where(muscle => !string.Equals(muscle, normalizedPrimaryMuscle, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new NormalizedExerciseRequest(
            string.IsNullOrWhiteSpace(slug) ? Slugify(normalizedName) : Slugify(slug),
            normalizedName,
            NormalizeOptional(description),
            NormalizeRequired(category, ExerciseCatalogDefaults.Category),
            NormalizeRequired(bodyRegion, ExerciseCatalogDefaults.BodyRegion),
            NormalizeRequired(movementPattern, ExerciseCatalogDefaults.MovementPattern),
            normalizedPrimaryMuscle,
            NormalizeOptional(primaryEquipment),
            NormalizeOptional(secondaryEquipment),
            NormalizeRequired(difficultyLevel, ExerciseCatalogDefaults.DifficultyLevel),
            NormalizeRequired(trainingStyle, ExerciseCatalogDefaults.TrainingStyle),
            isUnilateral,
            isArchived,
            normalizedAliases,
            normalizedSecondaryMuscles);
    }

    private static List<string> NormalizeValues(IEnumerable<string>? values) =>
        values?
            .Select(NormalizeOptional)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList()
        ?? [];

    private static string NormalizeRequired(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
                continue;
            }

            if (builder.Length > 0 && !previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "exercise" : slug;
    }

    private static string BuildSearchTerms(Exercise exercise)
    {
        var values = new List<string?>
        {
            exercise.Name,
            exercise.Description,
            exercise.Category,
            exercise.BodyRegion,
            exercise.MovementPattern,
            exercise.PrimaryMuscleGroup,
            exercise.PrimaryEquipment,
            exercise.SecondaryEquipment,
            exercise.DifficultyLevel,
            exercise.TrainingStyle
        };

        values.AddRange(exercise.Aliases.Select(alias => alias.Alias));
        values.AddRange(exercise.SecondaryMuscles.Select(muscle => muscle.MuscleGroup));

        return string.Join(' ', values
            .Select(NormalizeOptional)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}

public record ExerciseDto(
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
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> SecondaryMuscleGroups);

public record DeleteExerciseResult(bool Archived, string Message, ExerciseDto Exercise);

public record CreateExerciseRequest(
    string Name,
    string? Description = null,
    string? Slug = null,
    string? Category = null,
    string? BodyRegion = null,
    string? MovementPattern = null,
    string? PrimaryMuscleGroup = null,
    string? PrimaryEquipment = null,
    string? SecondaryEquipment = null,
    string? DifficultyLevel = null,
    string? TrainingStyle = null,
    bool IsUnilateral = false,
    bool IsArchived = false,
    string[]? Aliases = null,
    string[]? SecondaryMuscleGroups = null);

public record UpdateExerciseRequest(
    string Name,
    string? Description = null,
    string? Slug = null,
    string? Category = null,
    string? BodyRegion = null,
    string? MovementPattern = null,
    string? PrimaryMuscleGroup = null,
    string? PrimaryEquipment = null,
    string? SecondaryEquipment = null,
    string? DifficultyLevel = null,
    string? TrainingStyle = null,
    bool IsUnilateral = false,
    bool IsArchived = false,
    string[]? Aliases = null,
    string[]? SecondaryMuscleGroups = null);

internal record NormalizedExerciseRequest(
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
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> SecondaryMuscleGroups);
