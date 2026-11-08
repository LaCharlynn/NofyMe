using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe;

public partial class App : System.Windows.Application
{
    private const int SchedulerTickSeconds = 10;
    private const string MutexName = "NofyMe.SingleInstance";

    private readonly DispatcherTimer _scheduler = new()
    {
        Interval = TimeSpan.FromSeconds(SchedulerTickSeconds),
    };

    private Mutex? _instanceMutex;
    private bool _ownsMutex;

    /// <summary>Mensagem avisando a instância já aberta para mostrar a janela.</summary>
    public static readonly int ShowWindowMessage = RegisterWindowMessage("NofyMe.ShowWindow");

    private static readonly IntPtr HwndBroadcast = new(0xffff);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    protected override void OnStartup(StartupEventArgs e)
    {
        // instância única: a segunda apenas traz a primeira para a frente
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out var isFirst);
        _ownsMutex = isFirst;
        if (!isFirst)
        {
            PostMessage(HwndBroadcast, (uint)ShowWindowMessage, IntPtr.Zero, IntPtr.Zero);
            Shutdown();
            return;
        }

        // registro de exceções não tratadas (exceções de interface não derrubam o app em silêncio)
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nofyme_no.log"),
                    $"{DateTime.Now:HH:mm:ss} {args.Exception}\n");
            }
            catch { }
            args.Handled = true;
        };

        AppState.Load();
        ThemeManager.Apply(AppState.Settings.ThemeName);

        TrayService.Initialize();

        base.OnStartup(e);

        MainWindow = new MainWindow();
        MainWindow.Show();

        // dispara os gatilhos "Ao iniciar o Windows" uma vez por sessão
        if (AppState.Settings.NotifyAtStartup)
        {
            foreach (var item in AppState.Notifications)
            {
                if (item.Enabled && item.TriggerOnStartup) Notifier.Fire(item);
            }
        }
        AppState.StartupNotificationsDone = true;

        AppState.NotificationsChanged += UpdateSchedulerInterval;

        _scheduler.Tick += (_, _) => RunSchedulerTick();
        UpdateSchedulerInterval();
        _scheduler.Start();
    }

    /// <summary>
    /// Intervalos em segundos precisam de tick de 1 s; o resto do tempo
    /// o app dorme em ticks de 10 s (menos CPU e bateria).
    /// </summary>
    private void UpdateSchedulerInterval()
    {
        var needsFastTick = AppState.Notifications.Any(
            n => n.Enabled && n.TriggerAfterTime && n.IntervalSeconds < 60);

        var seconds = needsFastTick ? 1 : SchedulerTickSeconds;
        if (Math.Abs(_scheduler.Interval.TotalSeconds - seconds) > 0.001)
            _scheduler.Interval = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>Verifica a cada tick quais gatilhos venceram.</summary>
    private void RunSchedulerTick()
    {
        var now = DateTime.Now;
        var dirty = false;

        foreach (var item in AppState.Notifications)
        {
            if (!item.Enabled) continue;

            // (b) a cada X minutos/segundos
            if (item.TriggerAfterTime)
            {
                if (item.NextAfterTime is null)
                {
                    if (ScheduleEvaluator.IsDueAfterTime(now, item.IntervalSeconds, AppState.AppStartTime, out var next0))
                    {
                        // app ficou aberto além do intervalo sem tick: dispara e agenda o próximo
                        Notifier.Fire(item);
                        item.NextAfterTime = now + TimeSpan.FromSeconds(Math.Clamp(item.IntervalSeconds, 1, 86400));
                        dirty = true;
                    }
                    else
                    {
                        item.NextAfterTime = next0;
                        dirty = true;
                    }
                }
                else if (now >= item.NextAfterTime)
                {
                    Notifier.Fire(item);
                    item.NextAfterTime = now + TimeSpan.FromSeconds(Math.Clamp(item.IntervalSeconds, 1, 86400));
                    dirty = true;
                }
            }

            // (c) em determinado horário (com dias da semana)
            if (item.TriggerAtTime &&
                ScheduleEvaluator.IsDueAtTime(now, item.TimeOfDay, item.Days, item.LastAtTimeDate))
            {
                Notifier.Fire(item);
                item.LastAtTimeDate = now.ToString("yyyy-MM-dd");
                dirty = true;
            }
        }

        if (dirty) AppState.SaveNotifications();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _scheduler.Stop();
        TrayService.Dispose();

        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        _instanceMutex = null;

        base.OnExit(e);
    }
}
