using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // UserRole: composite PK (UserId + Role)
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.Role });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Workout → User
        modelBuilder.Entity<Workout>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkoutExercise → Workout (cascade)
        modelBuilder.Entity<WorkoutExercise>()
            .HasOne(we => we.Workout)
            .WithMany(w => w.WorkoutExercises)
            .HasForeignKey(we => we.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkoutExercise → Exercise (restrict — don't delete exercises when workout is deleted)
        modelBuilder.Entity<WorkoutExercise>()
            .HasOne(we => we.Exercise)
            .WithMany()
            .HasForeignKey(we => we.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkoutSet → WorkoutExercise (cascade)
        modelBuilder.Entity<WorkoutSet>()
            .HasOne(ws => ws.WorkoutExercise)
            .WithMany(we => we.Sets)
            .HasForeignKey(ws => ws.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSet>()
            .Property(ws => ws.WeightKg)
            .HasColumnType("decimal(6,2)");
    }
}
