using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseAlias> ExerciseAliases => Set<ExerciseAlias>();
    public DbSet<ExerciseSecondaryMuscle> ExerciseSecondaryMuscles => Set<ExerciseSecondaryMuscle>();
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

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.Slug)
            .IsUnique();

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => new { e.IsArchived, e.Name });

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.Category);

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.BodyRegion);

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.MovementPattern);

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.PrimaryMuscleGroup);

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.PrimaryEquipment);

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.TrainingStyle);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.Slug)
            .HasMaxLength(200);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.Description)
            .HasMaxLength(1000);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.Category)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.BodyRegion)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.MovementPattern)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.PrimaryMuscleGroup)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.PrimaryEquipment)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.SecondaryEquipment)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.DifficultyLevel)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.TrainingStyle)
            .HasMaxLength(100);

        modelBuilder.Entity<Exercise>()
            .Property(e => e.SearchTerms)
            .HasMaxLength(2000);

        modelBuilder.Entity<ExerciseAlias>()
            .HasIndex(a => new { a.ExerciseId, a.Alias })
            .IsUnique();

        modelBuilder.Entity<ExerciseAlias>()
            .Property(a => a.Alias)
            .HasMaxLength(200);

        modelBuilder.Entity<ExerciseAlias>()
            .HasOne(a => a.Exercise)
            .WithMany(e => e.Aliases)
            .HasForeignKey(a => a.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseSecondaryMuscle>()
            .HasIndex(m => new { m.ExerciseId, m.MuscleGroup })
            .IsUnique();

        modelBuilder.Entity<ExerciseSecondaryMuscle>()
            .Property(m => m.MuscleGroup)
            .HasMaxLength(100);

        modelBuilder.Entity<ExerciseSecondaryMuscle>()
            .HasOne(m => m.Exercise)
            .WithMany(e => e.SecondaryMuscles)
            .HasForeignKey(m => m.ExerciseId)
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
