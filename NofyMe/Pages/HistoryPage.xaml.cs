using System.Windows;
using System.Windows.Controls;
using NofyMe.Services;

namespace NofyMe.Pages;

public partial class HistoryPage : UserControl
{
    public HistoryPage()
    {
        InitializeComponent();
        Notifier.HistoryChanged += Refresh;
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) => Notifier.HistoryChanged -= Refresh;
    }

    private void Refresh()
    {
        var history = Store.LoadHistory();
        List.ItemsSource = history;
        EmptyHint.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
