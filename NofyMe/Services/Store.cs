using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NofyMe.Models;

namespace NofyMe.Services;

/// <summary>
/// Persistência dos dados do app em %AppData%\NofyMe (JSON).
/// </summary>
public static class Store
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Usado pelos testes para não tocar no %AppData% real.</summary>
    public static string? FolderOverride;

    public static string Folder
    {
        get
        {
            var dir = FolderOverride ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NofyMe");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string NotificationsPath => Path.Combine(Folder, "notifications.json");
    private static string SettingsPath => Path.Combine(Folder, "settings.json");
    private static string HistoryPath => Path.Combine(Folder, "history.json");

    /// <summary>
    /// Gravação atômica: escreve em arquivo temporário, força o conteúdo
    /// ir para o disco e só então substitui o arquivo oficial.
    /// Assim um desligamento crash no meio do salvamento não apaga os dados.
    /// </summary>
    private static void WriteAllTextSafe(string path, string content)
    {
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.Write(content);
            writer.Flush();
            stream.Flush(true); // FlushToDisk: conteúdo inteiro no disco antes de seguir
        }
        File.Move(temp, path, overwrite: true);
    }

    public static void SaveNotifications(System.Collections.Generic.IEnumerable<NotificationItem> items) =>
        WriteAllTextSafe(NotificationsPath, JsonSerializer.Serialize(items, JsonOpts));

    public static List<NotificationItem> LoadNotifications()
    {
        try
        {
            if (!File.Exists(NotificationsPath)) return [];
            var data = File.ReadAllText(NotificationsPath, System.Text.Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(data)) return [];
            return JsonSerializer.Deserialize<List<NotificationItem>>(data, JsonOpts) ?? [];
        }
        catch
        {
            // arquivo corrompido não pode derrubar o app; começa vazio
            return [];
        }
    }

    public static void SaveSettings(AppSettings settings) =>
        WriteAllTextSafe(SettingsPath, JsonSerializer.Serialize(settings, JsonOpts));

    public static AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var data = File.ReadAllText(SettingsPath, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettings>(data, JsonOpts) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void SaveHistory(List<HistoryEntry> entries) =>
        WriteAllTextSafe(HistoryPath, JsonSerializer.Serialize(entries, JsonOpts));

    public static List<HistoryEntry> LoadHistory()
    {
        try
        {
            if (!File.Exists(HistoryPath)) return [];
            var data = File.ReadAllText(HistoryPath, System.Text.Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(data)) return [];
            return JsonSerializer.Deserialize<List<HistoryEntry>>(data, JsonOpts) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public class HistoryEntry
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Mode { get; set; } = "";
    public DateTime FiredAt { get; set; } = DateTime.Now;
}
