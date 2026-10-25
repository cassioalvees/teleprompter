using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Arca.App.Controls;

/// <summary>
/// Aviso rápido do operador (velocidade, marcador, travas) — réplica do protótipo (Teleprompter.dc.html):
/// pílula rgba(20,20,22,.82), padding 7/14/7/8, espaço 10; rótulo de 20 px com seta, Sora 600 10 px espaçado;
/// texto Sora 600 13 px branco; barra 72×3. Fica a 76 px da base.
/// </summary>
public sealed class FlashToast : Border
{
    // Fonte carregada pelo pacote (e não dos recursos do app): a superfície também pode rodar em outra thread.
    private static readonly FontFamily Num = new(new Uri("pack://application:,,,/"), "./Fonts/#Sora");
    private static readonly Brush Card = Frozen(Color.FromArgb(209, 20, 20, 22));          // .82
    private static readonly Brush ChipFwd = Frozen(Color.FromArgb(41, 255, 255, 255));     // .16
    private static readonly Brush ChipRev = Frozen(Color.FromRgb(0xE5, 0x48, 0x4D));
    private static readonly Brush Track = Frozen(Color.FromArgb(51, 255, 255, 255));       // .2
    private static readonly Geometry Right = Frozen(new PathGeometry(new[] { new PathFigure(new Point(6, 3), new PathSegment[] { new LineSegment(new Point(12, 8), true), new LineSegment(new Point(6, 13), true) }, false) }));
    private static readonly Geometry Left = Frozen(new PathGeometry(new[] { new PathFigure(new Point(10, 3), new PathSegment[] { new LineSegment(new Point(4, 8), true), new LineSegment(new Point(10, 13), true) }, false) }));

    private readonly Border _chip = new() { Height = 20, Padding = new Thickness(9, 0, 9, 0), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly Path _arrow = new() { Width = 9, Height = 9, Stretch = Stretch.Uniform, Stroke = Brushes.White, StrokeThickness = 2.2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _chipText = new() { FontFamily = Num, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _text = new() { FontFamily = Num, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _track = new() { Width = 72, Height = 3, CornerRadius = new CornerRadius(2), Background = Track, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _fill = new() { HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(2), Background = Brushes.White };

    public FlashToast()
    {
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(16, 0, 16, 76);
        Capsule.Apply(this);
        Capsule.Apply(_chip);
        Background = Card;
        Padding = new Thickness(8, 7, 14, 7);
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        Typography.SetNumeralStyle(this, FontNumeralStyle.Lining);
        Typography.SetNumeralAlignment(this, FontNumeralAlignment.Tabular);
        var chip = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        chip.Children.Add(_arrow); chip.Children.Add(_chipText);
        _chip.Child = chip;
        _track.Child = _fill;
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(_chip); row.Children.Add(_text); row.Children.Add(_track);
        Child = row;
    }

    /// <summary>Mostra o aviso; <paramref name="sub"/> "Frente"/"Ré" vira o rótulo com seta; <paramref name="pct"/> &lt; 0 esconde a barra.</summary>
    public void Show(string? text, string? sub = null, double pct = -1)
    {
        if (string.IsNullOrEmpty(text)) { Visibility = Visibility.Collapsed; return; }
        Visibility = Visibility.Visible;
        _text.Text = text;
        var hasSub = !string.IsNullOrEmpty(sub);
        _chip.Visibility = hasSub ? Visibility.Visible : Visibility.Collapsed;
        // Sem letter-spacing no WPF: espaço fino entre as letras reproduz o 1,2 px do protótipo.
        _chipText.Text = string.Join(" ", (sub ?? "").ToUpperInvariant().ToCharArray());
        var rev = sub == "Ré";
        _chip.Background = rev ? ChipRev : ChipFwd;
        _arrow.Data = rev ? Left : Right;
        _track.Visibility = pct >= 0 ? Visibility.Visible : Visibility.Collapsed;
        _fill.Width = _track.Width * Math.Clamp(pct, 0, 1);
        Padding = hasSub ? new Thickness(8, 7, 14, 7) : new Thickness(14, 7, 14, 7);
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private static T Frozen<T>(T f) where T : Freezable { f.Freeze(); return f; }
    private static Brush Frozen(Color c) => Frozen(new SolidColorBrush(c));
}
