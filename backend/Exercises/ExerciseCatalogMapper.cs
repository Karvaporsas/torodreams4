using System.Globalization;
using System.Text;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Exercises;

public static class ExerciseCatalogMapper
{
    public static NormalizedExerciseRequest Normalize(
        string name,
        string? slug,
        string? description,
        string? category,
        string? bodyRegion,
        string? movementPattern,
        string? primaryMuscleGroup,
        string? primaryEquipment,
        string? secondaryEquipment,
        string? difficultyLevel,
        string? trainingStyle,
        bool isUnilateral,
        bool isArchived,
        string[]? aliases,
        string[]? secondaryMuscleGroups)
    {
        var normalizedName = name.Trim();
        var normalizedPrimaryMuscle = NormalizeRequired(primaryMuscleGroup, ExerciseCatalogDefaults.PrimaryMuscleGroup);
        var normalizedAliases = NormalizeValues(aliases)
            .Where(alias => !string.Equals(alias, normalizedName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var normalizedSecondaryMuscles = NormalizeValues(secondaryMuscleGroups)
            .Where(muscle => !string.Equals(muscle, normalizedPrimaryMuscle, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new NormalizedExerciseRequest(
            string.IsNullOrWhiteSpace(slug) ? Slugify(normalizedName) : Slugify(slug),
            normalizedName,
            NormalizeOptional(description),
            NormalizeRequired(category, ExerciseCatalogDefaults.Category),
            NormalizeRequired(bodyRegion, ExerciseCatalogDefaults.BodyRegion),
            NormalizeRequired(movementPattern, ExerciseCatalogDefaults.MovementPattern),
            normalizedPrimaryMuscle,
            NormalizeOptional(primaryEquipment),
            NormalizeOptional(secondaryEquipment),
            NormalizeRequired(difficultyLevel, ExerciseCatalogDefaults.DifficultyLevel),
            NormalizeRequired(trainingStyle, ExerciseCatalogDefaults.TrainingStyle),
            isUnilateral,
            isArchived,
            normalizedAliases,
            normalizedSecondaryMuscles);
    }

    public static void ApplyToExercise(Exercise exercise, NormalizedExerciseRequest normalized, DateTime now, bool isNew)
    {
        exercise.Slug = normalized.Slug;
        exercise.Name = normalized.Name;
        exercise.Description = normalized.Description;
        exercise.Category = normalized.Category;
        exercise.BodyRegion = normalized.BodyRegion;
        exercise.MovementPattern = normalized.MovementPattern;
        exercise.PrimaryMuscleGroup = normalized.PrimaryMuscleGroup;
        exercise.PrimaryEquipment = normalized.PrimaryEquipment;
        exercise.SecondaryEquipment = normalized.SecondaryEquipment;
        exercise.DifficultyLevel = normalized.DifficultyLevel;
        exercise.TrainingStyle = normalized.TrainingStyle;
        exercise.IsUnilateral = normalized.IsUnilateral;
        exercise.IsArchived = normalized.IsArchived;
        exercise.UpdatedAtUtc = now;

        if (isNew)
        {
            exercise.CreatedAtUtc = now;
        }

        exercise.Aliases = normalized.Aliases.Select(alias => new ExerciseAlias { Alias = alias }).ToList();
        exercise.SecondaryMuscles = normalized.SecondaryMuscleGroups
            .Select(muscle => new ExerciseSecondaryMuscle { MuscleGroup = muscle })
            .ToList();
        exercise.SearchTerms = BuildSearchTerms(exercise);
    }

    public static string BuildSearchTerms(Exercise exercise)
    {
        var values = new List<string?>
        {
            exercise.Name,
            exercise.Description,
            exercise.Category,
            exercise.BodyRegion,
            exercise.MovementPattern,
            exercise.PrimaryMuscleGroup,
            exercise.PrimaryEquipment,
            exercise.SecondaryEquipment,
            exercise.DifficultyLevel,
            exercise.TrainingStyle
        };

        values.AddRange(exercise.Aliases.Select(alias => alias.Alias));
        values.AddRange(exercise.SecondaryMuscles.Select(muscle => muscle.MuscleGroup));

        return string.Join(' ', values
            .Select(NormalizeOptional)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static List<string> NormalizeValues(IEnumerable<string>? values) =>
        values?
            .Select(NormalizeOptional)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList()
        ?? [];

    private static string NormalizeRequired(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
                continue;
            }

            if (builder.Length > 0 && !previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "exercise" : slug;
    }
}

public sealed record NormalizedExerciseRequest(
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
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> SecondaryMuscleGroups);
