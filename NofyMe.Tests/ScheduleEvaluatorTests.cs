using NofyMe.Services;

namespace NofyMe.Tests;

public class ScheduleEvaluatorTests
{
    [Theory]
    [InlineData("09:00", 9, 0, true)]
    [InlineData("09:00", 8, 59, false)] // ainda não chegou
    [InlineData("09:00", 10, 30, true)] // já passou
    public void AtTime_dispara_quando_chega_a_hora(string slot, int hour, int minute, bool due)
    {
        var now = new DateTime(2026, 11, 4, hour, minute, 0); // quarta-feira
        Assert.Equal(due, ScheduleEvaluator.IsDueAtTime(now, slot, [3], null));
    }

    [Fact]
    public void AtTime_nao_dispara_duas_vezes_no_mesmo_dia()
    {
        var now = new DateTime(2026, 11, 4, 10, 0, 0);
        var disparouHoje = now.ToString("yyyy-MM-dd");
        Assert.False(ScheduleEvaluator.IsDueAtTime(now, "09:00", [3], disparouHoje));
    }

    [Fact]
    public void AtTime_respeita_o_dia_da_semana()
    {
        var quarta9h = new DateTime(2026, 11, 4, 9, 0, 0);
        Assert.False(ScheduleEvaluator.IsDueAtTime(quarta9h, "09:00", [1], null)); // só segunda
        Assert.True(ScheduleEvaluator.IsDueAtTime(quarta9h, "09:00", [3], null));  // quarta
    }

    [Fact]
    public void AtTime_dias_vazio_significa_todos_os_dias()
    {
        var quarta9h = new DateTime(2026, 11, 4, 9, 0, 0);
        Assert.True(ScheduleEvaluator.IsDueAtTime(quarta9h, "09:00", [], null));
    }

    [Theory]
    [InlineData(2700, 2700, true)]  // intervalo cumprido (45 min)
    [InlineData(2700, 2640, false)] // ainda dentro
    [InlineData(1, 1, true)]
    [InlineData(30, 30, true)]      // segundos
    public void AfterTime_vence_no_intervalo(int seconds, int elapsed, bool due)
    {
        var start = new DateTime(2026, 11, 4, 9, 0, 0);
        var now = start + TimeSpan.FromSeconds(elapsed);
        Assert.Equal(due, ScheduleEvaluator.IsDueAfterTime(now, seconds, start, out _));
    }

    [Fact]
    public void AfterTime_clampa_intervalo_para_o_minimo()
    {
        var start = new DateTime(2026, 11, 4, 9, 0, 0);
        // intervalo 0 não faz sentido: comporta-se como 1 s
        Assert.True(ScheduleEvaluator.IsDueAfterTime(start.AddSeconds(1), 0, start, out var next));
        Assert.Equal(start.AddSeconds(1), next);
    }

    [Fact]
    public void Chip_ao_iniciar_windows()
    {
        Assert.Equal("Ao iniciar o Windows",
            ScheduleEvaluator.BuildChip(true, false, false, 0, "09:00", []));
    }

    [Fact]
    public void Chip_intervalo()
    {
        Assert.Equal("A cada 45 min",
            ScheduleEvaluator.BuildChip(false, true, false, 2700, "09:00", []));
    }

    [Fact]
    public void Chip_intervalo_em_segundos()
    {
        Assert.Equal("A cada 30 s",
            ScheduleEvaluator.BuildChip(false, true, false, 30, "09:00", []));
    }

    [Fact]
    public void Chip_seg_sex()
    {
        Assert.Equal("Seg–Sex, 09:00",
            ScheduleEvaluator.BuildChip(false, false, true, 0, "09:00", [1, 2, 3, 4, 5]));
    }

    [Fact]
    public void Chip_todos_os_dias()
    {
        Assert.Equal("Todos os dias, 07:30",
            ScheduleEvaluator.BuildChip(false, false, true, 0, "07:30", [0, 1, 2, 3, 4, 5, 6]));
    }

    [Fact]
    public void Chip_combinando_gatilhos()
    {
        Assert.Equal("Ao iniciar o Windows · A cada 30 min · Seg, 09:00",
            ScheduleEvaluator.BuildChip(true, true, true, 1800, "09:00", [1]));
    }

    [Fact]
    public void Chip_fim_de_semana()
    {
        Assert.Equal("Fim de semana, 10:00",
            ScheduleEvaluator.BuildChip(false, false, true, 0, "10:00", [0, 6]));
    }

    [Fact]
    public void Chip_dia_unico()
    {
        Assert.Equal("Ter, 22:00",
            ScheduleEvaluator.BuildChip(false, false, true, 0, "22:00", [2]));
    }
}
