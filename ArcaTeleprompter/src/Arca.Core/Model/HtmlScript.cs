using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Arca.Core.Model;

/// <summary>
/// Conversão entre o documento e o HTML do formato .tps v1 (o mesmo do protótipo):
/// <c>&lt;p data-bm&gt;</c> = marcador, <c>&lt;p data-stop&gt;</c> = pausa, <c>data-who="N"</c> = apresentador,
/// <c>data-skip</c> = marcador pulado; dentro dos parágrafos: b/strong, i/em, u e span com color.
/// </summary>
public static partial class HtmlScript
{
    [GeneratedRegex(@"<(/?)([a-zA-Z0-9]+)([^>]*)>|([^<]+)", RegexOptions.Singleline)]
    private static partial Regex TokenRx();
    [GeneratedRegex(@"([a-zA-Z-]+)(?:\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+))?")]
    private static partial Regex AttrRx();
    [GeneratedRegex(@"color\s*:\s*([^;""']+)", RegexOptions.IgnoreCase)]
    private static partial Regex ColorRx();

    private sealed record Fmt(bool B, bool I, bool U, string? C);

    public static ScriptDocument Parse(string html)
    {
        var doc = new ScriptDocument();
        ScriptBlock? cur = null;
        var stack = new Stack<(string Tag, Fmt F)>();
        var fmt = new Fmt(false, false, false, null);
        var sb = new StringBuilder();

        void FlushText()
        {
            if (sb.Length == 0) return;
            cur ??= NewBlock(doc, null);
            var t = WebUtility.HtmlDecode(sb.ToString());
            t = Regex.Replace(t, @"\s+", " ");
            sb.Clear();
            if (t.Length == 0) return;
            var run = new TextRun(t, fmt.B, fmt.I, fmt.U, fmt.C);
            if (cur.Runs.Count > 0 && cur.Runs[^1].SameFormat(run)) cur.Runs[^1] = run with { Text = cur.Runs[^1].Text + t };
            else cur.Runs.Add(run);
        }
        void EndBlock()
        {
            FlushText();
            if (cur != null) { Trim(cur); }
            cur = null; fmt = new Fmt(false, false, false, null); stack.Clear();
        }

        foreach (Match m in TokenRx().Matches(html))
        {
            if (m.Groups[4].Success) { sb.Append(m.Groups[4].Value); continue; }
            var closing = m.Groups[1].Value == "/";
            var tag = m.Groups[2].Value.ToLowerInvariant();
            var attrs = ParseAttrs(m.Groups[3].Value);
            if (tag is "p" or "div" or "h1" or "h2" or "h3" or "li")
            {
                EndBlock();
                if (!closing) cur = NewBlock(doc, attrs);
                continue;
            }
            if (tag == "br") { sb.Append(' '); continue; }
            FlushText();
            if (closing)
            {
                while (stack.Count > 0) { var (t, f) = stack.Pop(); if (t == tag) { fmt = f; break; } }
                continue;
            }
            var next = fmt;
            if (tag is "b" or "strong") next = next with { B = true };
            else if (tag is "i" or "em") next = next with { I = true };
            else if (tag == "u") next = next with { U = true };
            if (attrs.TryGetValue("style", out var st))
            {
                var cm = ColorRx().Match(st); if (cm.Success) next = next with { C = cm.Groups[1].Value.Trim() };
                if (st.Contains("font-weight:", StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(st, @"font-weight\s*:\s*(bold|[6-9]00)")) next = next with { B = true };
                if (Regex.IsMatch(st, @"font-style\s*:\s*italic")) next = next with { I = true };
                if (Regex.IsMatch(st, @"text-decoration[^;]*underline")) next = next with { U = true };
            }
            if (tag == "font" && attrs.TryGetValue("color", out var fc)) next = next with { C = fc };
            stack.Push((tag, fmt));
            fmt = next;
        }
        EndBlock();
        doc.Blocks.RemoveAll(b => b.Kind == BlockKind.Text && b.Runs.Count == 0 && doc.Blocks.Count > 1);
        if (doc.Blocks.Count == 0) doc.Blocks.Add(ScriptBlock.Paragraph(""));
        return doc;
    }

    private static ScriptBlock NewBlock(ScriptDocument doc, Dictionary<string, string>? attrs)
    {
        var b = new ScriptBlock();
        if (attrs != null)
        {
            if (attrs.ContainsKey("data-bm")) { b.Kind = BlockKind.Bookmark; b.Skip = attrs.ContainsKey("data-skip"); }
            else if (attrs.ContainsKey("data-stop")) b.Kind = BlockKind.Stop;
            if (attrs.TryGetValue("data-who", out var w) && int.TryParse(w, out var n) && n is >= 1 and <= 8 && b.Kind == BlockKind.Text) b.Who = n;
        }
        doc.Blocks.Add(b);
        return b;
    }

    private static void Trim(ScriptBlock b)
    {
        if (b.Runs.Count == 0) return;
        b.Runs[0] = b.Runs[0] with { Text = b.Runs[0].Text.TrimStart() };
        b.Runs[^1] = b.Runs[^1] with { Text = b.Runs[^1].Text.TrimEnd() };
        b.Runs.RemoveAll(r => r.Text.Length == 0);
    }

    private static Dictionary<string, string> ParseAttrs(string s)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in AttrRx().Matches(s))
        {
            var v = m.Groups[2].Success ? m.Groups[2].Value.Trim('"', '\'') : "";
            d[m.Groups[1].Value] = WebUtility.HtmlDecode(v);
        }
        return d;
    }

    /// <summary>HTML compatível com o .tps v1 / protótipo.</summary>
    public static string ToHtml(ScriptDocument doc)
    {
        var sb = new StringBuilder();
        foreach (var b in doc.Blocks)
        {
            switch (b.Kind)
            {
                case BlockKind.Bookmark:
                    sb.Append("<p data-bm=\"1\"").Append(b.Skip ? " data-skip=\"1\"" : "").Append(" contenteditable=\"false\">").Append(Enc(b.PlainText)).Append("</p>");
                    break;
                case BlockKind.Stop:
                    sb.Append("<p data-stop=\"1\" contenteditable=\"false\">").Append(Enc(b.PlainText)).Append("</p>");
                    break;
                default:
                    sb.Append(b.Who > 0 ? $"<p data-who=\"{b.Who}\">" : "<p>");
                    if (b.Runs.Count == 0) sb.Append("<br>");
                    foreach (var r in b.Runs) sb.Append(RunHtml(r));
                    sb.Append("</p>");
                    break;
            }
        }
        return sb.ToString();
    }

    public static string RunHtml(TextRun r)
    {
        var t = Enc(r.Text);
        if (r.Color != null) t = $"<span style=\"color:{Enc(r.Color)}\">{t}</span>";
        if (r.Underline) t = $"<u>{t}</u>";
        if (r.Italic) t = $"<i>{t}</i>";
        if (r.Bold) t = $"<b>{t}</b>";
        return t;
    }

    public static string Enc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
