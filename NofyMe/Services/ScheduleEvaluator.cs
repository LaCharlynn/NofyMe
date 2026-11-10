using NofyMe.Models;

namespace NofyMe.Services;

/// <summary>
/// Lógica pura dos gatilhos, sem dependência de interface — testável.
/// </summary>
public static class ScheduleEvaluator
{
    private static readonly string[] DayShort = { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };

    /// <summary>O gatilho "em determinado horário" está vencido AGORA?</summary>
    public static bool IsDueAtTime(
        DateTime now,
        string timeOfDay,
        IReadOnlyList<int> days,
        string? lastAtTimeDate)
    {
        if (!TimeSpan.TryParse(timeOfDay, out var slot)) return false;
        if (now.TimeOfDay < slot) return false;

        var today = now.ToString("yyyy-MM-dd");
        if (lastAtTimeDate == today) return false; // já disparou hoje

        if (days.Count == 0) return true; // vazio = todos os dias
        return days.Contains((int)now.DayOfWeek);
    }

    /// <summary>O gatilho "a cada X" está vencido? (intervalo relativo, recomeça ao abrir o app)</summary>
    public static bool IsDueAfterTime(DateTime now, int intervalSeconds, DateTime appStartTime, out DateTime nextAt)
    {
        const int min = 1, max = 86400; // 1 segundo até 24 horas
        var clamped = Math.Clamp(intervalSeconds, min, max);

        nextAt = appStartTime + TimeSpan.FromSeconds(clamped);
        return now >= nextAt;
    }

    /// <summary>Etiqueta do gatilho (chip do card): "Ao iniciar o Windows", "A cada 45 min", "Seg–Sex, 09:00"...</summary>
    public static string BuildChip(
        bool onStartup, bool afterTime, bool atTime,
        int intervalSeconds, string timeOfDay, IReadOnlyList<int> days,
        IntervalUnit unit = IntervalUnit.Minutes)
    {
        var parts = new List<string>();
        if (onStartup) parts.Add("Ao iniciar o Windows");
        if (afterTime) parts.Add($"A cada {DescribeInterval(intervalSeconds, unit)}");
        if (atTime)
        {
            var label = DescribeDays(days);
            parts.Add(label.Length > 0 ? $"{label}, {timeOfDay}" : timeOfDay);
        }
        return string.Join(" · ", parts);
    }

    /// <summary>"45 min" para minutos; "90 s" quando o usuário escolheu segundos.</summary>
    public static string DescribeInterval(int seconds, IntervalUnit unit = IntervalUnit.Minutes)
    {
        var s = Math.Clamp(seconds, 1, 86400);
        if (unit == IntervalUnit.Seconds) return $"{s} s";
        return s % 60 == 0 ? $"{s / 60} min" : $"{s} s";
    }

    public static string DescribeDays(IReadOnlyList<int> days)
    {
        if (days.Count == 0) return "Todos os dias";

        var inset = days.Distinct().OrderBy(d => d).ToHashSet();
        if (inset.Count == 7) return "Todos os dias";

        var weekdays = new HashSet<int> { 1, 2, 3, 4, 5 };
        var weekend = new HashSet<int> { 0, 6 };

        if (inset.SetEquals(weekdays)) return "Seg–Sex";
        if (inset.SetEquals(weekend)) return "Fim de semana";
        if (inset.Count == 1) return DayShort[inset.First()];

        return string.Join(" ", inset.Select(d => DayShort[d]));
    }
}
