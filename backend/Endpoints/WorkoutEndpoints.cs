using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Endpoints;

public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this WebApplication app)
    {
        // POST /api/workouts — start a new workout
        app.MapPost("/api/workouts", async (HttpContext ctx, AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = new Workout
            {
                UserId = userId.Value,
                StartedAt = DateTime.UtcNow
            };

            db.Workouts.Add(workout);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workouts/{workout.Id}",
                ToSummaryDto(workout));
        });

        // GET /api/workouts — list authenticated user's workouts, newest first
        app.MapGet("/api/workouts", async (HttpContext ctx, AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workouts = await db.Workouts
                .Where(w => w.UserId == userId.Value)
                .OrderByDescending(w => w.StartedAt)
                .Include(w => w.WorkoutExercises)
                .ToListAsync();

            return Results.Ok(workouts.Select(w => new WorkoutSummaryDto(
                w.Id,
                w.StartedAt,
                w.CompletedAt,
                w.DurationSeconds,
                w.WorkoutExercises.Count)));
        });

        // GET /api/workouts/{id} — full workout detail
        app.MapGet("/api/workouts/{id:int}", async (int id, HttpContext ctx, AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = await db.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout is null) return Results.NotFound();
            if (workout.UserId != userId.Value) return Results.Forbid();

            return Results.Ok(ToDetailDto(workout));
        });

        // POST /api/workouts/{id}/exercises — add exercise to workout
        app.MapPost("/api/workouts/{id:int}/exercises", async (
            int id,
            AddExerciseRequest req,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = await db.Workouts.FindAsync(id);
            if (workout is null) return Results.NotFound();
            if (workout.UserId != userId.Value) return Results.Forbid();

            var exerciseExists = await db.Exercises.AnyAsync(e => e.Id == req.ExerciseId && !e.IsArchived);
            if (!exerciseExists)
                return Results.BadRequest(new { error = "Exercise not found." });

            var order = req.Order ?? await db.WorkoutExercises
                .Where(we => we.WorkoutId == id)
                .CountAsync() + 1;

            var we = new WorkoutExercise
            {
                WorkoutId = id,
                ExerciseId = req.ExerciseId,
                Order = order
            };

            db.WorkoutExercises.Add(we);
            await db.SaveChangesAsync();

            await db.Entry(we).Reference(x => x.Exercise).LoadAsync();

            return Results.Created(
                $"/api/workouts/{id}/exercises/{we.Id}",
                new WorkoutExerciseDto(we.Id, we.ExerciseId, we.Exercise.Name, we.Order, []));
        });

        // DELETE /api/workouts/{id}/exercises/{workoutExerciseId}
        app.MapDelete("/api/workouts/{id:int}/exercises/{workoutExerciseId:int}", async (
            int id,
            int workoutExerciseId,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = await db.Workouts.FindAsync(id);
            if (workout is null) return Results.NotFound();
            if (workout.UserId != userId.Value) return Results.Forbid();

            var we = await db.WorkoutExercises
                .FirstOrDefaultAsync(x => x.Id == workoutExerciseId && x.WorkoutId == id);
            if (we is null) return Results.NotFound();

            db.WorkoutExercises.Remove(we);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // POST /api/workouts/{id}/exercises/{workoutExerciseId}/sets — add set
        app.MapPost("/api/workouts/{id:int}/exercises/{workoutExerciseId:int}/sets", async (
            int id,
            int workoutExerciseId,
            AddSetRequest req,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = await db.Workouts.FindAsync(id);
            if (workout is null) return Results.NotFound();
            if (workout.UserId != userId.Value) return Results.Forbid();

            var we = await db.WorkoutExercises
                .FirstOrDefaultAsync(x => x.Id == workoutExerciseId && x.WorkoutId == id);
            if (we is null) return Results.NotFound();

            var nextSetNumber = await db.WorkoutSets
                .Where(s => s.WorkoutExerciseId == workoutExerciseId)
                .MaxAsync(s => (int?)s.SetNumber) ?? 0;
            nextSetNumber++;

            var set = new WorkoutSet
            {
                WorkoutExerciseId = workoutExerciseId,
                SetNumber = nextSetNumber,
                WeightKg = req.WeightKg,
                Reps = req.Reps,
                IsDone = false
            };

            db.WorkoutSets.Add(set);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workouts/sets/{set.Id}",
                ToSetDto(set));
        });

        // PATCH /api/workouts/sets/{setId} — update a set
        app.MapMethods("/api/workouts/sets/{setId:int}", ["PATCH"], async (
            int setId,
            UpdateSetRequest req,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var set = await db.WorkoutSets
                .Include(s => s.WorkoutExercise)
                    .ThenInclude(we => we.Workout)
                .FirstOrDefaultAsync(s => s.Id == setId);

            if (set is null) return Results.NotFound();
            if (set.WorkoutExercise.Workout.UserId != userId.Value) return Results.Forbid();

            if (req.WeightKg.HasValue) set.WeightKg = req.WeightKg.Value;
            if (req.Reps.HasValue) set.Reps = req.Reps.Value;
            if (req.IsDone.HasValue) set.IsDone = req.IsDone.Value;

            await db.SaveChangesAsync();

            return Results.Ok(ToSetDto(set));
        });

        // DELETE /api/workouts/sets/{setId}
        app.MapDelete("/api/workouts/sets/{setId:int}", async (
            int setId,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var set = await db.WorkoutSets
                .Include(s => s.WorkoutExercise)
                    .ThenInclude(we => we.Workout)
                .FirstOrDefaultAsync(s => s.Id == setId);

            if (set is null) return Results.NotFound();
            if (set.WorkoutExercise.Workout.UserId != userId.Value) return Results.Forbid();

            db.WorkoutSets.Remove(set);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // PATCH /api/workouts/{id}/complete
        app.MapMethods("/api/workouts/{id:int}/complete", ["PATCH"], async (
            int id,
            HttpContext ctx,
            AppDbContext db) =>
        {
            var userId = await GetUserIdAsync(ctx, db);
            if (userId is null) return Results.Unauthorized();

            var workout = await db.Workouts.FindAsync(id);
            if (workout is null) return Results.NotFound();
            if (workout.UserId != userId.Value) return Results.Forbid();

            var now = DateTime.UtcNow;
            workout.CompletedAt = now;
            workout.DurationSeconds = (int)(now - workout.StartedAt).TotalSeconds;

            await db.SaveChangesAsync();

            return Results.Ok(ToSummaryDto(workout));
        });
    }

    private static async Task<int?> GetUserIdAsync(HttpContext ctx, AppDbContext db)
    {
        var username = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (username is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.Id;
    }

    private static WorkoutSummaryDto ToSummaryDto(Workout w) =>
        new(w.Id, w.StartedAt, w.CompletedAt, w.DurationSeconds, 0);

    private static WorkoutDetailDto ToDetailDto(Workout w) =>
        new(
            w.Id,
            w.StartedAt,
            w.CompletedAt,
            w.DurationSeconds,
            w.WorkoutExercises
                .OrderBy(we => we.Order)
                .Select(we => new WorkoutExerciseDto(
                    we.Id,
                    we.ExerciseId,
                    we.Exercise.Name,
                    we.Order,
                    we.Sets.OrderBy(s => s.SetNumber).Select(ToSetDto).ToList()))
                .ToList());

    private static WorkoutSetDto ToSetDto(WorkoutSet s) =>
        new(s.Id, s.SetNumber, s.WeightKg, s.Reps, s.IsDone);
}

public record WorkoutSummaryDto(
    int Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int? DurationSeconds,
    int ExerciseCount);

public record WorkoutDetailDto(
    int Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int? DurationSeconds,
    IList<WorkoutExerciseDto> Exercises);

public record WorkoutExerciseDto(
    int Id,
    int ExerciseId,
    string ExerciseName,
    int Order,
    IList<WorkoutSetDto> Sets);

public record WorkoutSetDto(
    int Id,
    int SetNumber,
    decimal WeightKg,
    int Reps,
    bool IsDone);

public record AddExerciseRequest(int ExerciseId, int? Order);
public record AddSetRequest(decimal WeightKg, int Reps);
public record UpdateSetRequest(decimal? WeightKg, int? Reps, bool? IsDone);
