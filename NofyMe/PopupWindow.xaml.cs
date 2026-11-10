using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe;

/// <summary>
/// Pop-up personalizado do NofyMe: entra pelo canto escolhido nas Configurações,
/// empilha com outros pop-ups abertos e some sozinho (ou por botão).
/// </summary>
public partial class PopupWindow : Window
{
    private static readonly List<PopupWindow> Open = [];

    private const int SlotHeight = 194;
    private const int Gap = 24;

    private readonly NotificationItem _item;
    private readonly int _lifetimeMs;
    private readonly DispatcherTimer _timer;

    public PopupWindow(NotificationItem item)
    {
        InitializeComponent();
        _item = item;
        DataContext = item;

        var seconds = _item.DisplaySeconds ?? AppState.Settings.PopupSeconds;
        _lifetimeMs = Math.Clamp(seconds, 1, 60) * 1000;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_lifetimeMs) };
        _timer.Tick += (_, _) => CloseInternal();

        Loaded += (_, _) =>
        {
            Open.Add(this);
            Position();

            // entra deslizando do canto escolhido
            // (RenderTransform não é válido em Window; anima o conteúdo)
            var onRight = Corner.EndsWith("direito");
            var offset = onRight ? Width + 48 : -(Width + 48);
            var slide = new DoubleAnimation(0, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };
            var transform = new TranslateTransform(offset, 0);
            Root.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, slide);

            Progress.BeginAnimation(WidthProperty, BuildCountdownAnimation());
            _timer.Start();
        };
        Closed += (_, _) =>
        {
            Open.Remove(this);
            _timer.Stop();
            RelayoutAll();
        };
    }

    private DoubleAnimation BuildCountdownAnimation()
    {
        return new DoubleAnimation
        {
            From = 340,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(_lifetimeMs),
        };
    }

    private static void RelayoutAll()
    {
        foreach (var w in Open.ToList()) w.Position();
    }

    /// <summary>Canto deste lembrete; sem escolha própria, o inferior esquerdo.</summary>
    private string Corner => ScreenCorners.Normalize(_item.DisplayCorner);

    private void Position()
    {
        var area = SystemParameters.WorkArea;
        var position = Corner;
        var onRight = position.EndsWith("direito");
        var slot = Math.Max(0, Open.IndexOf(this));

        Left = onRight
            ? area.Right - Width - Gap
            : area.Left + Gap;

        Top = position.StartsWith("Superior")
            ? area.Top + Gap + slot * SlotHeight
            : area.Bottom - Height - Gap - slot * SlotHeight;
    }

    private void CloseInternal()
    {
        _timer.Stop();
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        CloseInternal();
    }
}
