namespace ToroFitDreaming4.Models;

public class Workout
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
