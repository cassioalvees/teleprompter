using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Arca.Core.Engine;

namespace Arca.App.Editor;

/// <summary>Uma opção do seletor de fonte.</summary>
public sealed record FontOption(string Key, string Name, bool System)
{
    public override string ToString() => Name;
}

/// <summary>
/// Fontes do roteiro: as quatro empacotadas no app (Atkinson Hyperlegible, Source Sans 3, Source Serif 4,
/// IBM Plex Mono) e todas as fontes instaladas no Windows, listadas automaticamente ("sys:Família").
/// </summary>
public static class FontCatalog
{
    private static readonly Dictionary<string, double> FactorCache = new();
    public static readonly FontFamily Atkinson = new(new Uri("pack://application:,,,/"), "./Fonts/#Atkinson Hyperlegible");

    public static readonly FontOption[] Bundled =
    {
        new("atk", "Atkinson Hyperlegible", false),
        new("sans", "Source Sans 3", false),
        new("serif", "Source Serif 4", false),
        new("mono", "IBM Plex Mono", false)
    };

    private static List<FontOption>? _system;
    /// <summary>Fontes instaladas, em ordem alfabética (nome no idioma do Windows).</summary>
    public static IReadOnlyList<FontOption> SystemFonts => _system ??= LoadSystem();

    private static List<FontOption> LoadSystem()
    {
        var lang = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
        var names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var f in Fonts.SystemFontFamilies)
        {
            var n = f.FamilyNames.TryGetValue(lang, out var local) ? local : f.FamilyNames.Values.FirstOrDefault() ?? f.Source;
            if (!string.IsNullOrWhiteSpace(n) && !n.StartsWith('@')) names.Add(n);
        }
        return names.Select(n => new FontOption("sys:" + n, n, true)).ToList();
    }

    public static IEnumerable<FontOption> All => Bundled.Concat(SystemFonts);

    public static FontFamily Family(string key) => key switch
    {
        "sans" => new(new Uri("pack://application:,,,/"), "./Fonts/#Source Sans 3"),
        "serif" => new(new Uri("pack://application:,,,/"), "./Fonts/#Source Serif 4"),
        "mono" => new(new Uri("pack://application:,,,/"), "./Fonts/#IBM Plex Mono"),
        _ when key.StartsWith("sys:") => new(key[4..]),
        _ => Atkinson
    };

    /// <summary>Nome da família para o navegador (monitor web / iPad).</summary>
    public static string WebName(string key) => key switch
    {
        "sans" => "Source Sans 3", "serif" => "Source Serif 4", "mono" => "IBM Plex Mono",
        _ when key.StartsWith("sys:") => key[4..], _ => "Atkinson Hyperlegible"
    };

    /// <summary>
    /// Fator de largura para manter as palavras por linha: fontes do sistema são medidas em relação à
    /// Atkinson (fator 1); as empacotadas usam os fatores do design.
    /// </summary>
    public static double Factor(string key)
    {
        if (!key.StartsWith("sys:")) return PrompterLayout.BundledFontFactor(key);
        if (FactorCache.TryGetValue(key, out var f)) return f;
        const string sample = "Boa noite. Começa agora o Jornal da Noite, com as notícias do dia.";
        double W(FontFamily ff) => new FormattedText(sample, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(ff, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 100, Brushes.Black, 1).WidthIncludingTrailingWhitespace;
        var baseW = W(Atkinson); var w = W(Family(key));
        f = baseW > 0 && w > 0 ? Math.Clamp(w / baseW, 0.6, 1.6) : 1;
        FactorCache[key] = f;
        return f;
    }

    public static string Label(string key) => All.FirstOrDefault(o => o.Key == key)?.Name ?? key.Replace("sys:", "");
}
