using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
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

            var referenceCounts = await GetWorkoutReferenceCountsAsync(
                db,
                exercises.Select(exercise => exercise.Id).ToArray());

            return Results.Ok(new ExerciseSearchResponse(
                exercises.Select(exercise => ToDto(exercise, referenceCounts.GetValueOrDefault(exercise.Id))).ToList(),
                page,
                pageSize,
                totalCount,
                (page * pageSize) < totalCount));
        });

        app.MapGet("/api/admin/exercises/catalog-status", [Authorize(Policy = "AdminOnly")] async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var adminService = new ExerciseCatalogAdminService(db);
            var status = await adminService.GetStatusAsync(cancellationToken);
            return Results.Ok(status);
        });

        app.MapPost("/api/admin/exercises/reimport", [Authorize(Policy = "AdminOnly")] async (
            HttpContext httpContext,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var adminService = new ExerciseCatalogAdminService(db);
            var catalogPath = adminService.GetDefaultCatalogPath();
            var triggeredBy = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? "admin";

            if (!File.Exists(catalogPath))
            {
                await adminService.RecordFailureAsync(
                    catalogPath,
                    null,
                    triggeredBy,
                    "Catalog file was not found.",
                    cancellationToken);

                return Results.Problem(detail: "Catalog file was not found.");
            }

            try
            {
                var importer = new ExerciseCatalogImportService(db);
                var result = await importer.ImportFromFileAsync(catalogPath, cancellationToken);
                await adminService.RecordSuccessAsync(catalogPath, triggeredBy, result, cancellationToken);

                return Results.Ok(await adminService.GetStatusAsync(cancellationToken));
            }
            catch (ExerciseCatalogValidationException ex)
            {
                db.ChangeTracker.Clear();

                await adminService.RecordFailureAsync(
                    catalogPath,
                    await adminService.TryReadCatalogVersionAsync(catalogPath, cancellationToken),
                    triggeredBy,
                    ex.Message,
                    cancellationToken);

                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                db.ChangeTracker.Clear();

                await adminService.RecordFailureAsync(
                    catalogPath,
                    await adminService.TryReadCatalogVersionAsync(catalogPath, cancellationToken),
                    triggeredBy,
                    ex.Message,
                    cancellationToken);

                return Results.Problem(detail: ex.Message);
            }
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
            if (await NameExistsAsync(db, normalized.Name))
            {
                return Results.BadRequest(new { error = "Exercise name must be unique." });
            }

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
                ToDto(exercise, 0));
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
            if (await NameExistsAsync(db, normalized.Name, id))
            {
                return Results.BadRequest(new { error = "Exercise name must be unique." });
            }

            var slugExists = await db.Exercises.AnyAsync(e => e.Id != id && e.Slug == normalized.Slug);
            if (slugExists)
            {
                return Results.BadRequest(new { error = "Slug must be unique." });
            }

            db.ExerciseAliases.RemoveRange(exercise.Aliases);
            db.ExerciseSecondaryMuscles.RemoveRange(exercise.SecondaryMuscles);
            ExerciseCatalogMapper.ApplyToExercise(exercise, normalized, DateTime.UtcNow, isNew: false);

            await db.SaveChangesAsync();

            var referenceCount = await db.WorkoutExercises.CountAsync(we => we.ExerciseId == id);
            return Results.Ok(ToDto(exercise, referenceCount));
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

            var referenceCount = await db.WorkoutExercises.CountAsync(we => we.ExerciseId == id);
            if (referenceCount > 0)
            {
                exercise.IsArchived = true;
                exercise.UpdatedAtUtc = DateTime.UtcNow;
                exercise.SearchTerms = ExerciseCatalogMapper.BuildSearchTerms(exercise);
                await db.SaveChangesAsync();

                return Results.Ok(new DeleteExerciseResult(
                    true,
                    "Exercise is used by workouts and was archived instead of deleted.",
                    ToDto(exercise, referenceCount)));
            }

            db.Exercises.Remove(exercise);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    private static async Task<bool> NameExistsAsync(AppDbContext db, string name, int? excludedId = null)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var query = db.Exercises.Where(exercise => exercise.Name.ToLower() == normalizedName);

        if (excludedId.HasValue)
        {
            query = query.Where(exercise => exercise.Id != excludedId.Value);
        }

        return await query.AnyAsync();
    }

    private static async Task<Dictionary<int, int>> GetWorkoutReferenceCountsAsync(
        AppDbContext db,
        IReadOnlyCollection<int> exerciseIds)
    {
        if (exerciseIds.Count == 0)
        {
            return [];
        }

        return await db.WorkoutExercises
            .Where(workoutExercise => exerciseIds.Contains(workoutExercise.ExerciseId))
            .GroupBy(workoutExercise => workoutExercise.ExerciseId)
            .Select(group => new
            {
                ExerciseId = group.Key,
                ReferenceCount = group.Count()
            })
            .ToDictionaryAsync(item => item.ExerciseId, item => item.ReferenceCount);
    }

    private static ExerciseDto ToDto(Exercise exercise, int workoutReferenceCount) =>
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
            workoutReferenceCount > 0,
            workoutReferenceCount,
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
    bool IsReferencedByWorkouts,
    int WorkoutReferenceCount,
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
