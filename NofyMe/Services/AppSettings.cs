namespace NofyMe.Services;

public class AppSettings
{
    public string ThemeName { get; set; } = "Entardecer";
    public bool StartWithWindows { get; set; }
    /// <summary>Configuração "Notificar ao ligar": permite que gatilhos "Ao iniciar o Windows" disparem.</summary>
    public bool NotifyAtStartup { get; set; } = true;

    /// <summary>Segundos que o pop-up fica visível (1 a 60).</summary>
    public int PopupSeconds { get; set; } = 6;
}

/// <summary>Os quatro cantos da tela, usados pelo pop-up e pelo balão.</summary>
public static class ScreenCorners
{
    public const string TopLeft = "Superior esquerdo";
    public const string TopRight = "Superior direito";
    public const string BottomLeft = "Inferior esquerdo";
    public const string BottomRight = "Inferior direito";

    public static readonly string[] All = [TopLeft, TopRight, BottomLeft, BottomRight];

    public static string Normalize(string? value) =>
        value != null && All.Contains(value) ? value : BottomLeft;
}
