using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Exercises;

namespace ToroFitDreaming4.Cli;

public static class ExerciseCatalogImporterCommand
{
    public static async Task<int> RunAsync(string[] args, IConfiguration config)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("Usage: dotnet run -- --import-exercises [catalog-path]");
            return 1;
        }

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var catalogPath = args.Length == 2 ? args[1].Trim() : ExerciseCatalogPaths.GetDefaultCatalogPath();
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            Console.Error.WriteLine("Error: catalog path cannot be blank.");
            return 1;
        }

        var fullPath = Path.GetFullPath(catalogPath);
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Error: catalog file '{fullPath}' was not found.");
            return 1;
        }

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new AppDbContext(dbOptions);
        var importer = new ExerciseCatalogImportService(db);

        try
        {
            var result = await importer.ImportFromFileAsync(fullPath);
            Console.WriteLine($"Imported exercise catalog {result.Version}: total={result.TotalExercises}, created={result.Created}, updated={result.Updated}, unchanged={result.Unchanged}.");
            return 0;
        }
        catch (ExerciseCatalogValidationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
