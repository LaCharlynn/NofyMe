using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NofyMe.Services;

namespace NofyMe.Pages;

public partial class SettingsPage : UserControl
{
    private bool _tempoAtualizando;

    public SettingsPage()
    {
        InitializeComponent();
        AppState.ThemeChanged += RefreshTheme;
        Loaded += (_, _) => RefreshSettings();
        Unloaded += (_, _) => AppState.ThemeChanged -= RefreshTheme;
    }

    private void RefreshSettings()
    {
        SwStartWithWindows.IsChecked = AppState.Settings.StartWithWindows;
        SwNotifyAtStartup.IsChecked = AppState.Settings.NotifyAtStartup;

        _tempoAtualizando = true;
        TempoPopup.Text = Math.Clamp(AppState.Settings.PopupSeconds, 1, 60).ToString();
        _tempoAtualizando = false;
        TempoErro.Visibility = Visibility.Collapsed;

        MarkPopupCorner(AppState.Settings.PopupPosition);
        MarkBalloonCorner(AppState.Settings.BalloonPosition);
        RefreshTheme();
    }

    private void TempoPopup_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsDigit);

    private void TempoPopup_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || _tempoAtualizando) return;

        var digits = new string(TempoPopup.Text.Where(char.IsDigit).ToArray());
        if (digits != TempoPopup.Text)
        {
            _tempoAtualizando = true;
            TempoPopup.Text = digits;
            TempoPopup.CaretIndex = digits.Length;
            _tempoAtualizando = false;
            return;
        }

        if (!int.TryParse(digits, out var seconds) || seconds is < 1 or > 60)
        {
            TempoErro.Text = "Use um valor de 1 a 60.";
            TempoErro.Visibility = Visibility.Visible;
            return;
        }

        TempoErro.Visibility = Visibility.Collapsed;
        if (AppState.Settings.PopupSeconds == seconds) return;

        AppState.Settings.PopupSeconds = seconds;
        AppState.SaveSettings();
    }

    private void TempoPopup_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => TempoPopup.SelectAll()), DispatcherPriority.Input);
    }

    private void TempoPopup_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (int.TryParse(TempoPopup.Text, out var seconds) && seconds is >= 1 and <= 60) return;

        _tempoAtualizando = true;
        TempoPopup.Text = Math.Clamp(AppState.Settings.PopupSeconds, 1, 60).ToString();
        _tempoAtualizando = false;
        TempoErro.Visibility = Visibility.Collapsed;
    }

    private void TempoPopup_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void MarkPopupCorner(string position)
    {
        var normalized = ScreenCorners.Normalize(position);
        PopSupEsq.IsChecked = normalized == ScreenCorners.TopLeft;
        PopSupDir.IsChecked = normalized == ScreenCorners.TopRight;
        PopInfEsq.IsChecked = normalized == ScreenCorners.BottomLeft;
        PopInfDir.IsChecked = normalized == ScreenCorners.BottomRight;
    }

    private void PopupCorner_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string position }) return;

        var normalized = ScreenCorners.Normalize(position);
        if (AppState.Settings.PopupPosition == normalized) return;

        AppState.Settings.PopupPosition = normalized;
        AppState.SaveSettings();
    }

    private void MarkBalloonCorner(string position)
    {
        var normalized = ScreenCorners.Normalize(position);
        PosSupEsq.IsChecked = normalized == ScreenCorners.TopLeft;
        PosSupDir.IsChecked = normalized == ScreenCorners.TopRight;
        PosInfEsq.IsChecked = normalized == ScreenCorners.BottomLeft;
        PosInfDir.IsChecked = normalized == ScreenCorners.BottomRight;
    }

    private void BalloonCorner_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string position }) return;

        var normalized = ScreenCorners.Normalize(position);
        if (AppState.Settings.BalloonPosition == normalized) return;

        AppState.Settings.BalloonPosition = normalized;
        AppState.SaveSettings();
    }

    private void RefreshTheme()
    {
        foreach (var name in ThemeManager.ThemeNames)
        {
            if (this.FindName(ThemeNameToStatus(name)) is not TextBlock status ||
                this.FindName(ThemeButtonName(name)) is not RadioButton button) continue;

            var active = AppState.Settings.ThemeName == name;
            status.Text = active ? "Em uso" : "Aplicar";
            button.IsChecked = active;
        }
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string name }) AppState.ApplyTheme(name);
    }

    private void StartWithWindows_On(object sender, RoutedEventArgs e) => SetStartWith(true);
    private void StartWithWindows_Off(object sender, RoutedEventArgs e) => SetStartWith(false);

    private void SetStartWith(bool on)
    {
        AppState.Settings.StartWithWindows = on;
        AppState.SaveSettings();
        AutoStartService.Set(on);
    }

    private void NotifyAtStartup_On(object sender, RoutedEventArgs e) => SetNotifyAtStartup(true);
    private void NotifyAtStartup_Off(object sender, RoutedEventArgs e) => SetNotifyAtStartup(false);

    private static void SetNotifyAtStartup(bool on)
    {
        AppState.Settings.NotifyAtStartup = on;
        AppState.SaveSettings();
    }

    private static string ThemeNameToStatus(string theme) => theme switch
    {
        "Café" => "StatusCafé",
        "Floresta" => "StatusFloresta",
        "Pêssego" => "StatusPêssego",
        _ => "StatusEntardecer",
    };

    private static string ThemeButtonName(string theme) => theme switch
    {
        "Café" => "ThemeCafé",
        "Floresta" => "ThemeFloresta",
        "Pêssego" => "ThemePêssego",
        _ => "ThemeEntardecer",
    };
}
