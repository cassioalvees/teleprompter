using System.Text.Json;
using Arca.Core.Model;

namespace Arca.Core.Tests;

/// <summary>2.10: cada Enter = uma linha — o espaço extra de fábrica entre parágrafos (0,25) vira 0 nas configurações antigas.</summary>
public class SpacingTests
{
    [Fact]
    public void NovoPadraoSemEspacoExtra() => Assert.Equal(0, new PrompterSettings().MigrateSpacing().ParagraphGap);

    [Fact]
    public void PadraoAntigoViraZero()
    {
        var s = JsonSerializer.Deserialize<PrompterSettings>("{\"paraGap\":0.25}")!.MigrateSpacing();
        Assert.Equal(0, s.ParagraphGap);
        Assert.Equal(1, s.SpacingRev);
    }

    [Fact]
    public void ValorEscolhidoFica() => Assert.Equal(0.5, JsonSerializer.Deserialize<PrompterSettings>("{\"paraGap\":0.5}")!.MigrateSpacing().ParagraphGap);

    [Fact]
    public void DepoisDaMigracao025Fica()
    {
        // Já na regra nova, 0,25 é escolha do usuário (régua "Parágrafos") e não muda mais.
        var s = JsonSerializer.Deserialize<PrompterSettings>("{\"paraGap\":0.25,\"spacingRev\":1}")!.MigrateSpacing();
        Assert.Equal(0.25, s.ParagraphGap);
    }
}
