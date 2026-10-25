using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Arca.App.Editor;
using Arca.Core.Engine;
using Arca.Core.Model;

namespace Arca.App.Controls;

/// <summary>
/// Camadas fixas sobre o texto: faixa de leitura / degradê / destaque, linha de leitura e indicador
/// (seta, quadrado, ponto ou linha). Posições em pixels a partir da largura (escala k = largura/1000).
/// </summary>
public sealed class PrompterOverlay : Canvas
{
    private readonly Rectangle _dimTop = new(), _dimBottom = new(), _hi = new(), _readLine = new() { Height = 2, Opacity = 0.35 };
    private readonly Path _ind = new();
    private PrompterSettings? _s; private PrompterLayout? _l;

    public PrompterOverlay()
    {
        IsHitTestVisible = false; ClipToBounds = true;
        Children.Add(_readLine); Children.Add(_dimTop); Children.Add(_dimBottom); Children.Add(_hi); Children.Add(_ind);
        SizeChanged += (_, _) => Update();
    }

    /// <summary>Margem direita reservada (ex.: barra de rolagem do editor).</summary>
    public double RightInset { get; set; }

    public void Apply(PrompterSettings s, PrompterLayout l) { _s = s; _l = l; Update(); }

    public void Update()
    {
        if (_s == null || _l == null) return;
        var s = _s; var l = _l;
        double w = Math.Max(0, ActualWidth - RightInset), h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var k = (ActualWidth - RightInset) / PrompterLayout.Width;
        var top = h * s.ReadPct / 100; var lh = l.LineHeightPx * k;
        var bg = ScriptFlow.ParseColor(s.Background, Colors.Black);
        var dim = ScriptFlow.WithAlpha(bg, s.OverlayOpacity);
        var ind = ScriptFlow.ParseColor(s.IndicatorColor, Colors.White);

        _dimTop.Visibility = _dimBottom.Visibility = s.OverlayType is "faixa" or "fade" ? Visibility.Visible : Visibility.Collapsed;
        Place(_dimTop, 0, 0, w, top);
        Place(_dimBottom, 0, top + lh, w, Math.Max(0, h - top - lh));
        if (s.OverlayType == "fade")
        {
            _dimTop.Fill = new LinearGradientBrush(Color.FromArgb((byte)(s.OverlayOpacity * 255), bg.R, bg.G, bg.B), Color.FromArgb(0, bg.R, bg.G, bg.B), 90);
            _dimBottom.Fill = new LinearGradientBrush(Color.FromArgb(0, bg.R, bg.G, bg.B), Color.FromArgb((byte)(s.OverlayOpacity * 255), bg.R, bg.G, bg.B), 90);
        }
        else { _dimTop.Fill = dim; _dimBottom.Fill = dim; }
        _hi.Visibility = s.OverlayType == "destaque" ? Visibility.Visible : Visibility.Collapsed;
        _hi.Fill = ScriptFlow.WithAlpha(ind, s.OverlayOpacity * 0.3);
        Place(_hi, 0, top, w, lh);
        _readLine.Visibility = s.ReadingLine ? Visibility.Visible : Visibility.Collapsed;
        _readLine.Fill = new SolidColorBrush(ind);
        Place(_readLine, 0, top + lh / 2 - 1, w, 2);

        var (il, iw, ih) = l.Indicator(s.IndicatorType);
        _ind.Visibility = s.IndicatorType == "nenhum" || iw <= 0 ? Visibility.Collapsed : Visibility.Visible;
        iw *= k; ih *= k; il *= k;
        _ind.Fill = new SolidColorBrush(ind);
        _ind.Data = s.IndicatorType switch
        {
            "seta" => new PathGeometry(new[] { new PathFigure(new Point(0, 0), new PathSegment[] { new LineSegment(new Point(iw, ih / 2), true), new LineSegment(new Point(0, ih), true) }, true) }),
            "ponto" => new EllipseGeometry(new Rect(0, 0, iw, ih)),
            "quadrado" => new RectangleGeometry(new Rect(0, 0, iw, ih), 2, 2),
            _ => new RectangleGeometry(new Rect(0, 0, iw, ih), 1, 1)
        };
        SetLeft(_ind, il); SetTop(_ind, top + lh / 2 - ih / 2);
    }

    private static void Place(FrameworkElement e, double x, double y, double w, double h)
    {
        SetLeft(e, x); SetTop(e, y); e.Width = Math.Max(0, w); e.Height = Math.Max(0, h);
    }
}
