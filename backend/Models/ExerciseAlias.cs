namespace ToroFitDreaming4.Models;

public class ExerciseAlias
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string Alias { get; set; } = string.Empty;

    public Exercise Exercise { get; set; } = null!;
}
