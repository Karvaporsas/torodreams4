namespace ToroFitDreaming4.Exercises;

public static class ExerciseCatalogValidator
{
    private static readonly HashSet<string> ValidCategories =
    [
        "Arms", "Back", "Carry", "Chest", "Conditioning", "Core", "Hinge", "Kettlebell",
        "Locomotion", "Lower Body Isolation", "Lunge", "Olympic Lift", "Plyometric",
        "Shoulders", "Squat"
    ];

    private static readonly HashSet<string> ValidBodyRegions =
    [
        "Upper Body", "Lower Body", "Full Body", "Core"
    ];

    private static readonly HashSet<string> ValidMovementPatterns =
    [
        "Carry", "Conditioning", "Core Flexion", "Core Stability", "Hinge", "Horizontal Pull",
        "Horizontal Push", "Locomotion", "Lunge", "Olympic Lift", "Plyometric", "Rotation",
        "Squat", "Vertical Pull", "Vertical Push"
    ];

    private static readonly HashSet<string> ValidDifficultyLevels =
    [
        "Beginner", "Intermediate", "Advanced"
    ];

    private static readonly HashSet<string> ValidTrainingStyles =
    [
        "Strength", "Hypertrophy", "Power", "Athletic", "Conditioning", "Mobility"
    ];

    public static void Validate(ExerciseCatalogFile catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.Version))
        {
            throw new ExerciseCatalogValidationException("Catalog version is required.");
        }

        if (catalog.Exercises is null || catalog.Exercises.Count == 0)
        {
            throw new ExerciseCatalogValidationException("Catalog must include at least one exercise.");
        }

        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < catalog.Exercises.Count; index++)
        {
            var item = catalog.Exercises[index];
            var label = $"Exercise #{index + 1} ({item.Name})";

            EnsureRequired(item.Name, $"{label}: name is required.");
            EnsureRequired(item.Slug, $"{label}: slug is required.");
            EnsureRequired(item.Category, $"{label}: category is required.");
            EnsureRequired(item.BodyRegion, $"{label}: body region is required.");
            EnsureRequired(item.MovementPattern, $"{label}: movement pattern is required.");
            EnsureRequired(item.PrimaryMuscleGroup, $"{label}: primary muscle group is required.");
            EnsureRequired(item.DifficultyLevel, $"{label}: difficulty level is required.");
            EnsureRequired(item.TrainingStyle, $"{label}: training style is required.");

            var normalized = ExerciseCatalogMapper.Normalize(
                item.Name,
                item.Slug,
                item.Description,
                item.Category,
                item.BodyRegion,
                item.MovementPattern,
                item.PrimaryMuscleGroup,
                item.PrimaryEquipment,
                item.SecondaryEquipment,
                item.DifficultyLevel,
                item.TrainingStyle,
                item.IsUnilateral,
                item.IsArchived,
                item.Aliases ?? [],
                item.SecondaryMuscleGroups ?? []);

            if (!seenSlugs.Add(normalized.Slug))
            {
                throw new ExerciseCatalogValidationException($"{label}: duplicate slug '{normalized.Slug}'.");
            }

            EnsureAllowed(ValidCategories, normalized.Category, $"{label}: invalid category '{normalized.Category}'.");
            EnsureAllowed(ValidBodyRegions, normalized.BodyRegion, $"{label}: invalid body region '{normalized.BodyRegion}'.");
            EnsureAllowed(ValidMovementPatterns, normalized.MovementPattern, $"{label}: invalid movement pattern '{normalized.MovementPattern}'.");
            EnsureAllowed(ValidDifficultyLevels, normalized.DifficultyLevel, $"{label}: invalid difficulty level '{normalized.DifficultyLevel}'.");
            EnsureAllowed(ValidTrainingStyles, normalized.TrainingStyle, $"{label}: invalid training style '{normalized.TrainingStyle}'.");

            ValidateList(item.Aliases ?? [], normalized.Aliases, $"{label}: alias");
            ValidateList(item.SecondaryMuscleGroups ?? [], normalized.SecondaryMuscleGroups, $"{label}: secondary muscle");
        }
    }

    private static void ValidateList(IReadOnlyList<string> rawValues, IReadOnlyList<string> normalizedValues, string label)
    {
        if (rawValues.Count != normalizedValues.Count)
        {
            throw new ExerciseCatalogValidationException($"{label} entries must be non-empty and unique.");
        }
    }

    private static void EnsureRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExerciseCatalogValidationException(message);
        }
    }

    private static void EnsureAllowed(HashSet<string> allowed, string value, string message)
    {
        if (!allowed.Contains(value))
        {
            throw new ExerciseCatalogValidationException(message);
        }
    }
}

public sealed class ExerciseCatalogValidationException(string message) : Exception(message);
