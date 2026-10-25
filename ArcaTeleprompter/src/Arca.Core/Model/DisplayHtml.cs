using System.Text;

namespace Arca.Core.Model;

/// <summary>
/// HTML do roteiro para as saídas web (monitor/PWA): sem os blocos pulados, com classes para marcador,
/// pausa e apresentador, e o nome do apresentador no início de cada fala. Cada parágrafo leva
/// <c>data-i</c> com o índice no documento, usado para sincronizar a posição (âncora bloco + fração).
/// </summary>
public static class DisplayHtml
{
    public static string Build(ScriptDocument doc, PrompterSettings s)
    {
        var skipped = doc.SkippedMask();
        var starts = doc.WhoStartMask();
        var sb = new StringBuilder();
        for (var i = 0; i < doc.Blocks.Count; i++)
        {
            if (skipped[i]) continue;
            var b = doc.Blocks[i];
            switch (b.Kind)
            {
                case BlockKind.Bookmark:
                    sb.Append($"<p class=\"bm\" data-i=\"{i}\">").Append(HtmlScript.Enc(b.PlainText)).Append("</p>");
                    break;
                case BlockKind.Stop:
                    sb.Append($"<p class=\"stop\" data-i=\"{i}\">Pausa · ").Append(HtmlScript.Enc(b.PlainText)).Append("</p>");
                    break;
                default:
                    // Linha em branco: meia linha (classe "blank" no CSS), igual ao app.
                    if (string.IsNullOrWhiteSpace(b.PlainText)) { sb.Append($"<p class=\"blank\" data-i=\"{i}\"></p>"); break; }
                    sb.Append($"<p data-i=\"{i}\"");
                    if (b.Who > 0)
                    {
                        sb.Append($" class=\"w w{b.Who}{(starts[i] ? " ws" : "")}\"");
                        if (starts[i] && b.Who <= s.Presenters.Count) sb.Append(" data-name=\"").Append(HtmlScript.Enc(s.Presenters[b.Who - 1].Name)).Append('"');
                    }
                    sb.Append('>');
                    if (b.Runs.Count == 0) sb.Append("&nbsp;");
                    foreach (var r in b.Runs) sb.Append(HtmlScript.RunHtml(r));
                    sb.Append("</p>");
                    break;
            }
        }
        return sb.ToString();
    }
}
