using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NofyMe.Pages;
using NofyMe.Services;

namespace NofyMe;

public partial class MainWindow : Window
{
    private NotificationsPage _notificacoes = null!;
    private HistoryPage _historico = null!;
    private SettingsPage _configuracoes = null!;

    private readonly DispatcherTimer _undoTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private Action? _undoAction;

    public MainWindow()
    {
        InitializeComponent();

        _notificacoes = new NotificationsPage();
        _historico = new HistoryPage();
        _configuracoes = new SettingsPage();

        // sincroniza os interruptores do rodapé da janela com a página de configurações
        SwStartWithWindows.IsChecked = AppState.Settings.StartWithWindows;
        SwNotifyAtStartup.IsChecked = AppState.Settings.NotifyAtStartup;

        MarkThemeDots(AppState.Settings.ThemeName);
        AppState.ThemeChanged += () => Dispatcher.Invoke(() => MarkThemeDots(AppState.Settings.ThemeName));
        AppState.NotificationsChanged += () => Dispatcher.Invoke(UpdateHeader);

        ShowPage("notificacoes");

        // cantos arredondados: sem recorte, o fundo quadrado da barra lateral escapa da curva
        RootGrid.SizeChanged += (_, _) => ApplyRoundedClip();
        Loaded += (_, _) => ApplyRoundedClip();

        _undoTimer.Tick += (_, _) => HideUndo();

        // fecha a janela = esconder para a bandeja (o app vivo continua disparando)
        Closing += (s, e) =>
        {
            if (AppState.IsShuttingDown) return;
            e.Cancel = true;
            Hide();
        };
    }

    private void ApplyRoundedClip()
    {
        var radius = 17.0; // 18 do contorno menos 1 px da borda
        RootGrid.Clip = new RectangleGeometry(
            new Rect(0, 0, RootGrid.ActualWidth, RootGrid.ActualHeight), radius, radius);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // segunda instância avisa esta janela para aparecer
        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == App.ShowWindowMessage)
        {
            ShowFromTray();
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Mostra a janela (vinda da bandeja ou de uma segunda instância).</summary>
    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;   // garante que sobe para a frente mesmo com outro app em foco
        Topmost = false;
    }

    private void ShowPage(string page)
    {
        switch (page)
        {
            case "historico":
                PageHost.Content = _historico;
                SetHeader("Histórico", sub: null, showNewButton: false);
                break;
            case "config":
                PageHost.Content = _configuracoes;
                SetHeader("Configurações", sub: null, showNewButton: false);
                break;
            default:
                PageHost.Content = _notificacoes;
                UpdateHeader();
                break;
        }
    }

    private void UpdateHeader()
    {
        SetHeader("Notificações", $"{AppState.ActiveCount} ativas", showNewButton: true);
    }

    private void SetHeader(string title, string? sub, bool showNewButton)
    {
        PageTitle.Text = title;
        PageSub.Text = sub ?? "";
        PageSub.Visibility = sub == null ? Visibility.Collapsed : Visibility.Visible;
        BtnNewNotification.Visibility = showNewButton ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        // navega também por teclado/leitor de tela (o estado marcado é a página atual)
        if (!IsLoaded) return;
        if (sender is RadioButton { Tag: string page }) ShowPage(page);
    }

    private void BtnNewNotification_Click(object sender, RoutedEventArgs e)
    {
        // Owner + Activate: a janela modal abre na frente, e não atrás do app
        Activate();
        var dialog = new NewNotificationWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void Sidebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) TryDragMove();
    }

    // faixa superior: arrasta a janela, mas não quando o clique cai nos botões
    private void WindowBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        if (IsInsideButton(e.OriginalSource as DependencyObject)) return;
        TryDragMove();
    }

    private void TryDragMove()
    {
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // o botão do mouse já foi solto; nada a arrastar
        }
    }

    private static bool IsInsideButton(DependencyObject? node)
    {
        while (node != null)
        {
            if (node is Button) return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    // fechar = esconder para a bandeja (o app continua disparando)
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    // ---- desfazer exclusão ----

    public void ShowUndo(string message, Action undo)
    {
        _undoAction = undo;
        UndoText.Text = message;
        UndoBar.Visibility = Visibility.Visible;
        _undoTimer.Stop();
        _undoTimer.Start();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        var action = _undoAction;
        HideUndo();
        action?.Invoke();
    }

    private void HideUndo()
    {
        _undoTimer.Stop();
        _undoAction = null;
        UndoBar.Visibility = Visibility.Collapsed;
    }

    private void StartWithWindows_Checked(object sender, RoutedEventArgs e)
    {
        AppState.Settings.StartWithWindows = true;
        AppState.SaveSettings();
        AutoStartService.Set(true);
        SwStartWithWindows.IsChecked = true;
    }

    private void StartWithWindows_Unchecked(object sender, RoutedEventArgs e)
    {
        AppState.Settings.StartWithWindows = false;
        AppState.SaveSettings();
        AutoStartService.Set(false);
        SwStartWithWindows.IsChecked = false;
    }

    private void NotifyAtStartup_Changed(object sender, RoutedEventArgs e)
    {
        var on = SwNotifyAtStartup.IsChecked == true;
        AppState.Settings.NotifyAtStartup = on;
        AppState.SaveSettings();
        SwNotifyAtStartup.IsChecked = on;
    }

    private void ThemeDot_Checked(object sender, RoutedEventArgs e)
    {
        // o nome do tema fica no Tag; o estado ativo agora é o IsChecked (grupo TemaRapido)
        if (sender is RadioButton { Tag: string theme } && theme != AppState.Settings.ThemeName)
            AppState.ApplyTheme(theme);
    }

    private void MarkThemeDots(string theme)
    {
        SetDot("DotEntardecer", theme == "Entardecer");
        SetDot("DotCafe", theme == "Café");
        SetDot("DotFloresta", theme == "Floresta");
        SetDot("DotPessego", theme == "Pêssego");
    }

    private void SetDot(string name, bool active)
    {
        if (FindName(name) is RadioButton dot) dot.IsChecked = active;
    }
}
