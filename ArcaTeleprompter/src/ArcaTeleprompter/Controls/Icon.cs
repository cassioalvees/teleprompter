using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Arca.App.Controls;

/// <summary>
/// Ícone de traço em viewBox 16×16 (como no protótipo). Usa a cor do texto herdada (TextElement.Foreground).
/// <c>Filled</c> preenche em vez de contornar (play, pausa).
/// </summary>
public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(string), typeof(Icon),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((Icon)d)._geo = null));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(double), typeof(Icon),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeWidthProperty = DependencyProperty.Register(nameof(StrokeWidth), typeof(double), typeof(Icon),
        new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty FilledProperty = DependencyProperty.Register(nameof(Filled), typeof(bool), typeof(Icon),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(Icon),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    private Geometry? _geo;

    public string Data { get => (string)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double StrokeWidth { get => (double)GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    public bool Filled { get => (bool)GetValue(FilledProperty); set => SetValue(FilledProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public Icon() { SnapsToDevicePixels = true; IsHitTestVisible = false; }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    protected override void OnRender(DrawingContext dc)
    {
        if (string.IsNullOrEmpty(Data)) return;
        try { _geo ??= Geometry.Parse(Data); } catch (FormatException) { return; }
        var k = Size / 16.0;
        dc.PushTransform(new ScaleTransform(k, k));
        if (Filled) dc.DrawGeometry(Foreground, null, _geo);
        else dc.DrawGeometry(null, new Pen(Foreground, StrokeWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }, _geo);
        dc.Pop();
    }
}

/// <summary>Caminhos dos ícones (viewBox 16×16), iguais aos do protótipo.</summary>
public static class Icons
{
    public const string New = "M4 1.5h5l3 3v10H4z M9 1.5v3h3";
    public const string Open = "M1.5 3.5h4.5l1.5 1.5h7v8h-13z";
    public const string Save = "M2.5 2.5h9l2 2v9h-11z M5 2.5v3h5v-3 M5 13.5v-4h6v4";
    public const string Undo = "M5 3.5L2 6.5l3 3 M2 6.5h8a3.5 3.5 0 0 1 0 7H7";
    public const string Redo = "M11 3.5l3 3-3 3 M14 6.5H6a3.5 3.5 0 0 0 0 7h3";
    public const string Find = "M7 2.5a4.5 4.5 0 1 0 0 9 4.5 4.5 0 0 0 0-9z M10.5 10.5l3.5 3.5";
    public const string Bookmark = "M4 1.5h8v13l-4-3-4 3z";
    public const string StopMark = "M2.5 2.5h11v11h-11z M6.5 5.5v5 M9.5 5.5v5";
    public const string Lock = "M3.5 7.5h9v7h-9z M5.5 7.5V5a2.5 2.5 0 0 1 5 0v2.5";
    public const string Unlock = "M3.5 7.5h9v7h-9z M5.5 7.5V5a2.5 2.5 0 0 1 4.9-.7";
    public const string FlipH = "M8 1.5v13 M6 4.5L2 11.5h4z M10 4.5l4 7h-4z";
    public const string FlipV = "M1.5 8h13 M4.5 6L11.5 2v4z M4.5 10l7 4v-4z";
    public const string AlignL = "M2 3.5h12 M2 6.5h8 M2 9.5h12 M2 12.5h7";
    public const string AlignC = "M2 3.5h12 M4 6.5h8 M2 9.5h12 M4.5 12.5h7";
    public const string AlignR = "M2 3.5h12 M6 6.5h8 M2 9.5h12 M6.5 12.5h7.5";
    public const string Controls = "M1.5 2.5h13v8h-13z M4 13.5h8";
    public const string Clock = "M8 1.5a6.5 6.5 0 1 0 0 13 6.5 6.5 0 0 0 0-13z M8 4.5V8l2.5 1.5";
    public const string Fullscreen = "M2 6V2h4 M10 2h4v4 M14 10v4h-4 M6 14H2v-4";
    public const string Target = "M8 1.5v3 M8 11.5v3 M1.5 8h3 M11.5 8h3 M8 5.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5z";
    public const string Play = "M4 2.5l9.5 5.5L4 13.5z";
    public const string Pause = "M3.5 2.5h3v11h-3z M9.5 2.5h3v11h-3z";
    public const string Stop = "M3 3h10v10H3z";
    public const string Rewind = "M3 3v10 M13 3L5.5 8 13 13z";
    public const string Back = "M8 3.5v9L1.5 8z M14.5 3.5v9L8 8z";
    public const string Forward = "M8 3.5v9L14.5 8z M1.5 3.5v9L8 8z";
    public const string Direction = "M5 2.5v11 M2.5 5L5 2.5 7.5 5 M11 13.5v-11 M8.5 11l2.5 2.5 2.5-2.5";
    public const string Up = "M3.5 10L8 5.5l4.5 4.5";
    public const string Down = "M3.5 6L8 10.5 12.5 6";
    public const string ParaUp = "M3.5 8L8 3.5 12.5 8 M3.5 12.5L8 8 12.5 12.5";
    public const string ParaDown = "M3.5 3.5L8 8 12.5 3.5 M3.5 8L8 12.5 12.5 8";
    public const string Edit = "M10.5 2.5l3 3-8 8h-3v-3z";
    public const string Close = "M3 3l10 10M13 3L3 13";
    public const string X = "M4 4l8 8M12 4l-8 8";
    public const string Plus = "M8 2.5v11M2.5 8h11";
    public const string Monitor = "M1.5 2.5h13v9h-13z M5.5 14.5h5 M8 11.5v3";
    public const string Eye = "M2 8s2.2-4.5 6-4.5S14 8 14 8s-2.2 4.5-6 4.5S2 8 2 8z M8 6.3a1.7 1.7 0 1 0 0 3.4a1.7 1.7 0 1 0 0-3.4";
    public const string EyeOff = "M2 8s2.2-4.5 6-4.5S14 8 14 8s-2.2 4.5-6 4.5S2 8 2 8z M2.5 2.5l11 11";
    public const string Chevron = "M4 6l4 4 4-4";
    public const string Check = "M3 8.5l3 3 7-7";
    public const string PauseMark = "M4 3h3v10H4z M9 3h3v10H9z";
}
