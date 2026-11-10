using NofyMe.Services;

namespace NofyMe.Tests;

public class WindowResizerTests
{
    private static WindowResizer.Bounds Base => new(100, 100, 960, 600);

    [Fact]
    public void Arrastar_a_direita_aumenta_a_largura()
    {
        var r = WindowResizer.Apply(Base, "R", 200, 0, 840, 560, 1920, 1080);

        Assert.Equal(1160, r.Width);
        Assert.Equal(100, r.Left);
    }

    [Fact]
    public void Arrastar_a_esquerda_move_a_janela_e_cresce()
    {
        var r = WindowResizer.Apply(Base, "L", -100, 0, 840, 560, 1920, 1080);

        Assert.Equal(1060, r.Width);
        Assert.Equal(0, r.Left);
    }

    [Fact]
    public void Canto_inferior_direito_muda_largura_e_altura()
    {
        var r = WindowResizer.Apply(Base, "BR", 40, 30, 840, 560, 1920, 1080);

        Assert.Equal(1000, r.Width);
        Assert.Equal(630, r.Height);
        Assert.Equal(100, r.Top);
    }

    [Fact]
    public void Respeita_o_tamanho_minimo()
    {
        var r = WindowResizer.Apply(Base, "R", -5000, 0, 840, 560, 1920, 1080);

        Assert.Equal(840, r.Width);
        Assert.Equal(100, r.Left);
    }

    [Fact]
    public void Respeita_o_limite_da_area_de_trabalho()
    {
        var r = WindowResizer.Apply(Base, "BR", 5000, 5000, 840, 560, 1920, 1080);

        Assert.Equal(1920, r.Width);
        Assert.Equal(1080, r.Height);
    }
}
