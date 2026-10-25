using System.Text;
using Arca.Core.Model;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Arca.Import;

/// <summary>
/// Importa roteiros: .tps/.json (nativo), .docx (Word, com negrito/itálico/sublinhado/cor), .pdf (texto),
/// .txt e .html. Convenções de texto: linha iniciada por "*" vira marcador e "[PAUSA]" vira pausa.
/// RTF é lido pelo app (RichTextBox do WPF), fora desta biblioteca.
/// </summary>
public static class ScriptImporter
{
    public static readonly string[] Extensions = { ".tps", ".json", ".docx", ".pdf", ".txt", ".html", ".htm" };

    public static string Filter => "Roteiros e documentos|*.tps;*.json;*.docx;*.pdf;*.txt;*.html;*.htm;*.rtf|Roteiro ARCA (*.tps)|*.tps|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf|Texto (*.txt)|*.txt|Todos os arquivos|*.*";

    public static TpsFile.Loaded Import(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".tps" or ".json" => TpsFile.Parse(File.ReadAllText(path, Encoding.UTF8)),
            ".docx" => new(FromDocx(path), null),
            ".pdf" => new(FromPdf(path), null),
            ".html" or ".htm" => new(FromHtml(File.ReadAllText(path, Encoding.UTF8)), null),
            _ => new(ScriptDocument.FromPlainText(ReadText(path)), null)
        };
    }

    /// <summary>Lê texto detectando UTF-8 (com ou sem BOM) e caindo para Windows-1252.</summary>
    public static string ReadText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿'); }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    /// <summary>HTML de roteiro (com data-bm etc.) ou página comum: usa o conversor do .tps.</summary>
    public static ScriptDocument FromHtml(string html)
    {
        var body = html;
        var i = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (i >= 0) { var j = html.IndexOf('>', i); if (j > 0) body = html[(j + 1)..]; }
        body = System.Text.RegularExpressions.Regex.Replace(body, @"<(script|style)[\s\S]*?</\1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var doc = HtmlScript.Parse(body);
        return ApplyMarkers(doc);
    }

    public static ScriptDocument FromDocx(string path)
    {
        using var wd = WordprocessingDocument.Open(path, false);
        var body = wd.MainDocumentPart?.Document.Body ?? throw new InvalidDataException("Documento Word vazio.");
        var doc = new ScriptDocument();
        foreach (var p in body.Descendants<W.Paragraph>())
        {
            var b = new ScriptBlock();
            foreach (var r in p.Descendants<W.Run>())
            {
                var sb = new StringBuilder();
                foreach (var el in r.ChildElements)
                {
                    if (el is W.Text t) sb.Append(t.Text);
                    else if (el is W.TabChar or W.Break or W.CarriageReturn) sb.Append(' ');
                }
                if (sb.Length == 0) continue;
                var rp = r.RunProperties;
                bool On(DocumentFormat.OpenXml.OpenXmlLeafElement? e) => e != null && (e.GetAttributes().All(a => a.LocalName != "val") || e.GetAttributes().Any(a => a.LocalName == "val" && a.Value is "1" or "true" or "on"));
                var color = rp?.Color?.Val?.Value;
                var run = new TextRun(sb.ToString(),
                    On(rp?.Bold), On(rp?.Italic),
                    rp?.Underline?.Val?.Value is { } u && u != W.UnderlineValues.None,
                    color is null or "auto" or "000000" ? null : "#" + color.ToLowerInvariant());
                if (b.Runs.Count > 0 && b.Runs[^1].SameFormat(run)) b.Runs[^1] = run with { Text = b.Runs[^1].Text + run.Text };
                else b.Runs.Add(run);
            }
            if (b.PlainText.Trim().Length == 0) continue;
            doc.Blocks.Add(b);
        }
        if (doc.Blocks.Count == 0) doc.Blocks.Add(ScriptBlock.Paragraph(""));
        return ApplyMarkers(doc);
    }

    /// <summary>
    /// PDF: junta as palavras em linhas pela posição vertical e separa parágrafos quando o espaço entre
    /// linhas é maior que ~1,6 vez a altura da letra.
    /// </summary>
    public static ScriptDocument FromPdf(string path)
    {
        using var pdf = PdfDocument.Open(path);
        var paras = new List<string>();
        foreach (var page in pdf.GetPages())
        {
            var words = page.GetWords().OrderByDescending(w => Math.Round(w.BoundingBox.Bottom, 1)).ThenBy(w => w.BoundingBox.Left).ToList();
            var lines = new List<(double Y, double H, string Text)>();
            foreach (var g in GroupLines(words))
                lines.Add((g.Y, g.H, string.Join(' ', g.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))));
            var cur = new StringBuilder(); double? lastY = null; double lastH = 10;
            foreach (var (y, h, text) in lines)
            {
                if (lastY is { } ly && ly - y > Math.Max(h, lastH) * 1.6 && cur.Length > 0) { paras.Add(cur.ToString().Trim()); cur.Clear(); }
                if (cur.Length > 0) cur.Append(' ');
                cur.Append(text);
                lastY = y; lastH = h;
            }
            if (cur.Length > 0) paras.Add(cur.ToString().Trim());
        }
        return ScriptDocument.FromPlainText(string.Join("\n", paras.Select(p => System.Text.RegularExpressions.Regex.Replace(p, @"\s+", " "))));
    }

    private static IEnumerable<(double Y, double H, List<Word> Words)> GroupLines(List<Word> words)
    {
        var result = new List<(double Y, double H, List<Word> Words)>();
        foreach (var w in words)
        {
            var y = w.BoundingBox.Bottom; var h = Math.Max(1, w.BoundingBox.Height);
            var i = result.FindIndex(l => Math.Abs(l.Y - y) < h * 0.5);
            if (i >= 0) result[i].Words.Add(w); else result.Add((y, h, new List<Word> { w }));
        }
        return result.OrderByDescending(l => l.Y);
    }

    /// <summary>Converte parágrafos "*Nome" em marcadores e "[PAUSA] …" em pausas.</summary>
    public static ScriptDocument ApplyMarkers(ScriptDocument doc)
    {
        for (var i = 0; i < doc.Blocks.Count; i++)
        {
            var b = doc.Blocks[i];
            if (b.Kind != BlockKind.Text) continue;
            var t = b.PlainText.Trim();
            if (t.Length > 1 && t[0] == '*') doc.Blocks[i] = ScriptBlock.Mark(t[1..].Trim());
            else if (t.StartsWith("[pausa]", StringComparison.OrdinalIgnoreCase)) { var n = t[7..].Trim(); doc.Blocks[i] = ScriptBlock.Pause(n.Length > 0 ? n : "Pausa"); }
        }
        return doc;
    }
}
