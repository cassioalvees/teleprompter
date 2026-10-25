using System.Windows;
using System.Windows.Controls;

namespace Arca.App.Controls;

/// <summary>
/// Formato pílula (cantos = metade da altura), como o <c>border-radius: 980px</c> do protótipo. No WPF um raio
/// maior que a caixa não é limitado e o desenho vira uma elipse achatada — por isso o raio é calculado.
/// Uso: <c>c:Capsule.On="True"</c> num Border, ou <see cref="Apply"/> no código.
/// </summary>
public static class Capsule
{
    public static readonly DependencyProperty OnProperty =
        DependencyProperty.RegisterAttached("On", typeof(bool), typeof(Capsule), new PropertyMetadata(false, (d, e) => { if (d is Border b && (bool)e.NewValue) Apply(b); }));

    public static bool GetOn(DependencyObject d) => (bool)d.GetValue(OnProperty);
    public static void SetOn(DependencyObject d, bool v) => d.SetValue(OnProperty, v);

    public static void Apply(Border b)
    {
        void Fit() => b.CornerRadius = new CornerRadius(Math.Max(0, Math.Min(b.ActualHeight, b.ActualWidth) / 2));
        b.SizeChanged += (_, _) => Fit();
        Fit();
    }
}
