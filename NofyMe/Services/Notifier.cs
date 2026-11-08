using System.Windows.Threading;
using NofyMe.Models;

namespace NofyMe.Services;

/// <summary>
/// Dispara uma notificação pelo modo certo e registra no Histórico.
/// </summary>
public static class Notifier
{
    public static void Fire(NotificationItem item)
    {
        RecordHistory(item);

        switch (item.Mode)
        {
            case DisplayMode.Popup:
                new PopupWindow(item).Show();
                break;

            case DisplayMode.Native:
                try
                {
                    new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                        .AddText(item.Title)
                        .AddText(item.Message)
                        .Show();
                }
                catch
                {
                    // sem toasts possíveis (política de foco/sem AUMID): cai para o balão do app
                    BalloonWindow.ShowBalloon(item);
                }
                break;

            case DisplayMode.Balloon:
                BalloonWindow.ShowBalloon(item);
                break;
        }
    }

    /// <summary>"Adiar 10 min" do pop-up: reagenda o disparo.</summary>
    public static void FireLater(NotificationItem item, TimeSpan delay)
    {
        var t = new DispatcherTimer { Interval = delay };
        t.Tick += (_, _) =>
        {
            t.Stop();
            if (item.Enabled) Fire(item);
        };
        t.Start();
    }

    private static void RecordHistory(NotificationItem item)
    {
        var history = Store.LoadHistory();
        history.Insert(0, new HistoryEntry
        {
            Title = item.Title,
            Message = item.Message,
            Mode = ModeName(item.Mode),
            FiredAt = DateTime.Now,
        });
        if (history.Count > 200) history.RemoveRange(200, history.Count - 200);
        Store.SaveHistory(history);
        HistoryChanged?.Invoke();
    }

    public static string ModeName(DisplayMode mode) => mode switch
    {
        DisplayMode.Popup => "Pop-up na tela",
        DisplayMode.Native => "Nativa do Windows",
        _ => "Balão compacto",
    };

    /// <summary>Para a janela principal recarregar o histórico em tempo real.</summary>
    public static event Action? HistoryChanged;
}
