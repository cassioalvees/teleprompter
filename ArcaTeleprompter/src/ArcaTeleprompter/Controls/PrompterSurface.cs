using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arca.App.Editor;
using Arca.Core.Engine;
using Arca.Core.Model;

namespace Arca.App.Controls;

/// <summary>Posição de um bloco no espaço de 1000 unidades.</summary>
public readonly record struct BlockBox(int Block, double Top, double Height);

/// <summary>
/// Uma saída do prompter (janela do talento, telas externas, prévia, monitor do editor, NDI).
/// Diagrama o roteiro com 1000 px de largura e escala para o tamanho real; a rolagem é só uma
/// transformação (GPU), atualizada a cada quadro.
/// </summary>
public sealed class PrompterSurface : Grid
{
    private readonly Border _bgImage = new() { IsHitTestVisible = false };
    private readonly Grid _flip = new() { RenderTransformOrigin = new Point(0.5, 0.5) };
    private readonly Canvas _scroller = new() { IsHitTestVisible = false };
    private readonly RichTextBox _text;
    private readonly Canvas _names = new() { IsHitTestVisible = false };
    private readonly ScaleTransform _scale = new();
    private readonly TranslateTransform _translate = new();
    private readonly PrompterOverlay _overlay = new();
    private readonly Border _border = new() { BorderThickness = new Thickness(6), IsHitTestVisible = false, Opacity = 0 };
    // HUD
    private readonly Grid _hud = new() { Margin = new Thickness(20, 60, 20, 0), VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false };
    private readonly TextBlock _hudBlock = new(), _hudLeft = new(), _hudClock = new();
    private readonly Border _hudLast = new();
    private readonly FlashToast _flash = new();
    private readonly Border _count = new() { Visibility = Visibility.Collapsed, IsHitTestVisible = false };
    private readonly TextBlock _countText = new();

    private PrompterSettings _s = new();
    private PrompterLayout _l = PrompterLayout.Compute(new PrompterSettings(), 1);
    private List<(int Block, int Who)> _whoStarts = new();
    private bool _mapDirty = true;
    private List<BlockBox> _map = new();
    private double _y;
    private Thickness _pad;

