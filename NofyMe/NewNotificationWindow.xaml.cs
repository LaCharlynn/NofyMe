using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe;

public partial class NewNotificationWindow : Window
{
    private readonly NotificationItem? _existing;
    private bool _loading;

    /// <param name="existing">Item a editar; sem item, a janela cria uma nova notificação.</param>
    public NewNotificationWindow(NotificationItem? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        DataObject.AddPastingHandler(TxtTimeHour, OnTimePaste);
        DataObject.AddPastingHandler(TxtTimeMinute, OnTimePaste);
        DataObject.AddPastingHandler(TxtInterval, OnTimePaste);
        DataObject.AddPastingHandler(TxtDisplaySeconds, OnTimePaste);

        Loaded += (_, _) =>
        {
            if (_existing != null)
            {
                HeaderTitle.Text = "Editar notificação";
                Title = "Editar notificação";
                FillForm(_existing);
                TxtTitle.SelectAll();
            }
            TxtTitle.Focus();
            _loading = false;
            UpdateDisplayRows();

            // garante que o modal sobe para a frente (há apps que ficam sempre em primeiro plano)
            Activate();
            Topmost = true;
            Topmost = false;
        };

        _loading = true;
    }

    private bool Editing => _existing != null;

    private void FillForm(NotificationItem item)
    {
        _loading = true;
        try
        {
            TxtTitle.Text = item.Title;
            TxtMessage.Text = item.Message;

            RbPopup.IsChecked = item.Mode == DisplayMode.Popup;
            RbNative.IsChecked = item.Mode == DisplayMode.Native;
            RbBalloon.IsChecked = item.Mode == DisplayMode.Balloon;

            // exclusivo: dados antigos podiam ter os dois; o horário fixo vence
            ChkOnStartup.IsChecked = item.TriggerOnStartup;
            ChkAfterTime.IsChecked = item.TriggerAfterTime && !item.TriggerAtTime;
            ChkAtTime.IsChecked = item.TriggerAtTime;

            var seconds = Math.Clamp(item.IntervalSeconds, 1, 86400);
            var unit = item.IntervalUnit;
            if (unit == IntervalUnit.Minutes && seconds % 60 != 0) unit = IntervalUnit.Seconds;

            RbMin.IsChecked = unit == IntervalUnit.Minutes;
            RbSeg.IsChecked = unit == IntervalUnit.Seconds;
            TxtInterval.Text = unit == IntervalUnit.Minutes
                ? (seconds / 60).ToString()
                : seconds.ToString();

            TxtDisplaySeconds.Text = item.DisplaySeconds?.ToString() ?? "";
            TxtDisplayPlaceholder.Text = Math.Clamp(AppState.Settings.PopupSeconds, 1, 60).ToString();
            TempoHint.Text = $"Em branco usa o padrão ({Math.Clamp(AppState.Settings.PopupSeconds, 1, 60)} s).";
            MarkCorner(item.DisplayCorner);

            var parts = (item.TimeOfDay ?? "09:00").Split(':');
            TxtTimeHour.Text = parts.Length > 0 ? parts[0] : "09";
            TxtTimeMinute.Text = parts.Length > 1 ? parts[1] : "00";

            var all = new[] { DaySun, DayMon, DayTue, DayWed, DayThu, DayFri, DaySat };
            if (item.Days.Count == 0)
            {
                foreach (var d in all) d.IsChecked = true;
            }
            else
            {
                var set = item.Days.ToHashSet();
                foreach (var d in all) d.IsChecked = false;
                DaySun.IsChecked = set.Contains(0);
                DayMon.IsChecked = set.Contains(1);
                DayTue.IsChecked = set.Contains(2);
                DayWed.IsChecked = set.Contains(3);
                DayThu.IsChecked = set.Contains(4);
                DayFri.IsChecked = set.Contains(5);
                DaySat.IsChecked = set.Contains(6);
            }
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Dias marcados; se todos marcados, devolve [] (compacta como "Todos os dias").</summary>
    private List<int> SelectedDays()
    {
        var all = new[] { DaySun, DayMon, DayTue, DayWed, DayThu, DayFri, DaySat };
        if (all.All(t => t.IsChecked == true)) return [];

        var dayMap = new Dictionary<ToggleButton, int>
        {
            [DaySun] = 0, [DayMon] = 1, [DayTue] = 2, [DayWed] = 3,
            [DayThu] = 4, [DayFri] = 5, [DaySat] = 6,
        };
        return dayMap
            .Where(kv => kv.Key.IsChecked == true)
            .Select(kv => kv.Value)
            .ToList();
    }

    private DisplayMode GetMode()
    {
        if (RbPopup.IsChecked == true) return DisplayMode.Popup;
        if (RbNative.IsChecked == true) return DisplayMode.Native;
        return DisplayMode.Balloon;
    }

    /// <summary>"Na tela": pop-up mostra tempo e canto; balão só o canto; nativa, nada.</summary>
    private void UpdateDisplayRows()
    {
        if (DisplayCard == null) return;

        var mode = GetMode();
        DisplayCard.Visibility = mode == DisplayMode.Native ? Visibility.Collapsed : Visibility.Visible;
        TempoRow.Visibility = mode == DisplayMode.Popup ? Visibility.Visible : Visibility.Collapsed;
        CornerRow.Margin = mode == DisplayMode.Popup ? new Thickness(0, 10, 0, 0) : new Thickness(0);
    }

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        UpdateDisplayRows();
    }

    private void MarkCorner(string? corner)
    {
        var normalized = ScreenCorners.Normalize(corner);
        CornerSupEsq.IsChecked = normalized == ScreenCorners.TopLeft;
        CornerSupDir.IsChecked = normalized == ScreenCorners.TopRight;
        CornerInfEsq.IsChecked = normalized == ScreenCorners.BottomLeft;
        CornerInfDir.IsChecked = normalized == ScreenCorners.BottomRight;
    }

    private string SelectedCorner()
    {
        if (CornerSupEsq.IsChecked == true) return ScreenCorners.TopLeft;
        if (CornerSupDir.IsChecked == true) return ScreenCorners.TopRight;
        if (CornerInfDir.IsChecked == true) return ScreenCorners.BottomRight;
        return ScreenCorners.BottomLeft;
    }

    /// <summary>Tempo próprio do lembrete: vazio é válido (usa o padrão).</summary>
    private bool TryReadDisplaySeconds(out int? seconds)    {
        seconds = null;
        var text = TxtDisplaySeconds.Text.Trim();
        if (text.Length == 0) return true;

        if (!int.TryParse(text, out var value) || value is < 1 or > 60) return false;

        seconds = value;
        return true;
    }

    private bool TryReadInterval(out int seconds, out string error)
    {
        seconds = 0;
        error = "";

        if (!int.TryParse(TxtInterval.Text.Trim(), out var value))
        {
            error = "Escreva um número no intervalo.";
            return false;
        }

        var max = RbSeg.IsChecked == true ? 86400 : 1440;
        if (value < 1 || value > max)
        {
            error = RbSeg.IsChecked == true
                ? "O intervalo vai de 1 a 86400 segundos."
                : "O intervalo vai de 1 a 1440 minutos.";
            return false;
        }

        seconds = RbSeg.IsChecked == true ? value : value * 60;
        return true;
    }

    private bool TryReadTime(out string time)
    {
        time = "";
        if (!int.TryParse(TxtTimeHour.Text, out var hour) || hour is < 0 or > 23) return false;
        if (!int.TryParse(TxtTimeMinute.Text, out var minute) || minute is < 0 or > 59) return false;
        time = $"{hour:00}:{minute:00}";
        return true;
    }

    private void ApplyForm(NotificationItem item)
    {
        TryReadInterval(out var seconds, out _);
        TryReadTime(out var time);

        var oldTime = item.TimeOfDay;
        var oldDays = item.Days.ToArray();
        var oldAtTime = item.TriggerAtTime;

        item.Title = TxtTitle.Text.Trim();
        item.Message = TxtMessage.Text.Trim();
        item.Mode = GetMode();
        item.TriggerOnStartup = ChkOnStartup.IsChecked == true;
        item.TriggerAfterTime = ChkAfterTime.IsChecked == true;
        item.TriggerAtTime = ChkAtTime.IsChecked == true;
        item.IntervalSeconds = seconds;
        item.IntervalUnit = RbSeg.IsChecked == true ? IntervalUnit.Seconds : IntervalUnit.Minutes;
        item.DisplaySeconds = TryReadDisplaySeconds(out var displaySeconds) ? displaySeconds : null;
        item.DisplayCorner = SelectedCorner();
        item.TimeOfDay = time;
        item.Days = SelectedDays();

        if (item.TriggerAfterTime)
            item.NextAfterTime = Editing
                ? DateTime.Now + TimeSpan.FromSeconds(seconds)
                : null;
        else
            item.NextAfterTime = null;

        // só limpa o "já disparou hoje" se o horário/dias mudaram
        if (item.TriggerAtTime &&
            (!oldAtTime || oldTime != item.TimeOfDay || !oldDays.SequenceEqual(item.Days)))
        {
            item.LastAtTimeDate = null;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorHint.Visibility = Visibility.Collapsed;

        if (TxtTitle.Text.Trim().Length == 0)
        {
            ShowError("Escreva um título para a notificação.");
            TxtTitle.Focus();
            return;
        }

        var seconds = 0;
        if (ChkAfterTime.IsChecked == true)
        {
            if (!TryReadInterval(out seconds, out var intervalError))
            {
                ShowError(intervalError);
                TxtInterval.Focus();
                TxtInterval.SelectAll();
                return;
            }
        }

        if (ChkAtTime.IsChecked == true && !TryReadTime(out _))
        {
            ShowError("Escreva um horário entre 00:00 e 23:59.");
            TxtTimeHour.Focus();
            TxtTimeHour.SelectAll();
            return;
        }

        if (!TryReadDisplaySeconds(out _))
        {
            ShowError("Use um tempo de 1 a 60 s na tela.");
            TxtDisplaySeconds.Focus();
            TxtDisplaySeconds.SelectAll();
            return;
        }

        if (ChkOnStartup.IsChecked != true && ChkAfterTime.IsChecked != true && ChkAtTime.IsChecked != true)
        {
            ShowError("Marque quando a notificação deve aparecer.");
            ChkOnStartup.Focus();
            return;
        }

        var item = _existing ?? new NotificationItem();
        ApplyForm(item);

        if (!Editing)
        {
            AppState.Notifications.Add(item);
        }

        AppState.SaveNotifications();
        AppState.NotifyNotificationsChanged();
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorHint.Text = message;
        ErrorHint.Visibility = Visibility.Visible;
    }

    // ---- campos numéricos: só dígitos; o ":" é fixo ----

    private void TimePart_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsDigit);

    private void TimePart_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) e.Handled = true;
    }

    private void OnTimePaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var text = (string)e.DataObject.GetData(typeof(string))!;
        if (!text.All(char.IsDigit)) e.CancelCommand();
    }

    private void DisplaySeconds_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtDisplayPlaceholder == null) return;
        TxtDisplayPlaceholder.Visibility =
            TxtDisplaySeconds.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TimeHour_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        if (TxtTimeHour.Text.Length == 2 && TxtTimeHour.IsKeyboardFocused)
            TxtTimeMinute.Focus();
    }

    private void TimePart_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not TextBox box) return;

        var digits = new string(box.Text.Where(char.IsDigit).ToArray());
        box.Text = digits.Length == 0 ? "00" : digits.PadLeft(2, '0');
    }

    // ao entrar no campo, seleciona o valor para digitar por cima (09 -> 1 -> 15)
    private void TimePart_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box) return;

        // o clique/foco do WPF reposiciona o cursor depois deste evento:
        // selecionar no próximo ciclo garante que o valor fique selecionado
        Dispatcher.BeginInvoke(new Action(box.SelectAll));
    }

    // ---- gatilhos de tempo: um ou o outro ----

    private void ChkAfterTime_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (ChkAfterTime.IsChecked == true) ChkAtTime.IsChecked = false;
    }

    private void ChkAtTime_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (ChkAtTime.IsChecked == true) ChkAfterTime.IsChecked = false;
    }
}
