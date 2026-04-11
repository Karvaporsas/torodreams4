namespace ToroFitDreaming4.Exercises;

public static class ExerciseCatalogPaths
{
    public static string GetDefaultCatalogPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "SeedData", "exercise-catalog.v1.json");
    }
}
