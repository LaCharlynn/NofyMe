namespace NofyMe.Services;

/// <summary>
/// Cálculo puro do redimensionamento da janela sem moldura.
/// </summary>
public static class WindowResizer
{
    public readonly record struct Bounds(double Left, double Top, double Width, double Height);

    /// <summary>
    /// Aplica o arrasto de uma alça. <paramref name="direction"/> usa
    /// "L", "R", "T", "B" e combinações ("TL", "BR"...).
    /// </summary>
    public static Bounds Apply(
        Bounds current, string direction, double dx, double dy,
        double minWidth, double minHeight, double maxWidth, double maxHeight)
    {
        var bounds = current;

        if (direction.Contains('L'))
        {
            var width = Math.Clamp(bounds.Width - dx, minWidth, maxWidth);
            bounds = bounds with { Left = bounds.Left + bounds.Width - width, Width = width };
        }
        else if (direction.Contains('R'))
        {
            bounds = bounds with { Width = Math.Clamp(bounds.Width + dx, minWidth, maxWidth) };
        }

        if (direction.Contains('T'))
        {
            var height = Math.Clamp(bounds.Height - dy, minHeight, maxHeight);
            bounds = bounds with { Top = bounds.Top + bounds.Height - height, Height = height };
        }
        else if (direction.Contains('B'))
        {
            bounds = bounds with { Height = Math.Clamp(bounds.Height + dy, minHeight, maxHeight) };
        }

        return bounds;
    }
}
