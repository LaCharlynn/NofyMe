using System.Windows.Forms;

namespace NofyMe.Services;

/// <summary>
/// Ícone do NofyMe na bandeja do Windows, desenhado em runtime (sem arquivo .ico).
/// Também fornece o "Balão compacto" (balloon tip) para o modo Balão.
/// </summary>
public static class TrayService
{
    private static NotifyIcon? _icon;

    public static void Initialize()
    {
        _icon = new NotifyIcon
        {
            Text = "NofyMe",
            Visible = true,
            Icon = LoadAppIcon(),
            ContextMenuStrip = CreateMenu(),
        };
        _icon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>Ícone oficial (o mesmo do executável); desenha um sino se faltar.</summary>
    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (path != null)
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon != null) return icon;
            }
        }
        catch
        {
            // sem ícone associado: usa o desenho de reserva
        }
        return MakeIcon();
    }

    private static ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir NofyMe", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) =>
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
        });
        return menu;
    }

    private static void ShowMainWindow()
    {
        var win = System.Windows.Application.Current.Windows
            .OfType<MainWindow>()
            .FirstOrDefault();
        if (win == null) return;

        win.Show();
        win.WindowState = System.Windows.WindowState.Normal;
        win.Activate();
    }

    public static void ShowBalloon(string title, string message)
    {
        if (_icon == null) return;
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = string.IsNullOrWhiteSpace(message) ? title : message;
        _icon.ShowBalloonTip(6000);
    }

    /// <summary>Círculo roxo com o sino do design (mesma geometria do SVG), 32×32.</summary>
    private static System.Drawing.Icon MakeIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            using (var circle = new System.Drawing.Drawing2D.GraphicsPath())
            {
                circle.AddEllipse(1, 1, 30, 30);
                using var fill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x7C, 0x5C, 0xF0));
                g.FillPath(fill, circle);
            }

            // sino do design: viewBox 24x24, corpo 3..21 em x e 2..22 em y
            const float scale = 1.1f;
            const float offsetX = 2.8f;
            const float offsetY = 2.8f;

            using var bell = new System.Drawing.Drawing2D.GraphicsPath();
            bell.AddArc(6, 2, 12, 12, 180, 180);                        // topo do sino
            bell.AddBezier(18, 8, 18, 15, 21, 17, 21, 17);              // lateral direita
            bell.AddLine(21, 17, 3, 17);                                // base
            bell.AddBezier(3, 17, 3, 17, 6, 15, 6, 8);                  // lateral esquerda
            bell.CloseFigure();
            bell.StartFigure();
            bell.AddBezier(10.3f, 21, 11.2f, 22.2f, 12.8f, 22.2f, 13.7f, 21); // badalo

            using var matrix = new System.Drawing.Drawing2D.Matrix();
            matrix.Scale(scale, scale);
            matrix.Translate(offsetX, offsetY, System.Drawing.Drawing2D.MatrixOrder.Append);
            bell.Transform(matrix);

            using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2f * scale)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            };
            g.DrawPath(pen, bell);
        }

        var hIcon = bmp.GetHicon();
        var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon); // devolve o handle temporário do GetHicon
        return icon;        // Clone é indepeniente do bitmap, que pode ser descartado
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);

    public static void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _icon = null;
        }
    }
}

/// <summary>Chave de execução automática do Windows (por usuário, sem admin).</summary>
public static class AutoStartService
{
    private const string KeyName = "NofyMe";

    public static bool IsEnabled()
    {
        using var key = OpenRunKey();
        return key?.GetValue(KeyName) != null;
    }

    public static void Set(bool enable)
    {
        using var key = OpenRunKey(writable: true);
        if (key == null) return;
        try
        {
            if (enable)
            {
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exe)) return;
                key.SetValue(KeyName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(KeyName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // registro bloqueado por política não derruba o app
        }
    }

    private static Microsoft.Win32.RegistryKey? OpenRunKey(bool writable = false) =>
        Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable);
}
