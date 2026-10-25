using Arca.Core.Model;

namespace Arca.Core.Engine;

/// <summary>
/// Diagramação no "espaço de 1000 unidades": todo o conteúdo do prompter (editor, janelas, monitor web,
/// NDI) é diagramado com 1000 px de largura e escalado para a saída, o que garante quebra de linha
/// idêntica em todas as telas. Fórmulas do README do protótipo (§5.5).
/// </summary>
public sealed class PrompterLayout
{
    public const double Width = 1000;

    public double FontSize { get; init; }
    public double PaddingLeft { get; init; }
    public double PaddingRight { get; init; }
    public double LineHeightPx { get; init; }
    /// <summary>Fator k do indicador (seta/quadrado/ponto) em unidades de 1000.</summary>
    public double IndicatorK { get; init; }

    /// <summary>Fatores de largura das fontes empacotadas (Atkinson = 1).</summary>
    public static double BundledFontFactor(string key) => key switch { "sans" => 1.1, "serif" => 1.02, "mono" => 0.82, _ => 1.0 };

    private static readonly Dictionary<string, double> IndicatorWidth = new() { ["seta"] = 1.8, ["quadrado"] = 1.7, ["ponto"] = 1.8 };
    private static readonly double[] SizeMul = { 0.65, 1, 1.45 };

    /// <param name="fontFactor">Fator de largura da fonte (para fontes do sistema, medido pelo app).</param>
    public static PrompterLayout Compute(PrompterSettings s, double fontFactor)
    {
        var d = s.WordsPerLine * 3.05 * fontFactor;
        var right = 24 + Math.Round(s.Margin * (1 - s.MarginPos) / 2);
        // Primeiro uma estimativa da fonte para dimensionar a reserva do indicador.
        var guess = (Width - 48 - s.Margin) / d;
        var left = Left(s, guess);
        var fs = Math.Round((Width - right - left) / d);
        left = Left(s, fs);
        return new PrompterLayout
        {
            FontSize = fs,
            PaddingLeft = left,
            PaddingRight = right,
            LineHeightPx = fs * s.LineHeight,
            IndicatorK = fs * 0.028 * SizeMul[Math.Clamp(s.IndicatorSize, 0, 2)]
        };
    }

    /// <summary>
    /// Margem esquerda efetiva = reserva do indicador + parte da margem conforme a posição.
    /// O texto nunca fica sob a seta.
    /// </summary>
    private static double Left(PrompterSettings s, double fs)
    {
        double ind = 24;
        if (IndicatorWidth.TryGetValue(s.IndicatorType, out var w))
        {
            var k = fs * 0.028 * SizeMul[Math.Clamp(s.IndicatorSize, 0, 2)];
            ind = Math.Round((k * 0.4 + w * k) * 10 + 14);
        }
        return ind + Math.Round(s.Margin * (1 + s.MarginPos) / 2);
    }

    /// <summary>Indicador em unidades de 1000: (esquerda, largura, altura).</summary>
    public (double Left, double W, double H) Indicator(string type) => type switch
    {
        "seta" => (IndicatorK * 0.4 * 10, 1.8 * IndicatorK * 10, 2.4 * IndicatorK * 10),
        "quadrado" => (IndicatorK * 0.4 * 10, 1.7 * IndicatorK * 10, 1.7 * IndicatorK * 10),
        "ponto" => (IndicatorK * 0.4 * 10, 1.8 * IndicatorK * 10, 1.8 * IndicatorK * 10),
        "linha" => (0, 4 * IndicatorK * 10, 0.5 * IndicatorK * 10),
        _ => (0, 0, 0)
    };
}
