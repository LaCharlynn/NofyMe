using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe.Tests;

public class StoreTests : IDisposable
{
    private readonly string _temp = Path.Combine(
        Path.GetTempPath(),
        "nofyme-tests-" + Guid.NewGuid().ToString("N"));

    public StoreTests()
    {
        Store.FolderOverride = _temp;
    }

    public void Dispose()
    {
        Store.FolderOverride = null;
        if (Directory.Exists(_temp)) Directory.Delete(_temp, recursive: true);
    }

    [Fact]
    public void Roundtrip_notificacoes()
    {
        var items = new List<NotificationItem>
        {
            new()
            {
                Title = "Beber água",
                Message = "Hora de uma pausa e um copo d'água.",
                Mode = DisplayMode.Popup,
                TriggerAfterTime = true,
                IntervalSeconds = 2700,
            },
            new()
            {
                Title = "Reunião diária",
                Message = "Começa em 5 minutos.",
                Mode = DisplayMode.Native,
                TriggerAtTime = true,
                TimeOfDay = "09:00",
                Days = [1, 2, 3, 4, 5],
                Enabled = false,
            },
            new()
            {
                Title = "Bom dia",
                Message = "",
                Mode = DisplayMode.Balloon,
                TriggerOnStartup = true,
            },
        };

        Store.SaveNotifications(items);
        var loaded = Store.LoadNotifications();

        Assert.Equal(3, loaded.Count);
        Assert.Equal("Beber água", loaded[0].Title);
        Assert.Equal(DisplayMode.Popup, loaded[0].Mode);
        Assert.Equal(2700, loaded[0].IntervalSeconds);
        Assert.Equal(DisplayMode.Native, loaded[1].Mode);
        Assert.Equal("09:00", loaded[1].TimeOfDay);
        Assert.Equal([1, 2, 3, 4, 5], loaded[1].Days);
        Assert.False(loaded[1].Enabled);
        Assert.Equal(DisplayMode.Balloon, loaded[2].Mode);
        Assert.True(loaded[2].TriggerOnStartup);
    }

    [Fact]
    public void Roundtrip_config()
    {
        var settings = new AppSettings
        {
            ThemeName = "Floresta",
            StartWithWindows = true,
            NotifyAtStartup = false,
            BalloonPosition = ScreenCorners.TopLeft,
            PopupPosition = ScreenCorners.TopRight,
            PopupSeconds = 12,
        };

        Store.SaveSettings(settings);
        var loaded = Store.LoadSettings();

        Assert.Equal("Floresta", loaded.ThemeName);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.NotifyAtStartup);
        Assert.Equal(ScreenCorners.TopLeft, loaded.BalloonPosition);
        Assert.Equal(ScreenCorners.TopRight, loaded.PopupPosition);
        Assert.Equal(12, loaded.PopupSeconds);
    }

    [Fact]
    public void Balao_cai_no_canto_inferior_direito_quando_posicao_e_invalida()
    {
        Assert.Equal(ScreenCorners.BottomRight, ScreenCorners.Normalize(null));
        Assert.Equal(ScreenCorners.BottomRight, ScreenCorners.Normalize("Centro"));
        Assert.Equal(ScreenCorners.TopRight, ScreenCorners.Normalize(ScreenCorners.TopRight));
    }

    [Fact]
    public void Carrega_vazio_quando_nao_existe_arquivo()
    {
        Assert.Empty(Store.LoadNotifications());
        Assert.Empty(Store.LoadHistory());
        Assert.False(string.IsNullOrWhiteSpace(Store.LoadSettings().ThemeName));
    }

    [Fact]
    public void Arquivo_corrompido_nao_derruba_o_app()
    {
        var path = Path.Combine(Store.Folder, "notifications.json");
        File.WriteAllText(path, "{ isso não é json válido !!!");

        Assert.Empty(Store.LoadNotifications());
    }

    [Fact]
    public void Arquivo_gravado_em_utf8_sem_bom()
    {
        Store.SaveNotifications([new NotificationItem { Title = "Coração ção", Message = "Água, pão" }]);

        var bytes = File.ReadAllBytes(Path.Combine(Store.Folder, "notifications.json"));
        var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

        Assert.False(bom);
        var texto = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("Coração ção", texto);
        Assert.Equal("Coração ção", Store.LoadNotifications()[0].Title);
    }

    [Fact]
    public void Arquivo_antigo_em_minutos_migra_para_segundos()
    {
        var path = Path.Combine(Store.Folder, "notifications.json");
        File.WriteAllText(path, """
            [ { "Title": "Antiga", "IntervalMinutes": 45 } ]
            """);

        var loaded = Store.LoadNotifications();

        Assert.Equal(2700, loaded[0].IntervalSeconds);
    }

    [Fact]
    public void Arquivo_novo_nao_regrava_IntervalMinutes()
    {
        Store.SaveNotifications([new NotificationItem { IntervalSeconds = 45 }]);

        var texto = File.ReadAllText(Path.Combine(Store.Folder, "notifications.json"));

        Assert.Contains("IntervalSeconds", texto);
        Assert.DoesNotContain("IntervalMinutes", texto);
    }
}
