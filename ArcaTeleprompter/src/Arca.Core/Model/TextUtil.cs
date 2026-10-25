using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Arca.Core.Model;

/// <summary>Funções de texto compartilhadas (contagem de palavras, normalização para a voz).</summary>
public static partial class TextUtil
{
    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRx();

    public static int CountWords(string text) => string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Palavras (letras e números) com a posição no texto.</summary>
    public static IEnumerable<(int Index, string Word)> Words(string text)
    {
        foreach (Match m in WordRx().Matches(text)) yield return (m.Index, m.Value);
    }

    /// <summary>Minúsculas, sem acentos e só letras/números ("Começa," → "comeca").</summary>
    public static string Normalize(string w)
    {
        var d = w.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Similaridade 0–1 pela distância de Levenshtein.</summary>
    public static double Similarity(string a, string b)
    {
        if (a == b) return 1;
        int m = a.Length, n = b.Length;
        if (m == 0 || n == 0) return 0;
        var prev = new int[n + 1]; var cur = new int[n + 1];
        for (var j = 0; j <= n; j++) prev[j] = j;
        for (var i = 1; i <= m; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= n; j++)
                cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return 1.0 - (double)prev[n] / Math.Max(m, n);
    }

    /// <summary>mm:ss (minutos podem passar de 59).</summary>
    public static string FormatMmSs(double sec)
    {
        if (!double.IsFinite(sec)) sec = 0;
        var s = (int)Math.Max(0, Math.Round(sec));
        return $"{s / 60:00}:{s % 60:00}";
    }

    /// <summary>Lê "mm:ss", "hh:mm:ss" ou "m" (minutos). Retorna null se inválido.</summary>
    public static int? ParseDuration(string s)
    {
        var parts = s.Trim().Split(':');
        if (parts.Length is < 1 or > 3) return null;
        var total = 0;
        foreach (var p in parts)
        {
            if (!int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out var v)) return null;
            total = total * 60 + v;
        }
        return parts.Length == 1 ? total * 60 : total;
    }
}
