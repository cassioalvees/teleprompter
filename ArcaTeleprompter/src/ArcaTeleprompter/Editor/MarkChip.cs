using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Arca.Core.Model;

namespace Arca.App.Editor;

/// <summary>
/// Marcador ou pausa dentro do editor: bloco não editável (como contenteditable=false no protótipo).
/// Duplo clique renomeia; Delete/Backspace remove o bloco inteiro.
/// </summary>
public sealed class MarkChip : Grid
{
    private readonly TextBlock _label = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private readonly Rectangle _box = new() { RadiusX = 4, RadiusY = 4, StrokeDashArray = new DoubleCollection { 4, 3 } };
    private readonly Border _top = new() { BorderThickness = new Thickness(0, 1, 0, 0) };

    public BlockKind Kind { get; set; } = BlockKind.Bookmark;
    private string _text = "";
    public string Text { get => _text; set { _text = value; Refresh(); } }
    private bool _skip;
    public bool Skip { get => _skip; set { _skip = value; Refresh(); } }

    /// <summary>Pedido de renomear (o editor abre a caixa de texto).</summary>
    public static event Action<MarkChip>? RenameRequested;

    public MarkChip()
    {
        Children.Add(_top); Children.Add(_box); Children.Add(_label);
        Cursor = System.Windows.Input.Cursors.Arrow;
        ToolTip = "Duplo clique para renomear";
        MouseLeftButtonDown += (_, e) => { if (e.ClickCount == 2) { RenameRequested?.Invoke(this); e.Handled = true; } };
        Refresh();
    }

    private double _fs = 60, _after = 19.5; private Color _fg = Colors.White;

    /// <summary>Mesmas medidas do marcador e da pausa nas telas de prompter (ScriptFlow.BuildPrompter): com alturas
    /// iguais, a linha de leitura do editor cai no mesmo ponto das telas e do navegador.</summary>
    public void Apply(double fs, Color fg, double after) { _fs = fs; _fg = fg; _after = after; Refresh(); }

    private void Refresh()
    {
        var fs = _fs;
        _label.FontFamily = FontCatalog.Family("mono");
        // Mínimo legível no editor mesmo com fonte de prompter pequena.
        _label.FontSize = Math.Max(fs * 0.3, 13);
        _label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        _label.LineHeight = fs * 0.3 * 1.4;
        var a = Kind == BlockKind.Stop ? 0.65 : 0.55;
        _label.Foreground = ScriptFlow.WithAlpha(_fg, a);
        var name = (Kind == BlockKind.Stop ? "PAUSA · " : "") + Text.ToUpperInvariant() + (Kind == BlockKind.Bookmark && Skip ? " · PULADO" : "");
        _label.Text = name;
        if (Kind == BlockKind.Bookmark)
        {
            _top.Visibility = Visibility.Visible; _box.Visibility = Visibility.Collapsed;
            _top.BorderBrush = ScriptFlow.WithAlpha(_fg, 0.55);
            SetSpace(new Thickness(0, fs * 0.36, 0, fs * 0.24));
            _label.Margin = new Thickness(0, fs * 0.21 + 1, 0, fs * 0.06);   // + 1 da linha divisória (borda nas telas)
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            _top.Visibility = Visibility.Collapsed; _box.Visibility = Visibility.Visible;
            _box.Stroke = ScriptFlow.WithAlpha(_fg, 0.6);
            SetSpace(new Thickness(0, 0, 0, _after));
            _label.Margin = new Thickness(fs * 0.27 + 1, fs * 0.12 + 1, fs * 0.27 + 1, fs * 0.12 + 1);   // + 1 da borda
            HorizontalAlignment = HorizontalAlignment.Left;
        }
        Opacity = Kind == BlockKind.Bookmark && Skip ? 0.6 : 1;
    }

    /// <summary>
    /// O espaço acima e abaixo vai no bloco do documento (BlockUIContainer), não no cartão: assim ele se sobrepõe ao
    /// espaço do parágrafo vizinho (vale o maior), como nas telas de prompter e no navegador. Dentro do cartão os
    /// dois se somavam e o editor ficava fora de passo perto de marcadores.
    /// </summary>
    private void SetSpace(Thickness t)
    {
        Margin = new Thickness(0);
        // Só fora do desenho do documento: o bloco já existe quando o editor monta ou reaplica a aparência (Apply).
        if (Parent is System.Windows.Documents.BlockUIContainer c && c.Margin != t) c.Margin = t;
    }
}
