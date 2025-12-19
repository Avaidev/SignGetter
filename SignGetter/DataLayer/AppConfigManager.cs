using System.Text.Json;

namespace DataLayer;

public static class AppConfigManager
{
    private static AppSettings? _appSettings;

    public static AppSettings AppSettings
    {
        get
        {
            if (_appSettings == null) LoadAppSettings();
            return _appSettings!;
        }
        private set
        {
            _appSettings = value;
            SaveAppSettings();
        }
    }
    
    private static void LoadAppSettings()
    {
        _appSettings = JsonService.LoadFromJson<AppSettings>(@"appsettings.json") ?? new AppSettings();
    }
    
    public static bool SaveAppSettings()
    {
        if (_appSettings == null) return false;
        return JsonService.SaveToJson(_appSettings, @"appsettings.json");
    }
}