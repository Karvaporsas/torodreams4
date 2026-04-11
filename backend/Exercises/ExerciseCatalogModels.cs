namespace ToroFitDreaming4.Exercises;

public sealed record ExerciseCatalogFile(string Version, IReadOnlyList<ExerciseCatalogItem> Exercises);

public sealed record ExerciseCatalogItem(
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
    string[] Aliases,
    string[] SecondaryMuscleGroups);
