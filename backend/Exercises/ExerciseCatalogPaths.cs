namespace ToroFitDreaming4.Exercises;

public static class ExerciseCatalogPaths
{
    public static string GetDefaultCatalogPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "exercise-catalog.v1.json");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var repositoryPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "backend",
            "SeedData",
            "exercise-catalog.v1.json"));

        return repositoryPath;
    }
}
