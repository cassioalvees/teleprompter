using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.App.ViewModels;
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Storage;
using Arca.Import;
using Microsoft.Win32;

namespace Arca.App;

/// <summary>Editor WYSIWYG, abas, arquivos, marcadores, apresentadores e salvamento automático.</summary>
public partial class MainWindow
{
    private readonly DispatcherTimer _editT = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _saveT = new() { Interval = TimeSpan.FromMilliseconds(1200) };
    private int _suppress;
    private bool _skipEditorSync, _editorMapDirty = true;
    private readonly List<(int Block, double Top, double Height)> _edMap = new();
    private double _lastSetOffset = -1;
    private DateTime _lastVersion = DateTime.MinValue;
    private List<ScriptVersion> _versions = new();
    private DocTabVM? _active;

    private sealed class Suppressor : IDisposable { private readonly MainWindow _w; public Suppressor(MainWindow w) { _w = w; _w._suppress++; } public void Dispose() => _w._suppress--; }
    private IDisposable SuppressEdits() => new Suppressor(this);

    private void InitEditor()
    {
        _editT.Tick += (_, _) => { _editT.Stop(); CommitEdits(); };
        _saveT.Tick += (_, _) => { _saveT.Stop(); SaveSession(); };
        MarkChip.RenameRequested += chip => RenameChip(chip);
        DataObject.AddPastingHandler(Editor, OnPaste);
        _versions = Stores.LoadVersions();
        Editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => { }));
    }

    // ═══ Diagramação do editor (mesmo espaço de 1000 unidades do prompter) ═══
    private void LayoutEditor()
    {
        var vw = EdScroll.ViewportWidth > 0 ? EdScroll.ViewportWidth : EdHost.ActualWidth - 10;
        if (vw <= 0 || Editor.Document == null) return;
        var k = vw / PrompterLayout.Width;
        EdScale.ScaleX = EdScale.ScaleY = k;
        var vh = EdHost.ActualHeight / k;
        // Folga em cima (a 1ª linha alcança a linha de leitura) e embaixo (a última pode subir até ela).
        var pp = new Thickness(_layout.PaddingLeft, vh * Vm.S.ReadPct / 100, _layout.PaddingRight, vh);
        if (Editor.Document.PagePadding != pp) Editor.Document.PagePadding = pp;
        EdOverlay.Update();
        _editorMapDirty = true; _forceSync = true;
    }

    private double EditorScale => EdScale.ScaleY > 0 ? EdScale.ScaleY : 1;

    /// <summary>Posição de cada bloco do editor (coordenadas do texto, sem a folga do topo).</summary>
    private void BuildEditorMap()
    {
        _edMap.Clear();
        var fd = Editor.Document; if (fd == null) return;
        var pad = fd.PagePadding.Top;
        var i = 0;
        var tops = new List<(int, double)>();
        foreach (var b in fd.Blocks)
        {
            double top = double.NaN;
            if (b is BlockUIContainer { Child: FrameworkElement chip } && chip.IsVisible)
            {
                try { top = chip.TranslatePoint(new Point(0, 0), Editor).Y - chip.Margin.Top; } catch { }
            }
            else
            {
                var r = b.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                if (!r.IsEmpty) top = r.Top - b.Margin.Top * 0;
            }
            if (!double.IsNaN(top)) tops.Add((i, top - pad));
            i++;
        }
        var contentH = Editor.ActualHeight - pad - fd.PagePadding.Bottom;
        for (var j = 0; j < tops.Count; j++)
        {
            var next = j + 1 < tops.Count ? tops[j + 1].Item2 : contentH;
            _edMap.Add((tops[j].Item1, tops[j].Item2, Math.Max(1, next - tops[j].Item2)));
        }
        _editorMapDirty = false;
    }

    /// <summary>Prompter → editor: posiciona o editor na mesma âncora (bloco + fração) da linha de leitura.</summary>
    private void SyncEditorScroll(double y)
    {
        if (_editorMapDirty) BuildEditorMap();
        if (_edMap.Count == 0) return;
        var (block, frac) = _measure.AnchorAt(y);
        var e = _edMap.FirstOrDefault(x => x.Block == block);
        if (e.Height == 0) e = _edMap.FirstOrDefault(x => x.Block >= block);
        if (e.Height == 0) return;
        var ey = e.Top + frac * e.Height;
        var target = Math.Max(0, ey * EditorScale);
        if (Math.Abs(EdScroll.VerticalOffset - target) > 0.01)
        {
            _lastSetOffset = target;
            EdScroll.ScrollToVerticalOffset(target);
        }
    }

    /// <summary>
    /// Editor → prompter (operador rolando o editor com o prompt pausado). Só nesse sentido: a posição não
    /// volta para o editor no mesmo quadro — sem realimentação, sem "piscar" no topo ou em blocos pulados.
    /// </summary>
    private void EdScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Largura visível mudou (maximizar, restaurar, mover para outro monitor): o texto acompanha. Aqui a largura já é a
        // nova — no SizeChanged da área ela ainda era a antiga e o editor ficava um passo atrás (texto enorme ao restaurar).
        if (e.ViewportWidthChange != 0) LayoutEditor();
        if (e.ExtentHeightChange != 0 || e.ViewportHeightChange != 0) { _editorMapDirty = true; return; }
        if (e.VerticalChange == 0) return;
        if (Math.Abs(EdScroll.VerticalOffset - _lastSetOffset) < 1) return; // fomos nós
        if (Engine.Live || Voice.Active || !Vm.Follow) return;
        if (_editorMapDirty) BuildEditorMap();
        var ey = EdScroll.VerticalOffset / EditorScale;
        var e2 = _edMap.LastOrDefault(x => x.Top <= ey + 0.01);
        if (e2.Height == 0) { if (_edMap.Count > 0) e2 = _edMap[0]; else return; }
        var frac = Math.Clamp((ey - e2.Top) / e2.Height, 0, 1);
        _skipEditorSync = true;
        Engine.SetY(_measure.YOf(e2.Block, frac));
    }

    private void EdScroll_Wheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { ChangeWpl(e.Delta < 0 ? 1 : -1); return; }
        WheelInput(e.Delta);
    }

    // ═══ Documento ↔ editor ═══
    private void LoadEditor(ScriptDocument doc)
    {
        using (SuppressEdits())
        {
            Doc = doc;
            var fd = ScriptFlow.BuildEditor(doc, Vm.S, _layout, _font);
            Editor.Document = fd;
            Editor.IsReadOnly = Vm.Locked;
        }
        LayoutEditor();
        DrawEditorNames();
        _bmSig = null; _cur = -1; // força recriar a lista (inclusive vazia) do novo roteiro
        RebuildOutputs((0, 0));
        UpdateRundown();
        ResetHistory();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppress > 0) return;
        _editorMapDirty = true;
        _editT.Stop(); _editT.Start();
    }

    /// <summary>Pausa na digitação: lê o editor, aplica convenções e atualiza saídas.</summary>
    private void CommitEdits()
    {
        var fd = Editor.Document; if (fd == null) return;
        using (SuppressEdits())
        {
            SplitLineBreaks(fd);
            NormalizeEditor(fd);
            // "*Título" num parágrafo (fora do que está sendo digitado) vira marcador, como no protótipo.
            var caretBlock = ScriptFlow.TopBlock(Editor.CaretPosition, fd);
            foreach (var p in fd.Blocks.OfType<Paragraph>().ToList())
            {
                if (p == caretBlock) continue;
                var t = new TextRange(p.ContentStart, p.ContentEnd).Text.Trim();
                if (t.Length > 1 && t[0] == '*' && !char.IsWhiteSpace(t[1]))
                    fd.Blocks.InsertBefore(p, new BlockUIContainer(new MarkChip { Kind = BlockKind.Bookmark, Text = t[1..].Trim() }));
                else continue;
                fd.Blocks.Remove(p);
            }
            var next = ScriptFlow.Read(fd);
            ScriptFlow.ApplyPresentation(fd, Vm.S, _layout);
            LayoutEditor(); // garante a folga do topo/rodapé (só altera se mudou)
            DrawEditorNames();
            // Só conta como alteração se o conteúdo mudou (layout/margens também disparam TextChanged).
            if (Same(next, Doc)) { _lastCaret = CaretNow(); return; }
            PushUndo(Doc);
            Doc = next;
            _lastCaret = CaretNow();
            MarkDirty();
        }
        RebuildOutputs();
        UpdateRundown();
        UpdateCurrentWho();
        ScheduleSave();
        if ((DateTime.Now - _lastVersion).TotalMinutes >= 2 && _active?.Dirty == true) { _lastVersion = DateTime.Now; AddVersion("automática"); }
    }

    private static bool Same(ScriptDocument a, ScriptDocument b) =>
        System.Text.Json.JsonSerializer.Serialize(a.Blocks, TpsFile.Json) == System.Text.Json.JsonSerializer.Serialize(b.Blocks, TpsFile.Json);

    /// <summary>Listas/tabelas coladas viram parágrafos simples (o roteiro é uma lista linear de blocos).</summary>
    private void NormalizeEditor(FlowDocument fd)
    {
        if (fd.Blocks.All(b => b is Paragraph or BlockUIContainer { Child: MarkChip })) return;
        var doc = ScriptFlow.Read(fd);
        var caret = CaretNow();
        var rebuilt = ScriptFlow.BuildEditor(doc, Vm.S, _layout, _font);
        Editor.Document = rebuilt;
        // O RichTextBox zera a margem do documento ao recebê-lo (e de novo ao carregar): reaplica a folga do topo,
        // senão a 1ª linha fica acima da seta e o texto não desce mais até ela.
        LayoutEditor();
        Dispatcher.BeginInvoke(LayoutEditor, DispatcherPriority.Loaded);
        SetCaret(caret); // mesmo bloco e mesma posição no texto (o cursor não pula)
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        Dispatcher.BeginInvoke(EnsureCaretVisible, DispatcherPriority.ApplicationIdle);
        // Colar texto simples respeita as convenções (*marcador, [PAUSA]); RTF/HTML entram formatados e são normalizados.
        if (e.DataObject.GetDataPresent(DataFormats.Rtf) || e.DataObject.GetDataPresent(DataFormats.Xaml)) return;
        if (e.DataObject.GetData(DataFormats.UnicodeText) is string t && (t.Contains("\n*") || t.StartsWith('*') || t.Contains("[PAUSA]", StringComparison.OrdinalIgnoreCase)))
        {
            e.CancelCommand();
            var part = ScriptDocument.FromPlainText(t);
            InsertBlocksAtCaret(part.Blocks);
        }
    }

    private void InsertBlocksAtCaret(IEnumerable<ScriptBlock> blocks)
    {
        var fd = Editor.Document;
        var at = ScriptFlow.TopBlock(Editor.CaretPosition, fd);
        foreach (var b in blocks)
        {
            var el = ScriptFlow.EditorBlock(b, _layout.FontSize);
            if (at != null) fd.Blocks.InsertAfter(at, el); else fd.Blocks.Add(el);
            at = el;
        }
        CommitEdits();
    }

    // ═══ Seleção: apresentador atual ═══
    private void Editor_SelectionChanged(object sender, RoutedEventArgs e) { UpdateCurrentWho(); UpdateCaret(); }

    // ═══ Cursor de edição ═══
    private readonly DispatcherTimer _caretT = new() { Interval = TimeSpan.FromMilliseconds(530) };

    private void InitCaret()
    {
        Editor.CaretBrush = Brushes.Transparent;
        // Desfazer/refazer é do roteiro (MainWindow.Input): o do RichTextBox gravaria também as formatações internas.
        Editor.IsUndoEnabled = false;
        _caretT.Tick += (_, _) => EdCaret.Opacity = EdCaret.Opacity > 0 ? 0 : 1;
        Editor.GotKeyboardFocus += (_, _) => UpdateCaret();
        Editor.LostKeyboardFocus += (_, _) => UpdateCaret();
        Editor.TextChanged += (_, _) => UpdateCaret();
        EdSizer.SizeChanged += (_, _) => { UpdateCaret(); DrawEditorNames(); };
    }

    /// <summary>Posiciona o cursor próprio (3 px na tela, cor de destaque, piscando) na posição do cursor do editor.</summary>
    private void UpdateCaret() => Dispatcher.BeginInvoke(() =>
    {
        var show = Editor.IsKeyboardFocusWithin && !Editor.IsReadOnly && Editor.Selection.IsEmpty;
        var r = show ? Editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward) : Rect.Empty;
        if (r.IsEmpty || double.IsInfinity(r.Height)) { EdCaret.Visibility = Visibility.Collapsed; _caretT.Stop(); return; }
        var p = Editor.TranslatePoint(r.TopLeft, EdCaretLayer);
        var w = 3 / Math.Max(0.1, EditorScale);
        // Ao lado de um marcador/pausa o retângulo é o do bloco inteiro: limita à altura de uma linha de texto.
        var lineH = (Editor.Document?.FontSize ?? 40) * 1.3;
        var h = Math.Clamp(r.Height, 8, lineH);
        EdCaret.Width = w; EdCaret.Height = h;
        Canvas.SetLeft(EdCaret, p.X - w / 2); Canvas.SetTop(EdCaret, p.Y + (r.Height - h) / 2);
        EdCaret.Visibility = Visibility.Visible; EdCaret.Opacity = 1;
        _caretT.Stop(); _caretT.Start();
    }, DispatcherPriority.Loaded);
    private void UpdateCurrentWho()
    {
        var b = ScriptFlow.TopBlock(Editor.Selection.Start, Editor.Document) as Paragraph;
        var w = (b?.Tag as BlockTag)?.Who ?? 0;
        Vm.CurrentWho = w;
        var p = w > 0 && w <= Vm.Presenters.Count ? Vm.Presenters[w - 1] : null;
        Vm.CurPresName = p == null ? "Sem apresentador" : string.IsNullOrWhiteSpace(p.Name) ? "Apresentador " + w : p.Name;
        Vm.CurPresColor = p?.Color ?? "Transparent";
    }

    // ═══ Apresentadores ═══

    /// <summary>Alt+N: aplica o apresentador N aos parágrafos da seleção (0 remove).</summary>
    public void SetWho(int n)
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        if (n > Vm.Presenters.Count) { Flash($"Apresentador {n} não existe"); return; }
        var fd = Editor.Document;
        using (SuppressEdits())
        {
            // Trecho selecionado no meio de um parágrafo: separa o trecho em parágrafo(s) próprio(s), para só ele
            // mudar de apresentador (o resto do texto continua com quem já estava).
            if (!Editor.Selection.IsEmpty) SplitAtSelection();
            var a = ScriptFlow.TopBlock(Editor.Selection.Start, fd); var z = ScriptFlow.TopBlock(Editor.Selection.End, fd);
            if (a == null) { Flash("Posicione o cursor no texto"); return; }
            var on = false;
            foreach (var b in fd.Blocks)
            {
                if (b == a) on = true;
                if (on && b is Paragraph p) p.Tag = new BlockTag { Who = n };
                if (b == z) break;
            }
            ScriptFlow.ApplyPresentation(fd, Vm.S, _layout);
        }
        DrawEditorNames();
        MarkDirty();
        CommitEdits();
    }

    /// <summary>Quebra o parágrafo no início e no fim da seleção (quando não estão na borda do parágrafo).</summary>
    private void SplitAtSelection()
    {
        var sel = Editor.Selection;
        var s = sel.Start.GetPositionAtOffset(0, LogicalDirection.Forward)!;
        var e = sel.End.GetPositionAtOffset(0, LogicalDirection.Backward)!;
        // Fim primeiro (as posições antes dele não mudam). O pedaço depois da seleção mantém o apresentador original.
        if (e.Paragraph is { } pe && e.CompareTo(pe.ContentEnd.GetInsertionPosition(LogicalDirection.Backward)) < 0 && HasText(e, pe.ContentEnd))
        {
            var who = (pe.Tag as BlockTag)?.Who ?? 0;
            var after = e.InsertParagraphBreak();
            if (after.Paragraph is { } pa) pa.Tag = new BlockTag { Who = who };
        }
        if (s.Paragraph is { } ps && s.CompareTo(ps.ContentStart.GetInsertionPosition(LogicalDirection.Forward)) > 0 && HasText(ps.ContentStart, s))
        {
            var who = (ps.Tag as BlockTag)?.Who ?? 0;
            var start = s.InsertParagraphBreak();
            // O parágrafo original fica com o texto antes da seleção; o novo (a seleção) recebe o apresentador depois.
            if (start.Paragraph is { } pn) pn.Tag = new BlockTag { Who = who };
            Editor.Selection.Select(start, Editor.Selection.End);
        }
        static bool HasText(TextPointer a, TextPointer b) => new TextRange(a, b).Text.Trim().Length > 0;
    }

    /// <summary>Nome do apresentador acima do início de cada fala, no editor (igual ao prompter).</summary>
    private void DrawEditorNames() => Dispatcher.BeginInvoke(() =>
    {
        EdNames.Children.Clear();
        var fd = Editor.Document; var s = Vm.S;
        if (fd == null || !s.PresenterNames) return;
        var fs = _layout.FontSize; var prev = 0;
        foreach (var b in fd.Blocks)
        {
            if (b is not Paragraph p) { prev = 0; continue; }
            var who = (p.Tag as BlockTag)?.Who ?? 0;
            if (who > 0 && who != prev && who <= s.Presenters.Count)
            {
                var r = p.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                if (!r.IsEmpty)
                {
                    var pr = s.Presenters[who - 1];
                    var tb = new TextBlock
                    {
                        Text = (string.IsNullOrWhiteSpace(pr.Name) ? "Apresentador " + who : pr.Name).ToUpperInvariant(),
                        FontFamily = FontCatalog.Family("mono"), FontSize = fs * 0.3, Opacity = 0.85,
                        Foreground = ScriptFlow.ParseBrush(pr.Color, Brushes.White), TextTrimming = TextTrimming.CharacterEllipsis,
                        Width = PrompterLayout.Width - _layout.PaddingLeft - _layout.PaddingRight, IsHitTestVisible = false,
                        TextAlignment = Spacing.Align(s.Align)
                    };
                    var pt = Editor.TranslatePoint(r.TopLeft, EdNames);
                    Canvas.SetLeft(tb, _layout.PaddingLeft);
                    Canvas.SetTop(tb, pt.Y - Spacing.NameOffset(fs));
                    EdNames.Children.Add(tb);
                }
            }
            prev = who;
        }
    }, DispatcherPriority.Loaded);

    public void AddPresenter()
    {
        if (Vm.Presenters.Count >= 8) { Flash("Máximo de 8 apresentadores"); return; }
        var c = PrompterSettings.DefaultPresenterColors[Vm.Presenters.Count % 8];
        Vm.AddPresenterVM("Apresentador " + (Vm.Presenters.Count + 1), c);
        Vm.RenumberPresenters();
    }

    /// <summary>Remove o apresentador; as falas dele ficam sem apresentador e os seguintes são renumerados.</summary>
    public void RemovePresenter(PresenterVM p)
    {
        var n = Vm.Presenters.IndexOf(p) + 1; if (n <= 0) return;
        foreach (var para in Editor.Document.Blocks.OfType<Paragraph>())
            if (para.Tag is BlockTag t) para.Tag = new BlockTag { Who = t.Who == n ? 0 : t.Who > n ? t.Who - 1 : t.Who };
        if (int.TryParse(Vm.PresenterOnly, out var only)) Vm.PresenterOnly = only == n ? "0" : only > n ? (only - 1).ToString() : Vm.PresenterOnly;
        p.PropertyChanged -= null;
        Vm.Presenters.Remove(p);
        Vm.RenumberPresenters();
        MarkDirty();
        CommitEdits();
    }

    // ═══ Marcadores e pausas ═══
    private List<BlockUIContainer> Chips(BlockKind kind) => Editor.Document.Blocks.OfType<BlockUIContainer>().Where(b => b.Child is MarkChip c && c.Kind == kind).ToList();

    public void InsertBlock(BlockKind kind)
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        var fd = Editor.Document;
        var at = ScriptFlow.TopBlock(Editor.CaretPosition, fd) ?? fd.Blocks.FirstBlock;
        var name = kind == BlockKind.Bookmark
            ? (at is Paragraph p ? string.Join(' ', new TextRange(p.ContentStart, p.ContentEnd).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3)) : "")
            : "Ponto de pausa";
        if (string.IsNullOrWhiteSpace(name)) name = "Novo marcador";
        var chip = new BlockUIContainer(new MarkChip { Kind = kind, Text = name });
        if (at != null) fd.Blocks.InsertBefore(at, chip); else fd.Blocks.Add(chip);
        MarkDirty(); CommitEdits();
        Flash(kind == BlockKind.Bookmark ? "Marcador inserido · duplo clique para renomear" : "Pausa inserida");
    }

    public void RemoveBlock(BlockKind kind, int index)
    {
        var list = Chips(kind); if (index < 0 || index >= list.Count) return;
        Editor.Document.Blocks.Remove(list[index]);
        MarkDirty(); CommitEdits();
        Flash(kind == BlockKind.Bookmark ? "Marcador excluído" : "Pausa excluída");
    }

    public void RemoveAllStops()
    {
        foreach (var c in Chips(BlockKind.Stop)) Editor.Document.Blocks.Remove(c);
        MarkDirty(); CommitEdits();
    }

    /// <summary>Pular bloco: o marcador e o texto até o próximo marcador não vão ao ar.</summary>
    public void ToggleSkip(int index)
    {
        var list = Chips(BlockKind.Bookmark); if (index < 0 || index >= list.Count) return;
        var chip = (MarkChip)list[index].Child; chip.Skip = !chip.Skip;
        using (SuppressEdits()) ScriptFlow.ApplyPresentation(Editor.Document, Vm.S, _layout);
        DrawEditorNames();
        MarkDirty(); CommitEdits();
    }

    /// <summary>Arrastar na lista: move o marcador e todo o seu conteúdo para antes do destino.</summary>
    public void MoveBlock(int from, int to)
    {
        var doc = ScriptFlow.Read(Editor.Document);
        if (!doc.MoveRundownBlock(from, to)) return;
        var anchor = _measure.AnchorAt(Engine.Y);
        using (SuppressEdits())
        {
            var fd = ScriptFlow.BuildEditor(doc, Vm.S, _layout, _font);
            fd.PagePadding = Editor.Document.PagePadding;
            Editor.Document = fd;
        }
        Doc = doc;
        MarkDirty();
        RebuildOutputs(anchor);
        UpdateRundown();
        Flash("Bloco movido");
    }

    private void RenameChip(MarkChip chip)
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        var name = Prompt(chip.Kind == BlockKind.Bookmark ? "Renomear marcador" : "Texto da pausa", chip.Text);
        if (name == null) return;
        chip.Text = name.Trim().Length > 0 ? name.Trim() : chip.Text;
        MarkDirty(); CommitEdits();
    }

    public void JumpBookmark(int i)
    {
        if (i < 0 || i >= _bmBoxes.Count) return;
        // A seta vai para a primeira linha de texto do bloco, não para o título do marcador.
        var t = _bmBoxes[i].Top;
        for (var guard = 0; guard < 8 && !IsTextLine(t); guard++) { var nx = _measure.LineTop(t, 1); if (double.IsNaN(nx) || nx <= t) break; t = nx; }
        GoTo(t);
    }

    /// <summary>Próximo/anterior parágrafo de texto (README: ] e [).</summary>
    public void JumpParagraph(int d)
    {
        var y = Engine.Y; const double eps = 2;
        var paras = _measure.Map.Where(b => Doc.Blocks.ElementAtOrDefault(b.Block) is { Kind: BlockKind.Text } blk && blk.PlainText.Trim().Length > 0).ToList();
        if (paras.Count == 0) return;
        var t = d > 0 ? paras.FirstOrDefault(p => p.Top > y + eps) : paras.LastOrDefault(p => p.Top < y - eps);
        if (t.Height == 0) { Flash(d > 0 ? "Último parágrafo" : "Primeiro parágrafo"); return; }
        GoTo(t.Top);
    }

    /// <summary>Ctrl+J: leva o prompter até o parágrafo do cursor do editor.</summary>
    public void CursorToPrompter()
    {
        var b = ScriptFlow.TopBlock(Editor.CaretPosition, Editor.Document); if (b == null) return;
        var i = ScriptFlow.IndexOf(Editor.Document, b);
        GoTo(_measure.YOf(i, 0), textLine: true);
    }

    // ═══ Formatação ═══
    private void Exec(RoutedUICommand cmd) { if (Vm.Locked) { Flash("Pause o prompt para editar"); return; } Editor.Focus(); cmd.Execute(null, Editor); }
    private void SetColor(string? hex)
    {
        if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
        Editor.Focus();
        if (hex == null) Editor.Selection.ClearAllProperties();
        else Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, ScriptFlow.ParseBrush(hex, Brushes.White));
        MarkDirty(); CommitEdits();
    }
    private void InsertSymbol(string s) { if (Vm.Locked) return; Editor.Focus(); Editor.CaretPosition.InsertTextInRun(s + " "); }
    private void UpperSelection() { if (Vm.Locked || Editor.Selection.IsEmpty) return; Editor.Selection.Text = Editor.Selection.Text.ToUpper(System.Globalization.CultureInfo.CurrentCulture); }

    private void BuildToolbarExtras()
    {
        foreach (var (c, t) in new[] { ("#ffffff", "Cor padrão (limpa a formatação)"), ("#ffd54a", "Amarelo"), ("#ff6b6b", "Vermelho"), ("#6ccb5f", "Verde"), ("#6aa6dc", "Azul") })
        {
            var b = new Button { Style = (Style)FindResource("IconBtn"), Width = 24, ToolTip = t, Focusable = false, Content = new System.Windows.Shapes.Ellipse { Width = 12, Height = 12, Fill = ScriptFlow.ParseBrush(c, Brushes.White), Stroke = new SolidColorBrush(Color.FromArgb(153, 128, 128, 128)), StrokeThickness = 1 } };
            b.Click += (_, _) => SetColor(c == "#ffffff" ? null : c);
            ColorTools.Children.Add(b);
        }
        // Outra cor: seletor do app (cores prontas, últimas usadas, HEX/RGB).
        var hue = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        foreach (var (c, o) in new[] { ("#ff5f5f", 0.0), ("#ffd54a", 0.25), ("#6ccb5f", 0.5), ("#5e9eff", 0.75), ("#c77dff", 1.0) }) hue.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(c), o));
        var more = new Button { Style = (Style)FindResource("IconBtn"), Width = 24, ToolTip = "Outra cor para o texto selecionado…", Focusable = false, Content = new System.Windows.Shapes.Ellipse { Width = 12, Height = 12, Fill = hue, Stroke = new SolidColorBrush(Color.FromArgb(153, 128, 128, 128)), StrokeThickness = 1 } };
        more.Click += (_, _) =>
        {
            if (Vm.Locked) { Flash("Pause o prompt para editar"); return; }
            // Cor atual do trecho: a cor aplicada nele ou, sem cor própria, a cor do texto do roteiro.
            var cur = Vm.S.Foreground;
            if (Editor.Selection.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush sb && Editor.Document.Foreground is SolidColorBrush db && sb.Color != db.Color && sb.Color != ((Editor.Foreground as SolidColorBrush)?.Color ?? sb.Color))
                cur = $"#{sb.Color.R:x2}{sb.Color.G:x2}{sb.Color.B:x2}";
            if (ArcaDialog.PickColor(this, cur, "Cor do texto selecionado") is { } hex) SetColor(hex);
        };
        ColorTools.Children.Add(more);
        BuildTalentTools();
    }

    // ═══ Abas e sessão ═══
    private void RestoreSession()
    {
        var ses = Stores.LoadSession();
        if (ses.Docs.Count == 0)
        {
            // Primeira execução: começa limpo (os exemplos ficam em DocumentosARCA TeleprompterExemplos).
            var t = NewTabVM("Sem título.tps", new ScriptDocument { Blocks = { ScriptBlock.Paragraph("") } }, null);
            Vm.Tabs.Add(t); Activate(t);
            Vm.SavedAt = "";
            return;
        }
        foreach (var d in ses.Docs)
        {
            var t = NewTabVM(d.Name, new ScriptDocument { Blocks = d.Blocks.Count > 0 ? d.Blocks : new() { ScriptBlock.Paragraph("") } }, d.Path);
            t.Y = d.Y; t.Dirty = d.Dirty; Vm.Tabs.Add(t);
        }
        Activate(Vm.Tabs.FirstOrDefault(t => t.Id == ses.Active) ?? Vm.Tabs[0]);
        Vm.SavedAt = "Restaurado da última sessão";
    }

    private static long _ids = DateTime.Now.Ticks;
    private static DocTabVM NewTabVM(string name, ScriptDocument doc, string? path) => new() { Id = Interlocked.Increment(ref _ids), Name = name, Doc = doc, Path = path };

    private void SnapshotActive()
    {
        if (_active == null) return;
        if (_suppress == 0 && Editor.Document != null) Doc = ScriptFlow.Read(Editor.Document);
        _active.Doc = Doc.Clone(); _active.Y = Engine.Y;
    }

    private void Activate(DocTabVM t)
    {
        if (Engine.Live) Pause();
        if (Voice.Active) Voice.Stop();
        SnapshotActive();
        foreach (var x in Vm.Tabs) x.Active = x == t;
        _active = t;
        _rehearsal.Clear(); Vm.HasRehearsal = false;
        if (t.Settings != null) { Vm.S.CopyFrom(t.Settings); Vm.LoadFromSettings(); t.Settings = null; }
        ApplyLook(false);
        LoadEditor(t.Doc.Clone());
        _pendingAnchor = null;
        Dispatcher.BeginInvoke(() => { if (t.Y > 0) Engine.SetY(t.Y); else GoTo(0, textLine: true); _forceSync = true; }, DispatcherPriority.Loaded);
        UpdateTitle();
    }

    public void OpenInTab(string name, ScriptDocument doc, string? path, PrompterSettings? settings = null)
    {
        var existing = path != null ? Vm.Tabs.FirstOrDefault(t => string.Equals(t.Path, path, StringComparison.OrdinalIgnoreCase)) : null;
        if (existing != null) { Activate(existing); return; }
        var t = NewTabVM(name, doc, path); t.Settings = settings;
        // Demonstração: um roteiro por vez — o novo entra no lugar do atual (o atual pergunta se quer salvar).
        if (IsDemo && Vm.Tabs.Count > 0)
        {
            var old = Vm.Tabs.ToList();
            // Aba única, sem nome e vazia: troca sem perguntar.
            var blank = old.Count == 1 && old[0].Path == null && !old[0].Dirty && old[0].Doc.Blocks.All(x => string.IsNullOrWhiteSpace(x.PlainText));
            if (!blank && ArcaDialog.Ask(this, "Modo demonstração", "Um roteiro por vez na demonstração", $"Abrir \"{name}\" no lugar de \"{_active?.Name}\"? Com a licença gratuita, vários roteiros ficam abertos em abas.",
                    new[] { "Cancelar", "Abrir no lugar" }, 1, 0) != 1) return;
            Vm.Tabs.Add(t); Activate(t);
            foreach (var o in old) CloseTab(o);
            if (Vm.Tabs.Contains(old[0]) && Vm.Tabs.Count > 1) { Vm.Tabs.Remove(t); Activate(old[0]); return; }   // cancelou ao salvar
            ScheduleSave();
            return;
        }
        Vm.Tabs.Add(t); Activate(t);
        ScheduleSave();
    }

    /// <summary>Demonstração ao abrir: fica só a aba ativa; as outras vão para o histórico de versões (nada se perde).</summary>
    private void DemoTrimTabs()
    {
        if (Vm.Tabs.Count <= 1) return;
        foreach (var t in Vm.Tabs.Where(x => x != _active).ToList())
        {
            _versions = Stores.AddVersion(_versions, t.Name, t.Doc, "demonstração: aba fechada");
            Vm.Tabs.Remove(t);
        }
        Log.Info("Demonstração: só o roteiro ativo ficou aberto (os outros estão no histórico de versões)");
    }

    public void NewTab() => OpenInTab("Sem título.tps", new ScriptDocument { Blocks = { ScriptBlock.Paragraph("") } }, null);

    public void CloseTab(DocTabVM t)
    {
        if (t == _active) SnapshotActive();
        if (t.Dirty)
        {
            var r = ArcaDialog.Ask(this, "Fechar roteiro", $"Salvar as alterações em \"{t.Name}\"?", "Uma versão automática fica no histórico de qualquer forma.", new[] { "Cancelar", "Não salvar", "Salvar" }, primary: 2, cancel: 0);
            if (r == 0) return;
            _versions = Stores.AddVersion(_versions, t.Name, t.Doc, "ao fechar");
            if (r == 2) { if (t != _active) Activate(t); if (!Save(false)) return; }
        }
        var idx = Vm.Tabs.IndexOf(t);
        Vm.Tabs.Remove(t);
        if (Vm.Tabs.Count == 0) { NewTab(); return; }
        if (t == _active) { _active = null; Activate(Vm.Tabs[Math.Max(0, idx - 1)]); }
        ScheduleSave();
    }

    private void MarkDirty() { if (_active != null && !_active.Dirty) { _active.Dirty = true; UpdateTitle(); } }

    private void UpdateTitle()
    {
        var n = _active?.Name ?? "";
        Vm.DocTitle = n + (_active?.Dirty == true ? " •" : "");
        Title = $"{n} — ARCA Teleprompter";
    }

    public string CurrentDocName() => _active?.Name ?? "Roteiro";

    private void ScheduleSave() { _saveT.Stop(); _saveT.Start(); }

    /// <summary>Sessão (todas as abas) em %APPDATA%\ARCA Teleprompter\session.json.</summary>
    private void SaveSession()
    {
        try
        {
            SnapshotActive();
            var ses = new Session { Active = _active?.Id ?? 0, Docs = Vm.Tabs.Select(t => new OpenDoc { Id = t.Id, Name = t.Name, Path = t.Path, Blocks = t.Doc.Blocks, Y = t.Y, Dirty = t.Dirty }).ToList() };
            Stores.SaveSession(ses);
            Stores.SaveSettings(Vm.App);
            Vm.SavedAt = "Salvo automaticamente às " + DateTime.Now.ToString("HH:mm");
        }
        catch (Exception e) { Log.Error("Salvar sessão", e); Vm.SavedAt = "Falha ao salvar automaticamente"; }
    }

    private void AddVersion(string kind)
    {
        if (_active == null) return;
        SnapshotActive();
        _versions = Stores.AddVersion(_versions, _active.Name, Doc, kind);
    }

    // ═══ Arquivos ═══
    public void OpenFileDialog()
    {
        var d = new OpenFileDialog { Title = "Abrir roteiro", Filter = ScriptImporter.Filter, Multiselect = true };
        if (d.ShowDialog(this) == true) foreach (var f in d.FileNames) OpenPath(f);
    }

    public void OpenPath(string path)
    {
        try
        {
            if (Path.GetExtension(path).Equals(".rtf", StringComparison.OrdinalIgnoreCase))
            {
                var fd = new FlowDocument(); var tr = new TextRange(fd.ContentStart, fd.ContentEnd);
                using (var fs = File.OpenRead(path)) tr.Load(fs, DataFormats.Rtf);
                OpenInTab(Path.GetFileNameWithoutExtension(path) + ".tps", ScriptImporter.ApplyMarkers(ScriptFlow.Read(fd)), null);
                return;
            }
            var l = ScriptImporter.Import(path);
            var tps = Path.GetExtension(path).Equals(".tps", StringComparison.OrdinalIgnoreCase);
            OpenInTab(tps ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path) + ".tps", l.Document, tps ? path : null, l.Settings);
            AddRecent(path);
            Log.Info("Aberto: " + path);
        }
        catch (Exception e) { Log.Error("Abrir " + path, e); ArcaDialog.Info(this, "Abrir", $"Não foi possível abrir \"{Path.GetFileName(path)}\".", e.Message, danger: true); }
    }

    private static void AddRecent(string path) { try { System.Windows.Shell.JumpList.AddToRecentCategory(path); } catch { } }

    public bool Save(bool saveAs)
    {
        if (_active == null) return false;
        SnapshotActive();
        var path = _active.Path;
        if (saveAs || path == null || !path.EndsWith(".tps", StringComparison.OrdinalIgnoreCase))
        {
            var d = new SaveFileDialog { Title = "Salvar roteiro", Filter = "Roteiro ARCA (*.tps)|*.tps", FileName = Path.GetFileNameWithoutExtension(_active.Name) + ".tps", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
            if (d.ShowDialog(this) != true) return false;
            path = d.FileName;
        }
        try
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, TpsFile.Serialize(Doc, Vm.S));
            File.Move(tmp, path, true);
            _active.Path = path; _active.Name = Path.GetFileName(path); _active.Dirty = false;
            _versions = Stores.AddVersion(_versions, _active.Name, Doc, "salva");
            UpdateTitle(); ScheduleSave(); AddRecent(path);
            Flash("Salvo · " + _active.Name);
            return true;
        }
        catch (Exception e) { Log.Error("Salvar", e); ArcaDialog.Info(this, "Salvar", "Não foi possível salvar o roteiro.", e.Message, danger: true); return false; }
    }

    private void ExportTxt()
    {
        SnapshotActive();
        var d = new SaveFileDialog { Title = "Exportar texto", Filter = "Texto (*.txt)|*.txt", FileName = Path.GetFileNameWithoutExtension(CurrentDocName()) + ".txt" };
        if (d.ShowDialog(this) == true) { File.WriteAllText(d.FileName, Doc.ToPlainText(), new System.Text.UTF8Encoding(true)); Flash("Exportado · " + Path.GetFileName(d.FileName)); }
    }

    /// <summary>Espelho da redação (MOS) atualizado: abre/atualiza a aba correspondente mantendo a posição.</summary>
    public void ApplyRunningOrder(string roId, string slug, ScriptDocument doc)
    {
        var tag = "mos:" + roId;
        var t = Vm.Tabs.FirstOrDefault(x => x.Path == tag);
        if (t == null) { OpenInTab(slug + " (MOS)", doc, tag); Flash("Espelho recebido da redação: " + slug); return; }
        t.Name = slug + " (MOS)";
        if (t == _active)
        {
            var anchor = _measure.AnchorAt(Engine.Y);
            using (SuppressEdits())
            {
                var fd = ScriptFlow.BuildEditor(doc, Vm.S, _layout, _font); fd.PagePadding = Editor.Document.PagePadding; Editor.Document = fd;
            }
            Doc = doc; RebuildOutputs(anchor); UpdateRundown();
        }
        else t.Doc = doc;
        Flash("Espelho atualizado pela redação");
    }
}
