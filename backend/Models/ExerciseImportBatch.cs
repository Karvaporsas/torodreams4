namespace ToroFitDreaming4.Models;

public class ExerciseImportBatch
{
    public int Id { get; set; }
    public string? CatalogVersion { get; set; }
    public string CatalogPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Succeeded";
    public string TriggeredBy { get; set; } = string.Empty;
    public int? TotalExercises { get; set; }
    public int? Created { get; set; }
    public int? Updated { get; set; }
    public int? Unchanged { get; set; }
    public string? Message { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
}
