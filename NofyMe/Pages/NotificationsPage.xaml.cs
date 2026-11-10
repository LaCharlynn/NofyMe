using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe.Pages;

public partial class NotificationsPage : UserControl
{
    public NotificationsPage()
    {
        InitializeComponent();

        // liga/desliga salva o item (o TwoWay do binding altera Enabled)
        List.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(SwitchChanged));
        List.AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(SwitchChanged));

        AppState.NotificationsChanged += Refresh;
        AppState.ThemeChanged += Refresh;
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) =>
        {
            AppState.NotificationsChanged -= Refresh;
            AppState.ThemeChanged -= Refresh;
        };
    }

    private void Refresh()
    {
        // só recria a lista se a coleção mudou de fato; recriar a cada evento
        // dispara o "Checked" dos interruptores em cascata (loop de CPU)
        if (!ReferenceEquals(List.ItemsSource, AppState.Notifications))
            List.ItemsSource = AppState.Notifications;

        EmptyHint.Visibility = AppState.Notifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SwitchChanged(object sender, RoutedEventArgs e)
    {
        // eventos roteados: o "sender" é a lista; o botão de verdade é o OriginalSource
        var toggle = e.OriginalSource as ToggleButton;
        var item = toggle?.DataContext as Models.NotificationItem;
        if (item == null) return;

        item.NextAfterTime = null; // religar reconta o intervalo
        if (!item.Enabled) item.LastAtTimeDate = null;
        AppState.SaveNotifications();
        AppState.NotifyNotificationsChanged();
    }

    private void Switch_Click(object sender, RoutedEventArgs e)
    {
        // o cartão inteiro é um botão de editar; o clique do interruptor não deve abrir o modal
        e.Handled = true;
    }

    private void Card_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: NotificationItem item }) return;

        var dialog = new NewNotificationWindow(item);
        if (Window.GetWindow(this) is { } owner) dialog.Owner = owner;
        dialog.ShowDialog();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        // mesma razão do interruptor: não deixar o clique chegar ao cartão
        e.Handled = true;

        if (sender is not Button { DataContext: NotificationItem item }) return;

        var index = AppState.Notifications.IndexOf(item);
        if (index < 0) return;

        AppState.Notifications.RemoveAt(index);
        AppState.SaveNotifications();
        AppState.NotifyNotificationsChanged();

        if (Window.GetWindow(this) is MainWindow main)
        {
            main.ShowUndo("Notificação excluída.", () =>
            {
                if (AppState.Notifications.Contains(item)) return;
                AppState.Notifications.Insert(
                    Math.Min(index, AppState.Notifications.Count), item);
                AppState.SaveNotifications();
                AppState.NotifyNotificationsChanged();
            });
        }
    }
}
