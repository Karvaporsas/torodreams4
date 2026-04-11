namespace ToroFitDreaming4.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = ExerciseCatalogDefaults.Category;
    public string BodyRegion { get; set; } = ExerciseCatalogDefaults.BodyRegion;
    public string MovementPattern { get; set; } = ExerciseCatalogDefaults.MovementPattern;
    public string PrimaryMuscleGroup { get; set; } = ExerciseCatalogDefaults.PrimaryMuscleGroup;
    public string? PrimaryEquipment { get; set; }
    public string? SecondaryEquipment { get; set; }
    public string DifficultyLevel { get; set; } = ExerciseCatalogDefaults.DifficultyLevel;
    public string TrainingStyle { get; set; } = ExerciseCatalogDefaults.TrainingStyle;
    public bool IsUnilateral { get; set; }
    public bool IsArchived { get; set; }
    public string SearchTerms { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ExerciseAlias> Aliases { get; set; } = [];
    public ICollection<ExerciseSecondaryMuscle> SecondaryMuscles { get; set; } = [];
}
