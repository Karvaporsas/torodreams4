using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Exercises;

namespace ToroFitDreaming4.Cli;

public static class ExerciseCatalogBackfillCommand
{
    public static async Task<int> RunAsync(string[] args, IConfiguration config)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: dotnet run -- --backfill-exercises");
            return 1;
        }

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new AppDbContext(dbOptions);
        var backfill = new ExerciseCatalogBackfillService(db);
        var result = await backfill.BackfillAsync();

        Console.WriteLine(
            $"Backfilled exercises: total={result.TotalExercises}, updated={result.Updated}, unchanged={result.Unchanged}.");

        return 0;
    }
}
