using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Exercises;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Endpoints;

public static class ExerciseEndpoints
{
    public static void MapExerciseEndpoints(this WebApplication app)
    {
        // GET /api/exercises — any authenticated user
        app.MapGet("/api/exercises", async (
            AppDbContext db,
            string? search = null,
            string? category = null,
            string? movementPattern = null,
            string? equipment = null,
            string? bodyRegion = null,
            string? trainingStyle = null,
            string? sort = null,
            bool includeArchived = false,
            int page = 1,
            int pageSize = 250) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 250);

            IQueryable<Exercise> query = db.Exercises.AsNoTracking();

            if (!includeArchived)
            {
                query = query.Where(e => !e.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(normalizedSearch) ||
                    (e.Description != null && e.Description.ToLower().Contains(normalizedSearch)) ||
                    e.SearchTerms.Contains(normalizedSearch));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var normalizedCategory = category.Trim().ToLowerInvariant();
                query = query.Where(e => e.Category.ToLower() == normalizedCategory);
            }

            if (!string.IsNullOrWhiteSpace(movementPattern))
            {
                var normalizedMovementPattern = movementPattern.Trim().ToLowerInvariant();
                query = query.Where(e => e.MovementPattern.ToLower() == normalizedMovementPattern);
            }

            if (!string.IsNullOrWhiteSpace(equipment))
            {
                var normalizedEquipment = equipment.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    (e.PrimaryEquipment != null && e.PrimaryEquipment.ToLower() == normalizedEquipment) ||
                    (e.SecondaryEquipment != null && e.SecondaryEquipment.ToLower() == normalizedEquipment));
            }

            if (!string.IsNullOrWhiteSpace(bodyRegion))
            {
                var normalizedBodyRegion = bodyRegion.Trim().ToLowerInvariant();
                query = query.Where(e => e.BodyRegion.ToLower() == normalizedBodyRegion);
            }

            if (!string.IsNullOrWhiteSpace(trainingStyle))
            {
                var normalizedTrainingStyle = trainingStyle.Trim().ToLowerInvariant();
                query = query.Where(e => e.TrainingStyle.ToLower() == normalizedTrainingStyle);
            }

            var totalCount = await query.CountAsync();

            query = sort?.Trim().ToLowerInvariant() switch
            {
                "name-desc" => query.OrderByDescending(e => e.Name).ThenBy(e => e.Id),
                "updated" => query.OrderBy(e => e.UpdatedAtUtc).ThenBy(e => e.Name),
                "updated-desc" => query.OrderByDescending(e => e.UpdatedAtUtc).ThenBy(e => e.Name),
                _ => query.OrderBy(e => e.Name).ThenBy(e => e.Id)
            };

            var exercises = await query
                .Include(e => e.Aliases)
                .Include(e => e.SecondaryMuscles)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new ExerciseSearchResponse(
                exercises.Select(ToDto).ToList(),
                page,
                pageSize,
                totalCount,
                (page * pageSize) < totalCount));
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
            var exercise = new Exercise();
            ExerciseCatalogMapper.ApplyToExercise(exercise, normalized, now, isNew: true);

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

            db.ExerciseAliases.RemoveRange(exercise.Aliases);
            db.ExerciseSecondaryMuscles.RemoveRange(exercise.SecondaryMuscles);
            ExerciseCatalogMapper.ApplyToExercise(exercise, normalized, DateTime.UtcNow, isNew: false);

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
                exercise.SearchTerms = ExerciseCatalogMapper.BuildSearchTerms(exercise);
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
        ExerciseCatalogMapper.Normalize(
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
        ExerciseCatalogMapper.Normalize(
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

public record ExerciseSearchResponse(
    IReadOnlyList<ExerciseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);

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
