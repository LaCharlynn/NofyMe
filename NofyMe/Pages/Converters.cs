using System.Globalization;
using System.Windows.Data;
using NofyMe.Models;
using NofyMe.Services;

namespace NofyMe.Pages;

/// <summary>Converte um NotificationItem em chip de texto do gatilho.</summary>
public class ChipTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not NotificationItem item) return "";
        return ScheduleEvaluator.BuildChip(
            item.TriggerOnStartup, item.TriggerAfterTime, item.TriggerAtTime,
            item.IntervalSeconds, item.TimeOfDay, item.Days);
    }

    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

public class ModeNameConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is NotificationItem item ? Notifier.ModeName(item.Mode) : "";

    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}
