using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Arca.App.Controls;

/// <summary>
/// Seletor de cor no visual do app (substitui o seletor do Windows): quadro de saturação × brilho, faixa de matiz,
/// antes/depois, HEX e RGB, cores prontas para prompter e as últimas usadas. Usado por <see cref="ArcaDialog.PickColor"/>.
/// </summary>
public sealed class ColorPickerPanel : StackPanel
{
    /// <summary>Cores boas para leitura em fundo escuro e claro (texto, apresentadores, fundo).</summary>
    public static readonly string[] Presets =
    {
        "#ffffff", "#f2f2f2", "#cfcfcf", "#8a8a8a", "#3a3a3a", "#1e1e1e", "#000000", "#0a2a4a",
        "#ffd60a", "#ffb020", "#ff7a1a", "#ff4d4f", "#ff5fa2", "#b78cff", "#5e9eff", "#2ec5e6",
        "#20c997", "#6ccb5f", "#a3e635", "#f4e7c5"
    };

    // Compacto: cabe numa janela de ~360 px (o quadro, os campos e as bolinhas têm a mesma largura).
    private const double SvW = 280, SvH = 120;
    private double _h, _s, _v;
    private bool _sync;
    private readonly Rectangle _hueFill = new() { Width = SvW, Height = SvH, RadiusX = 8, RadiusY = 8 };
    private readonly Ellipse _svThumb = new() { Width = 16, Height = 16, Stroke = Brushes.White, StrokeThickness = 2, IsHitTestVisible = false, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.6 } };
    private readonly Border _hueThumb = new() { Width = 8, Height = 16, CornerRadius = new CornerRadius(3), BorderBrush = Brushes.White, BorderThickness = new Thickness(2), IsHitTestVisible = false, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.6 } };
    private readonly Canvas _sv = new() { Width = SvW, Height = SvH, Cursor = Cursors.Cross, ClipToBounds = false };
    private readonly Canvas _hue = new() { Width = SvW, Height = 12, Cursor = Cursors.Hand, Margin = new Thickness(0, 10, 0, 0) };
    private readonly Border _newSwatch = new() { Width = 24, Height = 30, CornerRadius = new CornerRadius(0, 7, 7, 0) };
    private readonly TextBox _hex = Field(76, 7), _r = Field(42, 3), _g = Field(42, 3), _b = Field(42, 3);

    /// <summary>Cor escolhida ("#rrggbb").</summary>
    public string Value { get; private set; }

    public ColorPickerPanel(string current, IReadOnlyList<string> recent)
    {
        Value = Normalize(current) ?? "#ffffff";

        // Quadro saturação (→) × brilho (↓) sobre a matiz.
        _sv.Children.Add(_hueFill);
        _sv.Children.Add(new Rectangle { Width = SvW, Height = SvH, RadiusX = 8, RadiusY = 8, Fill = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), 0) });
        _sv.Children.Add(new Rectangle { Width = SvW, Height = SvH, RadiusX = 8, RadiusY = 8, Fill = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, 90), Stroke = Res("Line"), StrokeThickness = 1 });
        _sv.Children.Add(_svThumb);
        Drag(_sv, p => { _s = Math.Clamp(p.X / SvW, 0, 1); _v = 1 - Math.Clamp(p.Y / SvH, 0, 1); FromHsv(); });
        Children.Add(_sv);

        // Faixa de matiz.
        var hueBrush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        for (var i = 0; i <= 6; i++) hueBrush.GradientStops.Add(new GradientStop(Hsv(i * 60 % 360, 1, 1), i / 6.0));
        _hue.Children.Add(new Rectangle { Width = SvW, Height = 12, RadiusX = 6, RadiusY = 6, Fill = hueBrush });
        Canvas.SetTop(_hueThumb, -2);
        _hue.Children.Add(_hueThumb);
        Drag(_hue, p => { _h = Math.Clamp(p.X / SvW, 0, 1) * 359.9; FromHsv(); });
        Children.Add(_hue);

        // Antes / depois + HEX e RGB.
        var row = new DockPanel { Margin = new Thickness(0, 12, 0, 0), LastChildFill = false };
        var cmp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
        var old = new Border { Width = 24, Height = 30, CornerRadius = new CornerRadius(7, 0, 0, 7), Background = Brush(Value), Cursor = Cursors.Hand, ToolTip = "Cor atual (clique para voltar a ela)" };
        var first = Value; old.MouseLeftButtonUp += (_, _) => Set(first);
        cmp.Children.Add(old); cmp.Children.Add(_newSwatch);
        var cmpBox = new Border { CornerRadius = new CornerRadius(7), BorderBrush = Res("Line"), BorderThickness = new Thickness(1), Child = cmp, VerticalAlignment = VerticalAlignment.Bottom };
        row.Children.Add(cmpBox);
        var fields = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
        DockPanel.SetDock(fields, Dock.Right);
        fields.Children.Add(Labeled("HEX", _hex));
        fields.Children.Add(Labeled("R", _r)); fields.Children.Add(Labeled("G", _g)); fields.Children.Add(Labeled("B", _b));
        row.Children.Add(fields);
        Children.Add(row);
        _hex.TextChanged += (_, _) => { if (!_sync && Normalize(_hex.Text) is { } v && _hex.Text.Trim().TrimStart('#').Length is 6) Set(v, keepHex: true); };
        _hex.LostFocus += (_, _) => { if (Normalize(_hex.Text) is { } v) Set(v); else Sync(); };
        foreach (var t in new[] { _r, _g, _b })
            t.TextChanged += (_, _) =>
            {
                if (_sync) return;
                if (byte.TryParse(_r.Text, out var r) && byte.TryParse(_g.Text, out var g) && byte.TryParse(_b.Text, out var b)) Set($"#{r:x2}{g:x2}{b:x2}", keepRgb: true);
            };

        // Cores prontas e últimas usadas.
        Children.Add(Caption("Cores prontas"));
        Children.Add(Swatches(Presets));
        var rec = recent.Select(Normalize).OfType<string>().Distinct().Take(10).ToList();
        if (rec.Count > 0) { Children.Add(Caption("Últimas usadas")); Children.Add(Swatches(rec)); }

        Set(Value);
    }

    // ——— Estado ———

    private void Set(string hex, bool keepHex = false, bool keepRgb = false)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        Value = $"#{c.R:x2}{c.G:x2}{c.B:x2}";
        ToHsv(c, out var h, out _s, out _v);
        if (_s > 0.001 && _v > 0.001) _h = h;   // cinza/preto: mantém a matiz atual
        Sync(keepHex, keepRgb);
    }

    private void FromHsv()
    {
        var c = Hsv(_h, _s, _v);
        Value = $"#{c.R:x2}{c.G:x2}{c.B:x2}";
        Sync();
    }

    private void Sync(bool keepHex = false, bool keepRgb = false)
    {
        _sync = true;
        var c = (Color)ColorConverter.ConvertFromString(Value);
        _hueFill.Fill = new SolidColorBrush(Hsv(_h, 1, 1));
        Canvas.SetLeft(_svThumb, _s * SvW - 8); Canvas.SetTop(_svThumb, (1 - _v) * SvH - 8);
        _svThumb.Stroke = _v > 0.55 && _s < 0.4 ? Brushes.Black : Brushes.White;
        Canvas.SetLeft(_hueThumb, _h / 360 * SvW - 4);
        _newSwatch.Background = new SolidColorBrush(c);
        if (!keepHex) _hex.Text = Value.ToUpperInvariant();
        if (!keepRgb) { _r.Text = c.R.ToString(); _g.Text = c.G.ToString(); _b.Text = c.B.ToString(); }
        _sync = false;
    }

    // ——— Peças ———

    private static void Drag(Canvas c, Action<Point> at)
    {
        c.Background = Brushes.Transparent;
        c.MouseLeftButtonDown += (_, e) => { c.CaptureMouse(); at(e.GetPosition(c)); e.Handled = true; };
        c.MouseMove += (_, e) => { if (c.IsMouseCaptured) at(e.GetPosition(c)); };
        c.MouseLeftButtonUp += (_, _) => c.ReleaseMouseCapture();
    }

    private WrapPanel Swatches(IEnumerable<string> colors)
    {
        var w = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        foreach (var hex in colors)
        {
            var b = new Border
            {
                Width = 22, Height = 22, Margin = new Thickness(0, 0, 5, 5), CornerRadius = new CornerRadius(6), Background = Brush(hex),
                BorderBrush = Res("Line"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, ToolTip = hex.ToUpperInvariant()
            };
            var v = hex; b.MouseLeftButtonUp += (_, _) => Set(v);
            w.Children.Add(b);
        }
        return w;
    }

    private static TextBlock Caption(string t) => new()
    {
        Text = t.ToUpperInvariant(), Style = (Style)Application.Current.FindResource("Eyebrow"), Foreground = Res("Muted"), Margin = new Thickness(0, 12, 0, 0)
    };

    private static StackPanel Labeled(string label, TextBox tb)
    {
        var sp = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = Res("Muted"), Margin = new Thickness(2, 0, 0, 4) });
        sp.Children.Add(tb);
        return sp;
    }

    private static TextBox Field(double w, int max) => new()
    {
        Style = (Style)Application.Current.FindResource("FieldSmall"), Width = w, MaxLength = max, HorizontalContentAlignment = HorizontalAlignment.Center
    };

    private static Brush Res(string key) => Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    private static Brush Brush(string hex) { try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); } catch { return Brushes.Transparent; } }

    // ——— Cor ———

    /// <summary>"#abc", "abc", "#aabbcc" ou "#ffaabbcc" → "#aabbcc"; null se inválido.</summary>
    public static string? Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().TrimStart('#');
        if (t.Length == 3) t = string.Concat(t.Select(ch => $"{ch}{ch}"));
        if (t.Length == 8) t = t[2..];
        return t.Length == 6 && int.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _) ? "#" + t.ToLowerInvariant() : null;
    }

    private static Color Hsv(double h, double s, double v)
    {
        h = (h % 360 + 360) % 360;
        var c = v * s; var x = c * (1 - Math.Abs(h / 60 % 2 - 1)); var m = v - c;
        var (r, g, b) = h switch { < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x), < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x) };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    private static void ToHsv(Color c, out double h, out double s, out double v)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var d = max - min;
        h = d == 0 ? 0 : max == r ? 60 * ((g - b) / d % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        if (h < 0) h += 360;
        s = max == 0 ? 0 : d / max; v = max;
    }
}
