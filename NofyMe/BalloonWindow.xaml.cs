using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NofyMe.Models;
using NofyMe.Services;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace NofyMe;

/// <summary>
/// "Balão compacto": bolha na cor de destaque, sem roubar o foco.
/// Os cantos são arredondados e o canto de origem salta para fora,
/// como um único formato de balão. Some sozinho ou com um clique.
/// </summary>
public partial class BalloonWindow : Window
{
    private const int LifetimeMs = 5000;

    // margem transparente da janela (espaço do canto saliente)
    private const double Pad = 14;
    private const double Gap = 24;
    private const double Lobe = 4;

    private static BalloonWindow? _current;

    private readonly NotificationItem _item;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(LifetimeMs) };

    public BalloonWindow(NotificationItem item)
    {
        InitializeComponent();
        _item = item;

        // o design mostra só a mensagem; sem mensagem, usa o título
        TxtBody.Text = string.IsNullOrWhiteSpace(item.Message) ? item.Title : item.Message;

        _timer.Tick += (_, _) => Close();

        Loaded += (_, _) =>
        {
            Position();
            _timer.Start();
        };

        Closed += (_, _) =>
        {
            _timer.Stop();
            if (ReferenceEquals(_current, this)) _current = null;
        };
    }

    public static void ShowBalloon(NotificationItem item)
    {
        _current?.Close();
        var balloon = new BalloonWindow(item);
        _current = balloon;
        balloon.Show();
    }

    private void Position()
    {
        var area = SystemParameters.WorkArea;
        var position = ScreenCorners.Normalize(_item.DisplayCorner);
        var onRight = position.EndsWith("direito");
        var onBottom = position.StartsWith("Inferior");

        var w = Bubble.ActualWidth;
        var h = Bubble.ActualHeight;

        // margem transparente fica para fora; a bolha mantém 24 px do canto
        Left = onRight
            ? area.Right - Gap - w - Pad
            : area.Left + Gap - Pad;

        Top = onBottom
            ? area.Bottom - Gap - h - Pad
            : area.Top + Gap - Pad;

        BubbleShape.Data = BuildBubble(Pad, Pad, w, h, onRight, onBottom);
    }

    /// <summary>
    /// Desenha a bolha com três cantos arredondados e o canto de origem
    /// saliente (curva suave que passa do retângulo e volta para as bordas).
    /// </summary>
    private static Geometry BuildBubble(double x0, double y0, double w, double h,
                                        bool onRight, bool onBottom)
    {
        var r = Math.Min(24, Math.Min(w, h) / 2);
        var p = Math.Min(Lobe, h / 5);
        var lobeStart = Math.Max(x0 + r, Math.Min(x0 + 4.25 * p, x0 + w - r));

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            // topo, canto superior direito e lateral direita
            ctx.BeginFigure(new Point(x0 + r, y0), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(x0 + w - r, y0), true, false);
            ctx.ArcTo(new Point(x0 + w, y0 + r), new Size(r, r), 0,
                      false, SweepDirection.Clockwise, true, false);
            ctx.LineTo(new Point(x0 + w, y0 + h - r), true, false);
            ctx.ArcTo(new Point(x0 + w - r, y0 + h), new Size(r, r), 0,
                      false, SweepDirection.Clockwise, true, false);

            // canto inferior esquerdo: salta para fora e volta à lateral
            ctx.LineTo(new Point(lobeStart, y0 + h), true, false);
            ctx.BezierTo(new Point(x0 + 2.5 * p, y0 + h + 0.19 * p),
                         new Point(x0 + 1.5 * p, y0 + h + 0.56 * p),
                         new Point(x0 + p, y0 + h + p), true, false);
            ctx.BezierTo(new Point(x0 + 0.375 * p, y0 + h + p + 0.19 * p),
                         new Point(x0, y0 + h + 0.625 * p),
                         new Point(x0, y0 + h - 1.75 * p), true, false);
            ctx.LineTo(new Point(x0, y0 + r), true, false);

            // canto superior esquerdo
            ctx.ArcTo(new Point(x0 + r, y0), new Size(r, r), 0,
                      false, SweepDirection.Clockwise, true, false);
        }

        // espelha para o canto escolhido (o desenho base é o inferior esquerdo)
        var cx = x0 + w / 2;
        var cy = y0 + h / 2;
        geo.Transform = new ScaleTransform(onRight ? -1 : 1, onBottom ? 1 : -1, cx, cy);
        geo.Freeze();
        return geo;
    }

    private void Bubble_Click(object sender, MouseButtonEventArgs e) => Close();
}