    public PrompterSurface()
    {
        ClipToBounds = true;
        // Rolagem em subpixel: sem arredondar posições para o pixel inteiro (a ~0,8 px por quadro isso vira
        // "parado / pula 1 px" — trancos). O texto já é desenhado com suavização.
        SnapsToDevicePixels = false;
        UseLayoutRounding = false;
        // Texto grande e escalado: modo Ideal em toda a superfície (nomes, marcadores, overlay). O modo Display da
        // janela encaixa as letras na grade de pixels no tamanho original e deforma o texto depois da escala.
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);
        _text = new RichTextBox
        {
            IsReadOnly = true, IsHitTestVisible = false, Focusable = false, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, Padding = new Thickness(0), Width = PrompterLayout.Width,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, IsUndoEnabled = false, IsDocumentEnabled = false
        };
        TextOptions.SetTextFormattingMode(_text, TextFormattingMode.Ideal);
        TextOptions.SetTextRenderingMode(_text, TextRenderingMode.Grayscale);
        _scroller.Children.Add(_text);
        _scroller.Children.Add(_names);
        var tg = new TransformGroup(); tg.Children.Add(_translate); tg.Children.Add(_scale);
        _scroller.RenderTransform = tg;
        _flip.Children.Add(_scroller);
        _flip.Children.Add(_overlay);
        BuildHud();
        _flip.Children.Add(_hudZone);
        _flip.Children.Add(_hud);
        _flip.Children.Add(_flash);
        _flip.Children.Add(_count);
        Children.Add(_bgImage);
        Children.Add(_flip);
        Children.Add(_border);
        _text.SizeChanged += (_, _) => { _mapDirty = true; LayoutChanged?.Invoke(); };
        _text.Loaded += (_, _) => { if (_text.Document is { } d && d.PagePadding != _pad) { d.PagePadding = _pad; _mapDirty = true; } };
        SizeChanged += (_, _) => { Apply(); LayoutHud(); };
    }

    /// <summary>A diagramação mudou (posições dos blocos precisam ser recalculadas).</summary>
    public event Action? LayoutChanged;

    public bool FlipH { get => _flipH; set { _flipH = value; ApplyFlip(); } }
    public bool FlipV { get => _flipV; set { _flipV = value; ApplyFlip(); } }
    private bool _flipH, _flipV;
    private void ApplyFlip() => _flip.RenderTransform = new ScaleTransform(_flipH ? -1 : 1, _flipV ? -1 : 1);

    /// <summary>Mostra o HUD do talento (bloco, tempo, relógio, velocidade, contagem).</summary>
    public bool ShowHud
    {
        get => _hud.Visibility == Visibility.Visible;
        set { _hud.Visibility = value ? Visibility.Visible : Visibility.Collapsed; LayoutHud(); }
    }
    public bool ShowFlashAndCount { get; set; } = true;
    public RichTextBox TextBox => _text;
    public FlowDocument? Document => _text.Document;

    /// <summary>Troca o conteúdo (FlowDocument novo, construído para esta saída).</summary>
    public void SetContent(FlowDocument fd, List<(int Block, int Who)> whoStarts, PrompterSettings s, PrompterLayout l)
    {
        _s = s; _l = l; _whoStarts = whoStarts;
        _pad = fd.PagePadding;
        _text.Document = fd;
        // O RichTextBox redefine a margem do documento ao recebê-lo e ao carregar: reaplica (indicador + margem).
        fd.PagePadding = _pad;
        _mapDirty = true;
        Background = ScriptFlow.ParseBrush(s.Background, Brushes.Black);
        _overlay.Apply(s, l);
        LayoutHud();
        Dispatcher.BeginInvoke(new Action(DrawNames), System.Windows.Threading.DispatcherPriority.Loaded);
        Apply();
    }

    /// <summary>Atualiza só a aparência (cores, overlay, indicador) sem trocar o texto.</summary>
    public void SetLook(PrompterSettings s, PrompterLayout l)
    {
        _s = s; _l = l;
        Background = ScriptFlow.ParseBrush(s.Background, Brushes.Black);
        _overlay.Apply(s, l);
        LayoutHud();
        Apply();
    }

    public void SetBackgroundImage(ImageSource? img, double opacity)
    {
        _bgImage.Background = img == null ? null : new ImageBrush(img) { Stretch = Stretch.UniformToFill };
        _bgImage.Opacity = opacity;
    }

    /// <summary>Alinhamento específico desta tela (sobrepõe o do roteiro).</summary>
    public string? AlignOverride { get; set; }

    /// <summary>Posição de leitura no espaço de 1000 unidades.</summary>
    public double Y { get => _y; set { _y = value; Apply(); } }

    private void Apply()
    {
        if (_text.Document is { } d && d.PagePadding != _pad) { d.PagePadding = _pad; _mapDirty = true; }
        var w = ActualWidth; if (w <= 0) return;
        var k = w / PrompterLayout.Width;
        _scale.ScaleX = _scale.ScaleY = k;
        _translate.Y = Math.Round(ActualHeight / k * _s.ReadPct / 100 - _y, 2);
    }

    /// <summary>Destaque de "localizar tela": borda vermelha piscando.</summary>
    public void Blink(bool on) { _border.BorderBrush = Brushes.Red; _border.Opacity = on ? 1 : 0; }

    // ——— Mapa de blocos ———

    /// <summary>Topo e altura de cada bloco visível (unidades de 1000), em ordem.</summary>
    public IReadOnlyList<BlockBox> Map { get { if (_mapDirty) BuildMap(); return _map; } }

    /// <summary>Altura total do texto.</summary>
    public double ContentHeight => _text.ActualHeight;

    private void BuildMap()
    {
        _map = new();
        var fd = _text.Document;
        if (fd == null || _text.ActualHeight <= 0) return;
        _text.UpdateLayout();
        var tops = new List<(int Block, double Top)>();
        foreach (var b in fd.Blocks)
        {
            if (b.Tag is not int idx) continue;
            var r = b.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            if (r.IsEmpty) continue;
            // Topo da caixa do bloco (inclui borda e preenchimento, como offsetTop no protótipo).
            var top = r.Top - b.Padding.Top - b.BorderThickness.Top;
            tops.Add((idx, top));
        }
        for (var i = 0; i < tops.Count; i++)
        {
            var next = i + 1 < tops.Count ? tops[i + 1].Top : _text.ActualHeight;
            _map.Add(new BlockBox(tops[i].Block, tops[i].Top, Math.Max(1, next - tops[i].Top)));
        }
        _mapDirty = false;
    }

    public void InvalidateMap() { _mapDirty = true; }

    /// <summary>Âncora (bloco, fração) da posição y.</summary>
    public (int Block, double Frac) AnchorAt(double y)
    {
        var m = Map; if (m.Count == 0) return (0, 0);
        var i = 0;
        for (var k = 0; k < m.Count; k++) { if (m[k].Top <= y + 0.01) i = k; else break; }
        var b = m[i];
        return (b.Block, Math.Clamp((y - b.Top) / b.Height, 0, 1));
    }

    /// <summary>Posição y de uma âncora. Bloco ausente (pulado/excluído): usa o próximo bloco visível.</summary>
    public double YOf(int block, double frac)
    {
        var m = Map; if (m.Count == 0) return 0;
        var b = m.FirstOrDefault(x => x.Block >= block);
        if (b.Height == 0) b = m[^1];
        return b.Block == block ? b.Top + frac * b.Height : b.Top;
    }

    public double TopOf(int block) => Map.FirstOrDefault(x => x.Block == block) is { Height: > 0 } b ? b.Top : double.NaN;

    /// <summary>Y da palavra (bloco + posição do caractere), para o acompanhamento por voz.</summary>
    public double WordY(int block, int charIndex)
    {
        var fd = _text.Document;
        var para = fd?.Blocks.OfType<Paragraph>().FirstOrDefault(p => p.Tag is int i && i == block);
        if (para == null) return double.NaN;
        var tp = PointerAt(para, charIndex);
        var r = tp.GetCharacterRect(LogicalDirection.Forward);
        return r.IsEmpty ? double.NaN : r.Top;
    }

    /// <summary>
    /// Posição de leitura contínua para o acompanhamento por voz: em vez de saltar linha a linha, avança
    /// conforme o quanto da linha já foi lido (fim da palavra ouvida). No fim de um parágrafo a primeira
    /// linha do próximo já chega à linha de leitura — o olhar não desce para buscar o texto.
    /// <paramref name="lead"/> antecipa a rolagem em frações de linha (1 = a linha lida fica uma acima da seta).
    /// </summary>
    public double ReadingY(int block, int charIndex, int length, double lead, Func<int, bool>? isTextBlock = null)
    {
        // Próxima linha de texto (pula título de marcador e aviso de pausa).
        TextPointer? NextLine(TextPointer from)
        {
            var p = from.GetLineStartPosition(1, out var m);
            for (var g = 0; g < 8 && p != null && m == 1 && isTextBlock != null && p.Paragraph?.Tag is int ix && !isTextBlock(ix); g++)
                p = p.GetLineStartPosition(1, out m);
            return p != null && m == 1 ? p : null;
        }
        var fd = _text.Document;
        var para = fd?.Blocks.OfType<Paragraph>().FirstOrDefault(p => p.Tag is int i && i == block);
        if (para == null) return double.NaN;
        var start = PointerAt(para, charIndex);
        var rs = start.GetCharacterRect(LogicalDirection.Forward);
        if (rs.IsEmpty) return double.NaN;
        var lh = rs.Height > 0 ? rs.Height : 40;
        var ls = start.GetLineStartPosition(0) ?? start;
        var t0 = ls.GetCharacterRect(LogicalDirection.Forward).Top;
        var n1 = NextLine(ls);
        var has1 = n1 != null;
        var t1 = has1 ? n1!.GetCharacterRect(LogicalDirection.Forward).Top : t0 + lh;
        var n2 = has1 ? NextLine(n1!) : null;
        var t2 = n2 != null ? n2.GetCharacterRect(LogicalDirection.Forward).Top : t1 + lh;
        // Progresso horizontal na linha: do início da linha até o fim da palavra ouvida.
        var x0 = ls.GetCharacterRect(LogicalDirection.Forward).Left;
        // Fim da linha atual: início da linha seguinte no documento (mesmo que seja um marcador), um passo para trás.
        var after = ls.GetLineStartPosition(1, out var ma);
        var lineEnd = (after != null && ma == 1 ? after : para.ContentEnd).GetNextInsertionPosition(LogicalDirection.Backward) ?? para.ContentEnd;
        var x1 = lineEnd.GetCharacterRect(LogicalDirection.Backward).Right;
        var xe = PointerAt(para, charIndex + Math.Max(1, length)).GetCharacterRect(LogicalDirection.Backward).Right;
        var f = x1 - x0 > 1 ? Math.Clamp((xe - x0) / (x1 - x0), 0, 1) : 1;
        // A linha falada fica centrada na seta enquanto é lida; perto do fim desliza (curva suave) até a próxima
        // linha de texto — também entre parágrafos. A antecipação adianta o começo desse deslize.
        var glide = Math.Clamp(0.8 - lead * 0.6, 0.2, 0.92);
        var u = Math.Clamp((f - glide) / (1 - glide), 0, 1);
        var k = u * u * (3 - 2 * u);
        _ = t2;
        return t0 + k * (t1 - t0);
    }

    /// <summary>
    /// Topo da linha de texto na posição <paramref name="y"/> (<paramref name="step"/> = 0), da próxima (+1)
    /// ou da anterior (−1). Usado para a linha de leitura sempre cair sobre uma linha do texto.
    /// </summary>
    public double LineTop(double y, int step)
    {
        var tb = _text; var fd = tb.Document;
        if (fd == null || tb.ActualHeight <= 0) return double.NaN;
        var p = tb.GetPositionFromPoint(new Point(_pad.Left + 2, Math.Clamp(y + 1, 0, tb.ActualHeight - 1)), true);
        if (p == null) return double.NaN;
        var cur = p.GetLineStartPosition(0) ?? p;
        var curTop = cur.GetCharacterRect(LogicalDirection.Forward).Top;
        if (step == 0) return curTop;
        if (step < 0 && y > curTop + 1) return curTop;
        var other = cur.GetLineStartPosition(step, out var moved);
        if (other == null || moved == 0) return double.NaN;
        var r = other.GetCharacterRect(LogicalDirection.Forward);
        return r.IsEmpty ? double.NaN : r.Top;
    }

    /// <summary>Ponteiro no caractere <paramref name="charIndex"/> do texto do parágrafo.</summary>
    public static TextPointer PointerAt(Paragraph p, int charIndex)
    {
        var nav = p.ContentStart; var left = charIndex;
        while (nav != null && nav.CompareTo(p.ContentEnd) < 0)
        {
            if (nav.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var run = nav.GetTextInRun(LogicalDirection.Forward);
                if (left <= run.Length) return nav.GetPositionAtOffset(left) ?? nav;
                left -= run.Length;
            }
            nav = nav.GetNextContextPosition(LogicalDirection.Forward);
        }
        return p.ContentEnd;
    }

    /// <summary>Nome do apresentador acima do primeiro parágrafo de cada fala.</summary>
    private void DrawNames()
    {
        _names.Children.Clear();
        if (!_s.PresenterNames || _whoStarts.Count == 0) return;
        var fs = _l.FontSize;
        foreach (var (block, who) in _whoStarts)
        {
            var top = TopOf(block); if (double.IsNaN(top) || who < 1 || who > _s.Presenters.Count) continue;
            var p = _s.Presenters[who - 1];
            var tb = new TextBlock
            {
                Text = (string.IsNullOrWhiteSpace(p.Name) ? "Apresentador " + who : p.Name).ToUpperInvariant(),
                FontFamily = FontCatalog.Family("mono"), FontSize = fs * 0.3, Opacity = 0.85,
                Foreground = ScriptFlow.ParseBrush(p.Color, Brushes.White), TextTrimming = TextTrimming.CharacterEllipsis,
                Width = PrompterLayout.Width - _l.PaddingLeft - _l.PaddingRight, TextAlignment = Spacing.Align(_s.Align)
            };
            Canvas.SetLeft(tb, _l.PaddingLeft);
            Canvas.SetTop(tb, top - Spacing.NameOffset(fs));
            _names.Children.Add(tb);
        }
    }

    // ——— HUD do talento ———

    private void BuildHud()
    {
        // Fontes criadas aqui (e não lidas dos recursos do app): a superfície também roda na thread do NDI.
        var mono = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Sora");
        var sans = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Raleway");
        Border Pill(UIElement child) => new() { Background = new SolidColorBrush(Color.FromArgb(214, 16, 16, 18)), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 6, 12, 6), Child = child };
        _hudLast.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D)); Capsule.Apply(_hudLast); _hudLast.Padding = new Thickness(8, 3, 8, 3);
        _hudLast.Child = _hudLastText = new TextBlock { Text = "ÚLTIMO BLOCO", FontFamily = mono, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
        _hudLast.Margin = new Thickness(0, 0, 10, 0);
        _hudBlock.FontFamily = sans; _hudBlock.FontSize = 13; _hudBlock.FontWeight = FontWeights.SemiBold; _hudBlock.Foreground = Brushes.White; _hudBlock.VerticalAlignment = VerticalAlignment.Center;
        _hudLeft.FontFamily = mono; _hudLeft.FontSize = 15; _hudLeft.FontWeight = FontWeights.SemiBold; _hudLeft.Margin = new Thickness(10, 0, 0, 0); _hudLeft.VerticalAlignment = VerticalAlignment.Center;
        _hudClock.FontFamily = mono; _hudClock.FontSize = 15; _hudClock.FontWeight = FontWeights.SemiBold; _hudClock.Foreground = Brushes.White;
        var left = new StackPanel { Orientation = Orientation.Horizontal };
        left.Children.Add(_hudLast); left.Children.Add(_hudBlock); left.Children.Add(_hudLeft);
        var l = Pill(left); l.HorizontalAlignment = HorizontalAlignment.Left;
        var r = Pill(_hudClock); r.HorizontalAlignment = HorizontalAlignment.Right;
        _hudL = l; _hudR = r;
        _hud.Children.Add(l); _hud.Children.Add(r);

        TextElement.SetFontFamily(_flash, sans);

        _countText.FontFamily = mono; _countText.FontSize = 84; _countText.Foreground = Brushes.White; _countText.HorizontalAlignment = HorizontalAlignment.Center; _countText.VerticalAlignment = VerticalAlignment.Center;
        _count.Width = _count.Height = 160; _count.CornerRadius = new CornerRadius(80); _count.Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)); _count.Child = _countText;
        _count.HorizontalAlignment = HorizontalAlignment.Center; _count.VerticalAlignment = VerticalAlignment.Center;
        ShowHud = false;
    }

    public void SetHud(string block, string left, bool warn, bool last, string clock)
    {
        _hudBlock.Text = block; _hudLeft.Text = left; _hudClock.Text = clock;
        _hudLeft.Foreground = warn ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)) : Brushes.White;
        _hudLast.Visibility = last && _hudShowBlock ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border _hudL = null!, _hudR = null!;
    private bool _hudShowBlock = true;

    /// <summary>
    /// Escolhe o que aparece nas informações da tela (bloco, tempo do bloco, relógio) e onde (em cima / embaixo).
    /// <paramref name="bottomGap"/>: distância da base (a janela do talento tem a barra de controle embaixo).
    /// </summary>
    // Zona morta: faixa da cor do fundo atrás das informações — o texto passa por baixo e não disputa com elas.
    private readonly Border _hudZone = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private bool _hudBottom = true; private double _hudGap = 16, _hudRight = 20;
    private TextBlock? _hudLastText;

    /// <summary>Tamanho das informações proporcional à tela (≈ 3% da altura) e a zona morta do tamanho delas.</summary>
    private void LayoutHud()
    {
        var h = ActualHeight; if (h <= 0 || _hudL == null) return;
        var k = Math.Clamp(h / 1080.0, 0.55, 2.2);
        _hudBlock.FontSize = 26 * k; _hudLeft.FontSize = 30 * k; _hudClock.FontSize = 30 * k;
        if (_hudLastText != null) _hudLastText.FontSize = 14 * k;
        _hudLeft.Margin = new Thickness(_hudShowBlock ? 14 * k : 0, 0, 0, 0);
        foreach (var p in new[] { _hudL, _hudR }) { p.Padding = new Thickness(18 * k, 8 * k, 18 * k, 8 * k); p.CornerRadius = new CornerRadius(12 * k); }
        var pill = 30 * k * 1.35 + 16 * k;               // altura da pílula
        var edge = _hudBottom ? _hudGap : 16 * k;          // distância da borda
        var zone = pill + edge + 18 * k;                   // + respiro até o texto
        _hud.Margin = _hudBottom ? new Thickness(20 * k, 0, _hudRight, _hudGap) : new Thickness(20 * k, 16 * k, _hudRight, 0);
        var bg = (Background as SolidColorBrush)?.Color ?? Colors.Black;
        // Sólido na borda, esmaecendo nos últimos 30% (o texto some suavemente ao entrar na zona).
        var grad = new LinearGradientBrush { StartPoint = new Point(0, _hudBottom ? 1 : 0), EndPoint = new Point(0, _hudBottom ? 0 : 1) };
        grad.GradientStops.Add(new GradientStop(bg, 0)); grad.GradientStops.Add(new GradientStop(bg, 0.7)); grad.GradientStops.Add(new GradientStop(Color.FromArgb(0, bg.R, bg.G, bg.B), 1));
        _hudZone.Background = grad;
        _hudZone.Height = zone;
        _hudZone.VerticalAlignment = _hudBottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        _hudZone.Visibility = _hud.Visibility == Visibility.Visible && (_hudL.Visibility == Visibility.Visible || _hudR.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetHudOptions(bool block, bool time, bool clock, bool bottom, double bottomGap = 16, double rightGap = 20)
    {
        _hudBottom = bottom; _hudGap = bottomGap; _hudRight = rightGap;
        _hudShowBlock = block;
        _hudBlock.Visibility = block ? Visibility.Visible : Visibility.Collapsed;
        if (!block) _hudLast.Visibility = Visibility.Collapsed;
        _hudLeft.Visibility = time ? Visibility.Visible : Visibility.Collapsed;
        _hudLeft.Margin = new Thickness(block ? 10 : 0, 0, 0, 0);
        _hudL.Visibility = block || time ? Visibility.Visible : Visibility.Collapsed;
        _hudR.Visibility = clock ? Visibility.Visible : Visibility.Collapsed;
        _hud.VerticalAlignment = bottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        LayoutHud();
    }

    public void SetFlash(string? text, string? sub, double pct = -1)
    {
        if (!ShowFlashAndCount || string.IsNullOrEmpty(text)) { _flash.Hide(); return; }
        _flash.Show(text, sub, pct);
    }

    public void SetCount(int n)
    {
        _count.Visibility = ShowFlashAndCount && n > 0 ? Visibility.Visible : Visibility.Collapsed;
        _countText.Text = n.ToString();
    }

    /// <summary>Camadas visíveis (a saída NDI renderiza texto e overlay separadamente).</summary>
    public void ShowLayers(bool text, bool overlay)
    {
        _scroller.Visibility = text ? Visibility.Visible : Visibility.Hidden;
        _overlay.Visibility = overlay ? Visibility.Visible : Visibility.Hidden;
    }

    private RenderTargetBitmap? _rtb;
    /// <summary>Renderiza a superfície (tamanho atual) em <paramref name="buf"/> (BGRA pré-multiplicado).</summary>
    public void CaptureInto(byte[] buf, int width, int height, bool transparent)
    {
        // Reaproveita o bitmap (criar um de 8 MB por faixa forçava limpezas de memória que paravam o envio do NDI).
        if (_rtb == null || _rtb.PixelWidth != width || _rtb.PixelHeight != height) _rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        else _rtb.Clear();
        var bmp = _rtb;
        var prevBg = Background;
        // A troca de fundo só vale para o RenderTargetBitmap depois de um passe de layout (OnRender).
        if (transparent) { Background = Brushes.Transparent; UpdateLayout(); }
        bmp.Render(this);
        if (transparent) { Background = prevBg; UpdateLayout(); }
        bmp.CopyPixels(buf, width * 4, 0);
    }

    /// <summary>Quadro BGRA desta superfície no tamanho pedido (saída NDI). Thread da superfície.</summary>
    public byte[] Capture(int width, int height, bool transparent)
    {
        var dpi = 96.0;
        var bmp = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        var prevBg = Background;
        if (transparent) { Background = Brushes.Transparent; UpdateLayout(); }
        bmp.Render(this);
        if (transparent) { Background = prevBg; UpdateLayout(); }
        var buf = new byte[width * height * 4];
        bmp.CopyPixels(buf, width * 4, 0);
        return buf;
    }
}
