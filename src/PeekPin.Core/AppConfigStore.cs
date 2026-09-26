using System.Text.Json;

namespace PeekPin;

public sealed class AppConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _directory;

    public AppConfigStore(string directory)
    {
        _directory = directory;
    }

    public string FilePath => Path.Combine(_directory, "config.json");

    public AppConfig Load()
    {
        if (!File.Exists(FilePath))
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return AppConfigNormalizer.Normalize(config);
        }
        catch (JsonException)
        {
            return AppConfig.CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(_directory);
        var normalized = AppConfigNormalizer.Normalize(config);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(FilePath))
        {
            File.Replace(tempPath, FilePath, null);
        }
        else
        {
            File.Move(tempPath, FilePath);
        }
    }
}
