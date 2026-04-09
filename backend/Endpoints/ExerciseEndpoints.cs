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
        app.MapGet("/api/exercises", async (AppDbContext db) =>
        {
            var exercises = await db.Exercises
                .OrderBy(e => e.Name)
                .Select(e => new ExerciseDto(e.Id, e.Name, e.Description))
                .ToListAsync();

            return Results.Ok(exercises);
        });

        // POST /api/exercises — admin only
        app.MapPost("/api/exercises", [Authorize(Policy = "AdminOnly")] async (
            CreateExerciseRequest req,
            AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });

            var exercise = new Exercise
            {
                Name = req.Name.Trim(),
                Description = req.Description?.Trim()
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/exercises/{exercise.Id}",
                new ExerciseDto(exercise.Id, exercise.Name, exercise.Description));
        });

        // PUT /api/exercises/{id} — admin only
        app.MapPut("/api/exercises/{id:int}", [Authorize(Policy = "AdminOnly")] async (
            int id,
            UpdateExerciseRequest req,
            AppDbContext db) =>
        {
            var exercise = await db.Exercises.FindAsync(id);
            if (exercise is null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });

            exercise.Name = req.Name.Trim();
            exercise.Description = req.Description?.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new ExerciseDto(exercise.Id, exercise.Name, exercise.Description));
        });

        // DELETE /api/exercises/{id} — admin only
        app.MapDelete("/api/exercises/{id:int}", [Authorize(Policy = "AdminOnly")] async (
            int id,
            AppDbContext db) =>
        {
            var exercise = await db.Exercises.FindAsync(id);
            if (exercise is null)
                return Results.NotFound();

            db.Exercises.Remove(exercise);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}

public record ExerciseDto(int Id, string Name, string? Description);
public record CreateExerciseRequest(string Name, string? Description);
public record UpdateExerciseRequest(string Name, string? Description);
