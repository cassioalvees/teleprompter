using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arca.Core.Storage;

/// <summary>
/// Pastas e arquivos do app. Dados do usuário em %APPDATA%\ARCA Teleprompter (roaming),
/// cache/registros em %LOCALAPPDATA%\ARCA Teleprompter.
/// </summary>
public static class AppPaths
{
    public const string Folder = "ARCA Teleprompter";
    /// <summary>Dados do usuário. <c>ARCA_DATA_DIR</c> troca a pasta (testes e suporte não mexem na sessão real).</summary>
    public static string Data { get; set; } = Environment.GetEnvironmentVariable("ARCA_DATA_DIR") is { Length: > 0 } d ? d : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Folder);
    public static string Local { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Folder);
    public static string Settings => Path.Combine(Data, "settings.json");
    public static string Session => Path.Combine(Data, "session.json");
    public static string Versions => Path.Combine(Data, "versions.json");
    public static string Profiles => Path.Combine(Data, "profiles.json");
    public static string Logs => Path.Combine(Local, "logs");
    /// <summary>Modelos de voz (Vosk): primeiro ao lado do executável, depois em %LOCALAPPDATA%.</summary>
    public static IEnumerable<string> VoiceModelDirs()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "models");
        yield return Path.Combine(Local, "models");
    }
}

/// <summary>Leitura e gravação atômica de JSON (grava num .tmp e renomeia).</summary>
public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static T Load<T>(string path, Func<T> fallback)
    {
        try { if (File.Exists(path)) return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? fallback(); }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Falha ao ler {path}: {e.Message}");
            try { File.Copy(path, path + ".corrompido", true); } catch { /* ignora */ }
        }
        return fallback();
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Options));
        File.Move(tmp, path, true);
    }
}

/// <summary>Registro simples em %LOCALAPPDATA%\ARCA Teleprompter\logs\arca-AAAA-MM-DD.log.</summary>
public static class Log
{
    private static readonly object Gate = new();
    public static event Action<string>? Written;
    public static void Info(string m) => Write("INFO", m);
    public static void Warn(string m) => Write("WARN", m);
    public static void Error(string m, Exception? e = null) => Write("ERRO", e == null ? m : $"{m}: {e}");
    private static void Write(string level, string m)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} {level} {m}";
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.Logs);
                File.AppendAllText(Path.Combine(AppPaths.Logs, $"arca-{DateTime.Now:yyyy-MM-dd}.log"), line + Environment.NewLine);
            }
        }
        catch { /* registro nunca derruba o app */ }
        Written?.Invoke(line);
    }
}
