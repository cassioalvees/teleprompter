namespace Arca.Core.Model;

/// <summary>Tipo de bloco do roteiro.</summary>
public enum BlockKind
{
    /// <summary>Parágrafo de texto lido pelo apresentador.</summary>
    Text,
    /// <summary>Marcador: inicia um bloco do rundown (atalhos 1–9, PageUp/PageDown).</summary>
    Bookmark,
    /// <summary>Ponto de pausa: a rolagem para ao alcançá-lo.</summary>
    Stop
}

/// <summary>Trecho de texto com formatação.</summary>
public sealed record TextRun(string Text, bool Bold = false, bool Italic = false, bool Underline = false, string? Color = null)
{
    public bool SameFormat(TextRun o) => Bold == o.Bold && Italic == o.Italic && Underline == o.Underline && string.Equals(Color, o.Color, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Um parágrafo do roteiro. Marcadores e pausas têm um único run com o nome.
/// </summary>
public sealed class ScriptBlock
{
    public BlockKind Kind { get; set; } = BlockKind.Text;
    public List<TextRun> Runs { get; set; } = new();
    /// <summary>Apresentador (1–8); 0 = sem apresentador. Só em blocos de texto.</summary>
    public int Who { get; set; }
    /// <summary>Marcador cujo bloco não vai ao ar (fica oculto no prompter).</summary>
    public bool Skip { get; set; }

    public string PlainText => string.Concat(Runs.Select(r => r.Text));
    public int WordCount => TextUtil.CountWords(PlainText);

    public static ScriptBlock Paragraph(string text, int who = 0) => new() { Runs = { new TextRun(text) }, Who = who };
    public static ScriptBlock Mark(string name, bool skip = false) => new() { Kind = BlockKind.Bookmark, Runs = { new TextRun(name) }, Skip = skip };
    public static ScriptBlock Pause(string name) => new() { Kind = BlockKind.Stop, Runs = { new TextRun(name) } };

    public ScriptBlock Clone() => new() { Kind = Kind, Who = Who, Skip = Skip, Runs = Runs.ToList() };
}

/// <summary>Bloco calculado do rundown (de um marcador até o próximo).</summary>
public sealed record RundownBlock(int BookmarkIndex, string Name, int Words, bool Skip);

/// <summary>Documento do roteiro: lista ordenada de blocos.</summary>
public sealed class ScriptDocument
{
    public List<ScriptBlock> Blocks { get; set; } = new();

    public ScriptDocument Clone() => new() { Blocks = Blocks.Select(b => b.Clone()).ToList() };

    /// <summary>Palavras lidas (exclui marcadores e pausas).</summary>
    public int WordCount => Blocks.Where(b => b.Kind == BlockKind.Text).Sum(b => b.WordCount);

    /// <summary>
    /// Para cada bloco, indica se ele está dentro de um marcador pulado
    /// (fica riscado no editor e oculto no prompter). Marcadores nunca são "pulados" por si.
    /// </summary>
    public bool[] SkippedMask()
    {
        var mask = new bool[Blocks.Count];
        var skip = false;
        for (var i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];
            if (b.Kind == BlockKind.Bookmark) { skip = b.Skip; continue; }
            mask[i] = skip;
        }
        return mask;
    }

    /// <summary>
    /// Índices dos blocos que iniciam uma fala (primeiro parágrafo de cada sequência do mesmo apresentador).
    /// O nome do apresentador aparece acima deles.
    /// </summary>
    public bool[] WhoStartMask()
    {
        var mask = new bool[Blocks.Count];
        var prev = 0;
        for (var i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];
            if (b.Kind != BlockKind.Text) { prev = 0; continue; }
            mask[i] = b.Who != 0 && b.Who != prev;
            prev = b.Who;
        }
        return mask;
    }

    /// <summary>Blocos do rundown, com contagem de palavras de cada um.</summary>
    public List<RundownBlock> Rundown()
    {
        var list = new List<RundownBlock>();
        int words = 0, bm = -1; string name = ""; var skip = false;
        void Flush() { if (bm >= 0) list.Add(new RundownBlock(bm, name, words, skip)); }
        for (var i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];
            if (b.Kind == BlockKind.Bookmark) { Flush(); bm = i; name = b.PlainText.Trim(); skip = b.Skip; words = 0; continue; }
            if (b.Kind == BlockKind.Text && bm >= 0) words += b.WordCount;
        }
        Flush();
        return list;
    }

    /// <summary>
    /// Move um bloco do rundown (marcador + conteúdo até o próximo marcador) para antes do marcador de destino.
    /// <paramref name="to"/> igual ao número de marcadores move para o fim.
    /// </summary>
    public bool MoveRundownBlock(int from, int to)
    {
        var marks = Blocks.Select((b, i) => (b, i)).Where(x => x.b.Kind == BlockKind.Bookmark).Select(x => x.i).ToList();
        if (from == to || from < 0 || from >= marks.Count || to < 0 || to > marks.Count) return false;
        var start = marks[from];
        var end = from + 1 < marks.Count ? marks[from + 1] : Blocks.Count;
        if (to == from + 1) return false;
        var chunk = Blocks.GetRange(start, end - start);
        Blocks.RemoveRange(start, end - start);
        int insertAt;
        if (to >= marks.Count) insertAt = Blocks.Count;
        else
        {
            var target = marks[to];
            insertAt = target > start ? target - chunk.Count : target;
        }
        Blocks.InsertRange(insertAt, chunk);
        return true;
    }

    /// <summary>Remove o apresentador <paramref name="n"/> (1–8) e renumera os seguintes.</summary>
    public void RemovePresenter(int n)
    {
        foreach (var b in Blocks)
        {
            if (b.Who == n) b.Who = 0;
            else if (b.Who > n) b.Who--;
        }
    }

    /// <summary>Texto simples com as convenções de importação (*marcador, [PAUSA]).</summary>
    public string ToPlainText() => string.Join("\r\n\r\n", Blocks.Select(b => b.Kind switch
    {
        BlockKind.Bookmark => "*" + b.PlainText,
        BlockKind.Stop => "[PAUSA] " + b.PlainText,
        _ => b.PlainText
    }));

    /// <summary>
    /// Converte texto simples: uma linha por parágrafo; "*Nome" vira marcador e "[PAUSA] x" vira pausa.
    /// </summary>
    public static ScriptDocument FromPlainText(string text)
    {
        var doc = new ScriptDocument();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var l = raw.Trim();
            if (l.Length == 0) continue;
            if (l.StartsWith('*') && l.Length > 1) doc.Blocks.Add(ScriptBlock.Mark(l[1..].Trim()));
            else if (l.StartsWith("[pausa]", StringComparison.OrdinalIgnoreCase))
            {
                var name = l[7..].Trim();
                doc.Blocks.Add(ScriptBlock.Pause(name.Length > 0 ? name : "Pausa"));
            }
            else doc.Blocks.Add(ScriptBlock.Paragraph(l));
        }
        if (doc.Blocks.Count == 0) doc.Blocks.Add(ScriptBlock.Paragraph(""));
        return doc;
    }
}
