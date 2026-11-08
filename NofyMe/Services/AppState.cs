using NofyMe.Models;

namespace NofyMe.Services;

/// <summary>
/// Estado executar compartilhado entre as janelas: itens, config global e eventos.
/// </summary>
public static class AppState
{
    public static DateTime AppStartTime { get; private set; } = DateTime.Now;

    public static System.Collections.ObjectModel.ObservableCollection<NotificationItem> Notifications
    { get; private set; } = new();
    public static AppSettings Settings { get; private set; } = new();

    public static bool IsShuttingDown { get; set; }

    public static bool StartupNotificationsDone;

    public static void Load()
    {
        AppStartTime = DateTime.Now;
        Settings = Store.LoadSettings();
        Notifications = new System.Collections.ObjectModel.ObservableCollection<NotificationItem>(Store.LoadNotifications());
    }

    public static void SaveNotifications() => Store.SaveNotifications(Notifications);

    public static void SaveSettings() => Store.SaveSettings(Settings);

    public static event Action? NotificationsChanged;

    public static void NotifyNotificationsChanged() => NotificationsChanged?.Invoke();

    public static int ActiveCount => Notifications.Count(n => n.Enabled);

    public static void ApplyTheme(string themeName)
    {
        Settings.ThemeName = ThemeManager.ThemeNames.Contains(themeName) ? themeName : Settings.ThemeName;
        ThemeManager.Apply(Settings.ThemeName);
        SaveSettings();
        ThemeChanged?.Invoke();
    }

    public static event Action? ThemeChanged;
}
