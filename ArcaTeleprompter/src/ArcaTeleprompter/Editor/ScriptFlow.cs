using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Arca.Core.Engine;
using Arca.Core.Model;

namespace Arca.App.Editor;

/// <summary>
/// Espaçamento do texto — o mesmo no editor, no prompter, nas saídas e no NDI (a web segue as mesmas proporções no CSS).
/// </summary>
public static class Spacing
{
    /// <summary>Espaço depois de cada parágrafo: fração da altura da linha (configurável).</summary>
    public static double After(PrompterSettings s, PrompterLayout l) => l.FontSize * s.LineHeight * Math.Clamp(s.ParagraphGap, 0, 1.5);
    /// <summary>Espaço acima de um parágrafo que começa uma fala (onde vai o nome do apresentador).</summary>
    public static double NameRoom(double fs) => fs * 0.75;
    /// <summary>
    /// Topo do nome do apresentador em relação ao topo do parágrafo: o nome (0,3 da fonte, linha de 1,5) fica encostado
    /// no próprio texto, como no protótipo — o espaço de <see cref="NameRoom"/> sobra acima dele, separando da fala anterior.
    /// </summary>
    public static double NameOffset(double fs) => fs * 0.45;
    /// <summary>Alinhamento do texto (esquerda/centro/direita) — o nome do apresentador e o marcador seguem o mesmo.</summary>
    public static TextAlignment Align(string? a) => a switch { "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => TextAlignment.Left };
    /// <summary>Linha em branco digitada: meia linha (aparece, mas não abre um buraco).</summary>
    public const double BlankLine = 0.5;

    /// <summary>Aplica (ou desfaz) o estilo de linha em branco num parágrafo.</summary>
    public static void Blank(Paragraph p, bool blank, PrompterSettings s, PrompterLayout l)
    {
        if (blank)
        {
            p.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            p.LineHeight = l.FontSize * s.LineHeight * BlankLine;
        }
        else
        {
            p.ClearValue(Block.LineStackingStrategyProperty);
            p.ClearValue(Block.LineHeightProperty);
        }
    }
}

/// <summary>
/// Metadados de um parágrafo de texto no editor (apresentador). Imutável de propósito: ao quebrar um parágrafo
/// (Enter, colar várias linhas) o WPF copia o Tag — o mesmo objeto — para o parágrafo novo. Se o objeto pudesse
/// ser alterado, mudar o apresentador de um parágrafo mudaria o de todos os que vieram dele.
/// </summary>
public sealed class BlockTag
{
    public int Who { get; init; }
}

/// <summary>
/// Conversões entre <see cref="ScriptDocument"/> e FlowDocument do WPF:
/// <list type="bullet">
/// <item>documento do prompter (somente leitura, sem blocos pulados) — usado por todas as saídas;</item>
/// <item>documento do editor (marcadores e pausas como blocos não editáveis, apresentação aplicada).</item>
/// </list>
/// </summary>
public static class ScriptFlow
{
    public static Brush ParseBrush(string? hex, Brush fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }
        catch (FormatException) { return fallback; }
    }

    public static Color ParseColor(string? hex, Color fallback)
    {
        try { return hex == null ? fallback : (Color)ColorConverter.ConvertFromString(hex); } catch (FormatException) { return fallback; }
    }

    public static Brush WithAlpha(Color c, double a) { var b = new SolidColorBrush(Color.FromArgb((byte)(a * 255), c.R, c.G, c.B)); b.Freeze(); return b; }

    /// <summary>Monta os runs de um bloco de texto.</summary>
    public static IEnumerable<Inline> Inlines(ScriptBlock b)
    {
        foreach (var r in b.Runs)
        {
            var run = new Run(r.Text);
            if (r.Bold) run.FontWeight = FontWeights.Bold;
            if (r.Italic) run.FontStyle = FontStyles.Italic;
            if (r.Underline) run.TextDecorations = TextDecorations.Underline;
            if (r.Color != null) run.Foreground = ParseBrush(r.Color, Brushes.White);
            yield return run;
        }
    }

