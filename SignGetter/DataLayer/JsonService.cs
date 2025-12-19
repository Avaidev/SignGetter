using System.Text.Json;

namespace DataLayer;

public static class JsonService
{
    public static string DataFolderName = "Data";

    private static JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };
    public static T? LoadFromJson<T>(string filename) where T : class
    {
        var fullPath = Path.Combine(Utils.GetFullPath(DataFolderName), filename);
        if (string.IsNullOrEmpty(fullPath)) return null;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(fullPath), _options);
    }
    
    public static bool SaveToJson<T>(T obj, string filename) where T : class
    {
        var fullPath = Path.Combine(Utils.GetFullPath(DataFolderName), filename);
        if (string.IsNullOrEmpty(fullPath)) return false;
        File.WriteAllText(fullPath, JsonSerializer.Serialize(obj, _options));
        return true;
    }
}