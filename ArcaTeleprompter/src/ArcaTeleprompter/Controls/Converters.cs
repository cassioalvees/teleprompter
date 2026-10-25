using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Arca.App.Editor;

namespace Arca.App.Controls;

/// <summary>bool → Visible/Collapsed (parâmetro "not" inverte).</summary>
public sealed class BoolVis : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => (v is true) ^ (p as string == "not") ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>bool invertido.</summary>
public sealed class Not : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is not true;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is not true;
}

/// <summary>Valor == parâmetro (para RadioButton ligados a uma string/int). Volta o parâmetro quando marcado.</summary>
public sealed class Is : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => string.Equals(v?.ToString(), p?.ToString(), StringComparison.Ordinal);
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
    {
        if (v is not true) return Binding.DoNothing;
        if (t == typeof(int) && int.TryParse(p?.ToString(), out var i)) return i;
        return p!;
    }
}

/// <summary>Valor == parâmetro → Visible.</summary>
public sealed class IsVis : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => string.Equals(v?.ToString(), p?.ToString(), StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>"#rrggbb" → pincel.</summary>
public sealed class HexBrush : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => ScriptFlow.ParseBrush(v as string, Brushes.Transparent);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Nome de recurso de cor ("Ok", "Faint", "Accent"…) → pincel do tema.</summary>
public sealed class ResBrush : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => Application.Current.TryFindResource(v as string ?? "Faint") as Brush ?? Brushes.Gray;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Formata número com o formato do parâmetro (ex.: "0.00", "P0").</summary>
public sealed class Fmt : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is IFormattable f ? f.ToString(p as string ?? "0", CultureInfo.CurrentCulture) : v?.ToString() ?? "";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Nível em dBFS (−70…0) → largura relativa 0…1 multiplicada pelo parâmetro (px).</summary>
public sealed class DbWidth : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        var db = v is double d ? d : -70; var w = double.TryParse(p as string, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 100;
        return Math.Clamp((db + 70) / 70, 0, 1) * w;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