    // ——— Documento do prompter ———

    /// <summary>
    /// FlowDocument das saídas. Cada parágrafo leva Tag = índice do bloco no documento (para âncoras).
    /// Retorna também os blocos que iniciam uma fala (para desenhar o nome do apresentador acima).
    /// </summary>
    public static FlowDocument BuildPrompter(ScriptDocument doc, PrompterSettings s, PrompterLayout l, FontFamily font, out List<(int Block, int Who)> whoStarts)
    {
        var fs = l.FontSize;
        var fg = ParseColor(s.Foreground, Colors.White);
        var fd = new FlowDocument
        {
            FontFamily = font, FontSize = fs, Foreground = new SolidColorBrush(fg),
            PagePadding = new Thickness(l.PaddingLeft, 0, l.PaddingRight, 0),
            LineHeight = fs * s.LineHeight, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            TextAlignment = s.Align switch { "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => TextAlignment.Left },
            PageWidth = PrompterLayout.Width, ColumnWidth = 99999, IsHyphenationEnabled = false,
            IsOptimalParagraphEnabled = false
        };
        whoStarts = new();
        var skipped = doc.SkippedMask();
        var starts = doc.WhoStartMask();
        var only = int.TryParse(s.PresenterOnly, out var o) ? o : 0;
        var mono = FontCatalog.Family("mono");
        for (var i = 0; i < doc.Blocks.Count; i++)
        {
            if (skipped[i]) continue;
            var b = doc.Blocks[i];
            Paragraph p;
            if (b.Kind == BlockKind.Bookmark)
            {
                p = new Paragraph(new Run(b.PlainText.ToUpperInvariant()))
                {
                    FontFamily = mono, FontSize = fs * 0.3, LineHeight = fs * 0.3 * 1.4,
                    Foreground = WithAlpha(fg, 0.55), BorderBrush = WithAlpha(fg, 0.55), BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(0, fs * 0.21, 0, fs * 0.06), Margin = new Thickness(0, fs * 0.36, 0, fs * 0.24)
                };
                Typography.SetCapitalSpacing(p, true);
            }
            else if (b.Kind == BlockKind.Stop)
            {
                p = new Paragraph(new Run("PAUSA · " + b.PlainText.ToUpperInvariant()))
                {
                    FontFamily = mono, FontSize = fs * 0.3, LineHeight = fs * 0.3 * 1.4,
                    Foreground = WithAlpha(fg, 0.65), BorderBrush = WithAlpha(fg, 0.5), BorderThickness = new Thickness(1),
                    Padding = new Thickness(fs * 0.27, fs * 0.12, fs * 0.27, fs * 0.12), Margin = new Thickness(0, 0, 0, Spacing.After(s, l)),
                    TextAlignment = TextAlignment.Left
                };
            }
            else
            {
                p = new Paragraph { Margin = new Thickness(0, 0, 0, Spacing.After(s, l)) };
                if (string.IsNullOrWhiteSpace(b.PlainText)) { Spacing.Blank(p, true, s, l); p.Margin = new Thickness(0); }
                p.Inlines.AddRange(Inlines(b));
                if (b.Runs.Count == 0) p.Inlines.Add(new Run(" "));
                if (b.Who > 0 && b.Who <= s.Presenters.Count) p.Foreground = ParseBrush(s.Presenters[b.Who - 1].Color, p.Foreground);
                if (starts[i] && s.PresenterNames) { p.Margin = new Thickness(0, Spacing.NameRoom(fs), 0, Spacing.After(s, l)); whoStarts.Add((i, b.Who)); }
                if (only > 0 && b.Who != only) p.Foreground = WithAlpha(((SolidColorBrush)(p.Foreground ?? fd.Foreground)).Color, 0.22);
            }
            p.Tag = i;
            fd.Blocks.Add(p);
        }
        if (fd.Blocks.Count == 0) fd.Blocks.Add(new Paragraph(new Run(" ")) { Tag = 0 });
        return fd;
    }

    // ——— Documento do editor ———

    public static FlowDocument BuildEditor(ScriptDocument doc, PrompterSettings s, PrompterLayout l, FontFamily font)
    {
        var fs = l.FontSize;
        var fd = new FlowDocument
        {
            FontFamily = font, FontSize = fs, Foreground = ParseBrush(s.Foreground, Brushes.White),
            PagePadding = new Thickness(l.PaddingLeft, 0, l.PaddingRight, 0),
            LineHeight = fs * s.LineHeight, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            PageWidth = PrompterLayout.Width, ColumnWidth = 99999, IsOptimalParagraphEnabled = false
        };
        foreach (var b in doc.Blocks) fd.Blocks.Add(EditorBlock(b, fs));
        if (fd.Blocks.Count == 0) fd.Blocks.Add(new Paragraph { Tag = new BlockTag() });
        ApplyPresentation(fd, s, l);
        return fd;
    }

    public static Block EditorBlock(ScriptBlock b, double fs)
    {
        if (b.Kind != BlockKind.Text)
            return new BlockUIContainer(new MarkChip { Kind = b.Kind, Text = b.PlainText, Skip = b.Skip }) { Margin = new Thickness(0) };
        var p = new Paragraph { Tag = new BlockTag { Who = b.Who } };
        p.Inlines.AddRange(Inlines(b));
        return p;
    }

    /// <summary>
    /// Reaplica a apresentação (cores dos apresentadores, blocos pulados riscados, tamanhos dos marcadores)
    /// sem recriar o documento — preserva cursor e desfazer.
    /// </summary>
    public static void ApplyPresentation(FlowDocument fd, PrompterSettings s, PrompterLayout l)
    {
        var fs = l.FontSize;
        var fg = ParseColor(s.Foreground, Colors.White);
        fd.FontSize = fs; fd.LineHeight = fs * s.LineHeight;
        fd.PagePadding = new Thickness(l.PaddingLeft, fd.PagePadding.Top, l.PaddingRight, fd.PagePadding.Bottom);
        fd.Foreground = new SolidColorBrush(fg);
        fd.TextAlignment = s.Align switch { "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => TextAlignment.Left };
        var skip = false; int prevWho = 0;
        foreach (var block in fd.Blocks)
        {
            if (block is BlockUIContainer { Child: MarkChip chip })
            {
                chip.Apply(fs, fg, Spacing.After(s, l));
                if (chip.Kind == BlockKind.Bookmark) skip = chip.Skip;
                prevWho = 0;
                continue;
            }
            if (block is not Paragraph p) continue;
            var tag = p.Tag as BlockTag ?? new BlockTag();
            p.Tag = new BlockTag { Who = tag.Who }; // um objeto por parágrafo (separa os que o WPF compartilhou)
            var c = tag.Who > 0 && tag.Who <= s.Presenters.Count ? ParseColor(s.Presenters[tag.Who - 1].Color, fg) : fg;
            p.Foreground = skip ? WithAlpha(c, 0.32) : new SolidColorBrush(c);
            p.TextDecorations = skip ? TextDecorations.Strikethrough : null;
            var start = tag.Who > 0 && tag.Who != prevWho && s.PresenterNames;
            // Igual ao prompter: espaço entre parágrafos proporcional à linha; linha em branco = meia linha.
            var blank = new TextRange(p.ContentStart, p.ContentEnd).Text.Trim().Length == 0 && p.Inlines.Count <= 1;
            Spacing.Blank(p, blank, s, l);
            p.Margin = blank ? new Thickness(0) : new Thickness(0, start ? Spacing.NameRoom(fs) : 0, 0, Spacing.After(s, l));
            prevWho = tag.Who;
        }
    }

    /// <summary>Lê o documento do editor de volta para o modelo.</summary>
    public static ScriptDocument Read(FlowDocument fd)
    {
        var doc = new ScriptDocument();
        foreach (var block in fd.Blocks) ReadBlock(block, doc);
        if (doc.Blocks.Count == 0) doc.Blocks.Add(ScriptBlock.Paragraph(""));
        return doc;
    }

    private static void ReadBlock(Block block, ScriptDocument doc)
    {
        switch (block)
        {
            case BlockUIContainer { Child: MarkChip chip }:
                doc.Blocks.Add(chip.Kind == BlockKind.Bookmark ? ScriptBlock.Mark(chip.Text, chip.Skip) : ScriptBlock.Pause(chip.Text));
                break;
            case Paragraph p:
                var b = new ScriptBlock { Who = (p.Tag as BlockTag)?.Who ?? 0 };
                foreach (var inl in p.Inlines) ReadInline(inl, b, new TextRun(""));
                // Junta runs vizinhos com a mesma formatação.
                var merged = new List<TextRun>();
                foreach (var r in b.Runs)
                {
                    if (r.Text.Length == 0) continue;
                    if (merged.Count > 0 && merged[^1].SameFormat(r)) merged[^1] = merged[^1] with { Text = merged[^1].Text + r.Text };
                    else merged.Add(r);
                }
                b.Runs = merged;
                doc.Blocks.Add(b);
                break;
            case List list:
                foreach (var li in list.ListItems) foreach (var lb in li.Blocks) ReadBlock(lb, doc);
                break;
            case Section sec:
                foreach (var sb in sec.Blocks) ReadBlock(sb, doc);
                break;
            case Table t:
                foreach (var rg in t.RowGroups) foreach (var row in rg.Rows) foreach (var cell in row.Cells) foreach (var cb in cell.Blocks) ReadBlock(cb, doc);
                break;
        }
    }

    /// <summary>Formatação vem só de valores locais (a cor do apresentador fica no parágrafo, não no texto).</summary>
    private static void ReadInline(Inline inl, ScriptBlock b, TextRun f)
    {
        f = f with
        {
            Bold = f.Bold || (inl.ReadLocalValue(TextElement.FontWeightProperty) is FontWeight w && w.ToOpenTypeWeight() >= 600),
            Italic = f.Italic || (inl.ReadLocalValue(TextElement.FontStyleProperty) is FontStyle st && st != FontStyles.Normal),
            Underline = f.Underline || (inl.ReadLocalValue(Inline.TextDecorationsProperty) is TextDecorationCollection td && td.Any(d => d.Location == TextDecorationLocation.Underline)),
            Color = inl.ReadLocalValue(TextElement.ForegroundProperty) is SolidColorBrush cb ? $"#{cb.Color.R:x2}{cb.Color.G:x2}{cb.Color.B:x2}" : f.Color
        };
        switch (inl)
        {
            case Run r: b.Runs.Add(f with { Text = r.Text.Replace(' ', ' ') }); break;
            case LineBreak: b.Runs.Add(f with { Text = " " }); break;
            case Span sp: foreach (var c in sp.Inlines) ReadInline(c, b, f); break;
            case InlineUIContainer: break;
        }
    }

    /// <summary>Índice do bloco do modelo correspondente a um bloco do FlowDocument do editor.</summary>
    public static int IndexOf(FlowDocument fd, Block target)
    {
        var i = 0;
        foreach (var b in fd.Blocks) { if (b == target) return i; i++; }
        return -1;
    }

    /// <summary>Bloco de topo que contém a posição.</summary>
    public static Block? TopBlock(TextPointer? p, FlowDocument fd)
    {
        if (p == null) return null;
        DependencyObject? cur = p.Parent;
        while (cur is TextElement te)
        {
            if (te is Block blk && blk.Parent == fd) return blk;
            cur = te.Parent;
        }
        // Posição entre blocos: o próximo bloco.
        return p.Paragraph is { } para ? (para.Parent == fd ? para : null) : fd.Blocks.FirstOrDefault(b => b.ContentStart.CompareTo(p) >= 0);
    }
}
