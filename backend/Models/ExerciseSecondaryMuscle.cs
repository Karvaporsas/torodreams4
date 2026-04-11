namespace ToroFitDreaming4.Models;

public class ExerciseSecondaryMuscle
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string MuscleGroup { get; set; } = string.Empty;

    public Exercise Exercise { get; set; } = null!;
}
