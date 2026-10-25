using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.Core.Model;

namespace Arca.App;

/// <summary>
/// Entrada fina: roda do mouse/trackpad (rolagem suave e velocidade, com sensibilidade e inversão ajustáveis),
/// histórico próprio de desfazer/refazer e âncora do cursor do editor.
/// </summary>
public partial class MainWindow
{
    // ═══ Roda do mouse e trackpad ═══

    private double? _scrollTarget;

    /// <summary>
    /// Um giro da roda (ou o deslizar do trackpad, que manda valores pequenos e contínuos):
    /// rodando → muda a velocidade; parado → rola o texto suavemente. Proporcional ao movimento.
    /// </summary>
    private void WheelInput(int delta)
    {
        var a = Vm.App;
        var notches = delta / 120.0 * (a.WheelInvert ? -1 : 1);
        if (Engine.Live)
        {
            if (Voice.Active) return;
            // Rodar para baixo = mais rápido (padrão); "inverter" troca.
            AdjustSpeed(-notches * a.WheelSpeedStep);
        }
        else ScrollLines(-notches * a.WheelLines);
    }

    /// <summary>Rola o texto <paramref name="lines"/> linhas com animação (o alvo acumula giros seguidos).</summary>
    public void ScrollLines(double lines)
    {
        var lh = Math.Max(1, _layout.LineHeightPx);
        var from = _scrollTarget ?? Engine.Y;
        _scrollTarget = Math.Clamp(from + lines * lh, 0, Math.Max(0, Engine.EndY));
        Vm.Follow = true;
        if (Voice.Active) Voice.JumpTo(_scrollTarget.Value);
    }

    private double _scrollV;
    /// <summary>
    /// Chamado a cada quadro: leva a posição ao alvo da rolagem manual (roda, trackpad, linha) com uma mola criticamente
    /// amortecida — sai devagar, acelera e chega devagar em ≈ 0,25 s. A aproximação exponencial de antes começava na
    /// velocidade máxima: no NDI (30 quadros/s) a linha pulava meia altura no primeiro quadro.
    /// </summary>
    private void StepManualScroll(double dt)
    {
        if (_scrollTarget is not { } t) { _scrollV = 0; return; }
        if (Engine.Live) { _scrollTarget = null; _scrollV = 0; return; }
        dt = Math.Min(dt, 0.05);
        const double w = 18;   // rad/s
        // Passos de ~4 ms: um quadro mais longo não faz a mola "estourar" (segue a mesma curva no tempo).
        var ny = Engine.Y; var n = Math.Max(1, (int)Math.Ceiling(dt / 0.004)); var h = dt / n;
        for (var i = 0; i < n; i++) { _scrollV += (w * w * (t - ny) - 2 * w * _scrollV) * h; ny += _scrollV * h; }
        if (Math.Abs(t - ny) < 0.3 && Math.Abs(_scrollV) < 20) { ny = t; _scrollTarget = null; _scrollV = 0; }
        Engine.SetY(ny);
        _forceSync = true;
    }

    // ═══ Navegação manual ═══

    /// <summary>
    /// Leva o texto para <paramref name="y"/>: cancela rolagem suave pendente, sincroniza o editor e, com a voz ligada,
    /// recomeça o acompanhamento dali. <paramref name="textLine"/>: ajusta para a primeira linha de texto (nunca o
    /// título de um marcador ou uma pausa).
    /// </summary>
    public void GoTo(double y, bool textLine = false)
    {
        if (textLine) y = FirstTextLineAt(y);
        _scrollTarget = null;
        Engine.SetY(y);
        Vm.Follow = true; _forceSync = true;
        if (Voice.Active) Voice.JumpTo(Engine.Y);
    }

    /// <summary>Primeira linha de texto a partir de <paramref name="y"/> (pula marcadores e pausas).</summary>
    public double FirstTextLineAt(double y)
    {
        var t = y;
        for (var guard = 0; guard < 8 && !IsTextLine(t); guard++)
        {
            var nx = _measure.LineTop(t, 1);
            // Linha em branco (parágrafo vazio) não tem "próxima linha" para o RichTextBox: vai ao próximo bloco com texto.
            if (double.IsNaN(nx) || nx <= t) nx = NextReadableTop(_measure.AnchorAt(t + 1).Block);
            if (double.IsNaN(nx) || nx <= t) break;
            t = nx;
        }
        return t;
    }

    private double NextReadableTop(int block)
    {
        for (var k = block + 1; k < Doc.Blocks.Count; k++)
            if (IsReadable(k) && _measure.TopOf(k) is var top && !double.IsNaN(top)) return top;
        return double.NaN;
    }

    /// <summary>Topo da última linha do bloco com texto anterior a <paramref name="block"/>.</summary>
    private double PrevReadableLastTop(int block)
    {
        for (var k = block - 1; k >= 0; k--)
            if (IsReadable(k) && _measure.LastLineTopOf(k) is var top && !double.IsNaN(top)) return top;
        return double.NaN;
    }

    // ═══ Desfazer / refazer (histórico do roteiro, não das formatações internas do editor) ═══

