using System.IO;
using System.Text.Json;

namespace UESoundExtractor.utils;

public static class Settings {

    public static AppSettings settings = new();
    
    public static void SaveSettings() {
        string json = JsonSerializer.Serialize(settings);
        string settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");

        File.WriteAllText(settingsFilePath, json);
    }

    public static void LoadSettings() {
        string settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);
            settings = JsonSerializer.Deserialize<AppSettings>(json);
        }
    }
}