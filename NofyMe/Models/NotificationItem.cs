using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NofyMe.Models;

public enum DisplayMode
{
    Popup,
    Native,
    Balloon,
}

public enum TriggerType
{
    OnStartup,
    AfterTime,
    AtTime,
}

/// <summary>
/// Uma notificação criada pelo usuário. Um item pode ter 1..3 gatilhos:
/// ao iniciar o Windows, a cada X minutos/segundos, ou em horário fixo (por dia da semana).
/// </summary>
public class NotificationItem : INotifyPropertyChanged
{
    private string _title = "";
    private string _message = "";
    private DisplayMode _mode = DisplayMode.Popup;
    private bool _triggerOnStartup;
    private bool _triggerAfterTime;
    private bool _triggerAtTime;
    private int _intervalSeconds = 2700;
    private string _timeOfDay = "09:00";
    private List<int> _days = [];
    private bool _enabled = true;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    public DisplayMode Mode
    {
        get => _mode;
        set => Set(ref _mode, value);
    }

    // Gatilhos ativos para este item
    public bool TriggerOnStartup
    {
        get => _triggerOnStartup;
        set => Set(ref _triggerOnStartup, value);
    }

    public bool TriggerAfterTime
    {
        get => _triggerAfterTime;
        set => Set(ref _triggerAfterTime, value);
    }

    public bool TriggerAtTime
    {
        get => _triggerAtTime;
        set => Set(ref _triggerAtTime, value);
    }

    // Gatilho "após determinado tempo" (a cada X minutos ou segundos)
    public int IntervalSeconds
    {
        get => _intervalSeconds;
        set => Set(ref _intervalSeconds, value);
    }

    /// <summary>
    /// Compatibilidade: versões antigas gravavam "IntervalMinutes". Lê o valor
    /// antigo uma vez e converte para segundos; nunca é gravado de volta.
    /// </summary>
    [JsonPropertyName("IntervalMinutes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LegacyIntervalMinutes
    {
        get => 0;
        set
        {
            if (value > 0) IntervalSeconds = value * 60;
        }
    }

    // Gatilho "em determinado horário"
    public string TimeOfDay
    {
        get => _timeOfDay;
        set => Set(ref _timeOfDay, value);
    }

    /// <summary>Dias da semana: 0=Domingo, 1=Segunda ... 6=Sábado (vazio = todos).</summary>
    public List<int> Days
    {
        get => _days;
        set => Set(ref _days, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => Set(ref _enabled, value);
    }

    // Estado interno (persistido para evitar disparo duplicado no mesmo dia)
    public string? LastAtTimeDate { get; set; }

    // Próxima disparada do gatilho "a cada X" (tick desde o início do app)
    public DateTime? NextAfterTime { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        // chip e cartão dependem de várias propriedades: avisa o binding do item inteiro
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
