namespace DataLayer;

public static class Utils
{
    public static string GetFullPath(string dataFolder)
    {
        var availablePaths = new string[]
        {
            Path.Combine("..", dataFolder),
            Path.Combine("..", "..", dataFolder),
            Path.Combine("..", "..", "..", "..", dataFolder),
            Path.Combine("..", "..", "..", "..", "..", dataFolder),
            Path.Combine(""),
            Path.Combine(dataFolder),
        };

        foreach (var path in availablePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return "";
    }
}