namespace ToroFitDreaming4.Models;

public class WorkoutSet
{
    public int Id { get; set; }
    public int WorkoutExerciseId { get; set; }
    public int SetNumber { get; set; }
    public decimal WeightKg { get; set; }
    public int Reps { get; set; }
    public bool IsDone { get; set; }

    public WorkoutExercise WorkoutExercise { get; set; } = null!;
}
