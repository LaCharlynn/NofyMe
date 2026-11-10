using System.Windows;
using System.Windows.Media;

namespace NofyMe.Services;

/// <summary>
/// Paletas tiradas do design (Interface/*.png + HTML de referência).
/// Nomes dos brushes usados nas telas com {DynamicResource}.
/// </summary>
public static class ThemeManager
{
    public static readonly string[] ThemeNames = ["Entardecer", "Café", "Floresta", "Pêssego"];

    public static void Apply(string themeName)
    {
        var dict = new ResourceDictionary();

        var (bg, panel, border, text, sub, faint, accent, accentSoft, active, input, offTrack, chipBg, onAccent, inputBorder) = themeName switch
        {
            "Café" => (
                bg: "#211A16", panel: "#2E241E", border: "#3A2B25", text: "#F6EBDD",
                sub: "#B9A594", faint: "#8A7668", accent: "#D98E5B", accentSoft: "#E8A579",
                active: "#3A2B25", input: "#3E342C", offTrack: "#8A92A0", chipBg: "#382E27",
                onAccent: "#2B1A10", inputBorder: "#6B5546"),
            "Floresta" => (
                bg: "#171D19", panel: "#212A24", border: "#2B3A31", text: "#E8F0E6",
                sub: "#9BAE9F", faint: "#6E8578", accent: "#8DB596", accentSoft: "#A7CBB0",
                active: "#22332A", input: "#1E2A23", offTrack: "#8A92A0", chipBg: "#22302A",
                onAccent: "#12201A", inputBorder: "#3D4F45"),
            "Pêssego" => (
                bg: "#FBF1E7", panel: "#FFF9F3", border: "#EAD9C7", text: "#3A2B25",
                sub: "#7A655B", faint: "#A69286", accent: "#B4472F", accentSoft: "#C85A41",
                active: "#F6E2D8", input: "#F6EEE3", offTrack: "#8A92A0", chipBg: "#F3E6D9",
                onAccent: "#FFFFFF", inputBorder: "#DEC6B0"),
            _ => ( // Entardecer
                bg: "#1B1A20", panel: "#25232C", border: "#34313D", text: "#F3EFF8",
                sub: "#C9C3D6", faint: "#A39CB4", accent: "#7C5CF0", accentSoft: "#B7A5FF",
                active: "#332C54", input: "#3E3A49", offTrack: "#8A92A0", chipBg: "#302D39",
                onAccent: "#FFFFFF", inputBorder: "#5A566A"),
        };

        dict["Bg"] = Brush(bg);
        dict["Panel"] = Brush(panel);
        dict["Border"] = Brush(border);
        dict["Text"] = Brush(text);
        dict["Sub"] = Brush(sub);
        dict["Faint"] = Brush(faint);
        dict["Accent"] = Brush(accent);
        dict["AccentSoft"] = Brush(accentSoft);
        dict["ActiveBg"] = Brush(active);
        dict["InputBg"] = Brush(input);
        dict["OffTrack"] = Brush(offTrack);
        dict["ChipBg"] = Brush(chipBg);
        dict["OnAccent"] = Brush(onAccent);
        dict["InputBorder"] = Brush(inputBorder);

        var merged = Application.Current.Resources.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i].Contains("Bg")) merged.RemoveAt(i);
        }
        merged.Add(dict);
    }

    private static SolidColorBrush Brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }
}