    private readonly Stack<(ScriptDocument Doc, CaretAnchor Caret)> _undo = new(), _redo = new();
    private CaretAnchor _lastCaret;

    /// <summary>Guarda o estado anterior antes de uma alteração do roteiro (chamado no commit da edição).</summary>
    private void PushUndo(ScriptDocument before)
    {
        _undo.Push((before.Clone(), _lastCaret));
        while (_undo.Count > 200) { var keep = _undo.Take(150).Reverse().ToList(); _undo.Clear(); foreach (var k in keep) _undo.Push(k); }
        _redo.Clear();
    }

    private void ResetHistory() { _undo.Clear(); _redo.Clear(); _lastCaret = CaretNow(); }

    public void Undo()
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        _editT.Stop(); CommitEdits(); // o que acabou de ser digitado também entra no histórico
        if (_undo.Count == 0) { Flash("Nada para desfazer"); return; }
        _redo.Push((Doc.Clone(), CaretNow()));
        var (d, c) = _undo.Pop();
        RestoreDoc(d, c);
        Flash("Desfeito");
    }

    public void Redo()
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        if (_redo.Count == 0) { Flash("Nada para refazer"); return; }
        _undo.Push((Doc.Clone(), CaretNow()));
        var (d, c) = _redo.Pop();
        RestoreDoc(d, c);
        Flash("Refeito");
    }

    /// <summary>Troca o roteiro do editor mantendo a posição do prompter e o cursor.</summary>
    private void RestoreDoc(ScriptDocument d, CaretAnchor caret)
    {
        var anchor = _measure.AnchorAt(Engine.Y);
        using (SuppressEdits())
        {
            Doc = d;
            Editor.Document = ScriptFlow.BuildEditor(d, Vm.S, _layout, _font);
        }
        LayoutEditor();
        Dispatcher.BeginInvoke(LayoutEditor, System.Windows.Threading.DispatcherPriority.Loaded);
        _bmSig = null;
        RebuildOutputs(anchor);
        UpdateRundown();
        DrawEditorNames();
        MarkDirty(); ScheduleSave();
        SetCaret(caret);
    }

    // ═══ Âncora do cursor: bloco + posição no texto (sobrevive à reconstrução do documento) ═══

    public readonly record struct CaretAnchor(int Block, int Offset);

    private CaretAnchor CaretNow()
    {
        var fd = Editor.Document; if (fd == null) return default;
        var caret = Editor.CaretPosition;
        var top = ScriptFlow.TopBlock(caret, fd);
        var i = top == null ? 0 : fd.Blocks.TakeWhile(b => b != top).Count();
        var off = top is Paragraph p ? new TextRange(p.ContentStart, caret).Text.Length : 0;
        return new CaretAnchor(i, off);
    }

    private void SetCaret(CaretAnchor a)
    {
        var fd = Editor.Document; if (fd == null) return;
        Dispatcher.BeginInvoke(() =>
        {
            var blk = fd.Blocks.ElementAtOrDefault(Math.Min(a.Block, fd.Blocks.Count - 1));
            TextPointer? ptr = blk is Paragraph p ? PrompterSurface.PointerAt(p, a.Offset) : blk?.ContentStart;
            if (ptr == null) return;
            Editor.CaretPosition = ptr;
            Editor.Focus();
            EnsureCaretVisible();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Rola o editor só o necessário para o cursor ficar visível (ex.: depois de colar um texto longo).</summary>
    private void EnsureCaretVisible()
    {
        var r = Editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
        if (r.IsEmpty) return;
        var p = Editor.TranslatePoint(r.TopLeft, EdScroll);
        var vh = EdScroll.ViewportHeight; var lh = r.Height * EditorScale;
        if (p.Y < 0 || p.Y + lh > vh)
        {
            var target = EdScroll.VerticalOffset + p.Y - vh * 0.6;
            EdScroll.ScrollToVerticalOffset(Math.Max(0, target));
        }
    }

    /// <summary>
    /// Quebra de linha dentro de um parágrafo (Shift+Enter ou texto colado) vira parágrafo próprio — cada linha
    /// pode ter seu apresentador e o espaçamento fica igual no editor e no prompter. Feito no lugar: o cursor não pula.
    /// </summary>
    private static bool SplitLineBreaks(FlowDocument fd)
    {
        var any = false;
        foreach (var p in fd.Blocks.OfType<Paragraph>().ToList())
        {
            var breaks = Descendants(p.Inlines).OfType<LineBreak>().ToList();
            if (breaks.Count == 0) continue;
            any = true;
            var who = (p.Tag as BlockTag)?.Who ?? 0;
            foreach (var lb in breaks)
            {
                var pos = lb.ElementStart;
                lb.SiblingInlines?.Remove(lb);
                var np = pos.InsertParagraphBreak();
                if (np.Paragraph is { } n) n.Tag = new BlockTag { Who = who };
            }
        }
        return any;

        static IEnumerable<Inline> Descendants(InlineCollection inl)
        {
            foreach (var i in inl.ToList())
            {
                yield return i;
                if (i is Span s) foreach (var c in Descendants(s.Inlines)) yield return c;
            }
        }
    }
}
