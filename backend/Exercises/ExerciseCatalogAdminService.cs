using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Exercises;

public sealed class ExerciseCatalogAdminService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public string GetDefaultCatalogPath() => Path.GetFullPath(ExerciseCatalogPaths.GetDefaultCatalogPath());

    public async Task<ExerciseCatalogStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var catalogPath = GetDefaultCatalogPath();
        var catalogFileExists = File.Exists(catalogPath);
        string? currentCatalogVersion = null;
        string? catalogMessage = null;

        if (catalogFileExists)
        {
            try
            {
                currentCatalogVersion = await ReadCatalogVersionAsync(catalogPath, cancellationToken);
            }
            catch (JsonException)
            {
                catalogMessage = "Catalog file is empty or invalid JSON.";
            }
            catch (IOException ex)
            {
                catalogMessage = ex.Message;
            }
        }
        else
        {
            catalogMessage = "Catalog file was not found.";
        }

        var lastImport = await db.ExerciseImportBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.ImportedAtUtc)
            .ThenByDescending(batch => batch.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new ExerciseCatalogStatusDto(
            catalogPath,
            catalogFileExists,
            currentCatalogVersion,
            catalogMessage,
            lastImport is null ? null : ToDto(lastImport));
    }

    public async Task RecordSuccessAsync(
        string catalogPath,
        string triggeredBy,
        ExerciseCatalogImportResult result,
        CancellationToken cancellationToken = default)
    {
        db.ExerciseImportBatches.Add(new ExerciseImportBatch
        {
            CatalogVersion = result.Version,
            CatalogPath = catalogPath,
            Status = "Succeeded",
            TriggeredBy = triggeredBy,
            TotalExercises = result.TotalExercises,
            Created = result.Created,
            Updated = result.Updated,
            Unchanged = result.Unchanged,
            Message = $"Imported catalog {result.Version}.",
            ImportedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        string catalogPath,
        string? catalogVersion,
        string triggeredBy,
        string message,
        CancellationToken cancellationToken = default)
    {
        db.ExerciseImportBatches.Add(new ExerciseImportBatch
        {
            CatalogVersion = catalogVersion,
            CatalogPath = catalogPath,
            Status = "Failed",
            TriggeredBy = triggeredBy,
            Message = message,
            ImportedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> TryReadCatalogVersionAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await ReadCatalogVersionAsync(path, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static async Task<string?> ReadCatalogVersionAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var catalog = await JsonSerializer.DeserializeAsync<ExerciseCatalogFile>(stream, JsonOptions, cancellationToken)
            ?? throw new JsonException("Catalog file is empty or invalid JSON.");

        return catalog.Version;
    }

    private static ExerciseImportBatchDto ToDto(ExerciseImportBatch batch) =>
        new(
            batch.Id,
            batch.CatalogVersion,
            batch.CatalogPath,
            batch.Status,
            batch.TriggeredBy,
            batch.TotalExercises,
            batch.Created,
            batch.Updated,
            batch.Unchanged,
            batch.Message,
            batch.ImportedAtUtc);
}

public sealed record ExerciseCatalogStatusDto(
    string CatalogPath,
    bool CatalogFileExists,
    string? CurrentCatalogVersion,
    string? CatalogMessage,
    ExerciseImportBatchDto? LastImport);

public sealed record ExerciseImportBatchDto(
    int Id,
    string? CatalogVersion,
    string CatalogPath,
    string Status,
    string TriggeredBy,
    int? TotalExercises,
    int? Created,
    int? Updated,
    int? Unchanged,
    string? Message,
    DateTime ImportedAtUtc);
