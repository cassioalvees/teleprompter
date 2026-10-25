using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Arca.App.Controls;

/// <summary>
/// Caixa de diálogo no visual do app (substitui o MessageBox do Windows): cartão com cantos arredondados,
/// rótulo, mensagem, detalhe e botões do tema. Enter = botão principal, Esc = cancelar. Modal.
/// </summary>
public sealed class ArcaDialog : Window
{
    private int _result = -1;
    private readonly int _cancel;

    private ArcaDialog(Window? owner, string eyebrow, string message, string? detail, string[] buttons, int primary, int cancel, UIElement? extra, bool danger)
    {
        _cancel = cancel;
        Owner = owner is { IsLoaded: true } ? owner : null;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = owner == null; Topmost = owner == null;
        SizeToContent = SizeToContent.Height; Width = 460;
        WindowStartupLocation = Owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
        Title = "ARCA Teleprompter";
        FontFamily = (FontFamily)Application.Current.FindResource("FSans"); FontSize = 13; FontWeight = FontWeights.Medium;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        UseLayoutRounding = true;

        var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };
        sp.Children.Add(new TextBlock { Text = eyebrow.ToUpperInvariant(), Style = (Style)Application.Current.FindResource("Eyebrow"), Foreground = Res(danger ? "Live" : "Accent"), Margin = new Thickness(0, 0, 0, 8) });
        sp.Children.Add(new TextBlock { Text = message, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Res("Text"), TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrWhiteSpace(detail))
            sp.Children.Add(new TextBlock { Text = detail, FontSize = 13, FontWeight = FontWeights.Normal, Foreground = Res("Muted"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), LineHeight = 19 });
        if (extra != null) { if (extra is FrameworkElement fe) fe.Margin = new Thickness(0, 14, 0, 0); sp.Children.Add(extra); }

        var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
        for (var i = 0; i < buttons.Length; i++)
        {
            var idx = i;
            var b = new Button
            {
                Content = buttons[i], MinWidth = 96, Height = 34, Margin = new Thickness(i == 0 ? 0 : 8, 0, 0, 0),
                Style = (Style)Application.Current.FindResource(i == primary ? "Primary" : "Pill"), Focusable = true,
                IsDefault = i == primary, IsCancel = i == cancel
            };
            if (i == primary && danger) b.Background = Res("Live");
            b.Click += (_, _) => { _result = idx; Close(); };
            bar.Children.Add(b);
        }
        sp.Children.Add(bar);

        var card = new Border
        {
            Background = Res("Ui"), BorderBrush = Res("Line"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14),
            Margin = new Thickness(18), Child = sp,
            Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 6, Opacity = 0.28, Color = Colors.Black }
        };
        card.MouseLeftButtonDown += (_, e) => { if (e.OriginalSource is not TextBox) try { DragMove(); } catch { } };
        Content = card;
        // Botões nunca cortados: se não couberem na largura padrão, a janela cresce (até 90% da tela).
        bar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var need = Math.Ceiling(bar.DesiredSize.Width + sp.Margin.Left + sp.Margin.Right + card.Margin.Left + card.Margin.Right + 2);
        if (need > Width) Width = Math.Min(need, SystemParameters.WorkArea.Width * 0.9);
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { _result = _cancel; Close(); e.Handled = true; } };
        Loaded += (_, _) => (extra as Control ?? (bar.Children[primary] as Control))?.Focus();
    }

    private static Brush Res(string key) => Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;

    /// <summary>Mostra o diálogo e devolve o índice do botão escolhido (Esc/fechar = <paramref name="cancel"/>).</summary>
    public static int Ask(Window? owner, string eyebrow, string message, string? detail, string[] buttons, int primary = 0, int cancel = -1, bool danger = false)
    {
        var d = new ArcaDialog(owner, eyebrow, message, detail, buttons, primary, cancel < 0 ? buttons.Length - 1 : cancel, null, danger);
        d.ShowDialog();
        return d._result < 0 ? d._cancel : d._result;
    }

    /// <summary>Como <see cref="Ask"/>, com um conteúdo extra entre o texto e os botões (campo, links…).</summary>
    public static int Ask(Window? owner, string eyebrow, string message, string? detail, string[] buttons, int primary, int cancel, UIElement extra, bool danger = false)
    {
        var d = new ArcaDialog(owner, eyebrow, message, detail, buttons, primary, cancel < 0 ? buttons.Length - 1 : cancel, extra, danger);
        d.ShowDialog();
        return d._result < 0 ? d._cancel : d._result;
    }

    /// <summary>Aviso com um botão "OK".</summary>
    public static void Info(Window? owner, string eyebrow, string message, string? detail = null, bool danger = false)
        => Ask(owner, eyebrow, message, detail, new[] { "OK" }, 0, 0, danger);

    /// <summary>
    /// Cria o diálogo sem mostrar, para conteúdo que muda sozinho (ex.: download). <paramref name="close"/> fecha com o
    /// resultado indicado; <see cref="ShowCreated"/> mostra e devolve o resultado.
    /// </summary>
    public static Window Create(Window? owner, string eyebrow, string message, string? detail, string[] buttons, int primary, int cancel, UIElement? extra, bool danger, out Action<int> close)
    {
        var d = new ArcaDialog(owner, eyebrow, message, detail, buttons, primary, cancel, extra, danger);
        close = r => { d._result = r; d.Close(); };
        return d;
    }

    public static int ShowCreated(Window w)
    {
        var d = (ArcaDialog)w; d.ShowDialog();
        return d._result < 0 ? d._cancel : d._result;
    }

    /// <summary>Escolhe uma cor no seletor do app. Devolve "#rrggbb" ou null se cancelado; guarda as últimas usadas.</summary>
    public static string? PickColor(Window? owner, string current, string message = "Escolher cor")
    {
        var app = MainWindow.Instance?.Vm.App;
        var panel = new ColorPickerPanel(current, app?.RecentColors ?? new List<string>());
        var d = new ArcaDialog(owner, "Cor", message, null, new[] { "Cancelar", "Aplicar" }, 1, 0, panel, false) { Width = 364 };
        d.ShowDialog();
        if (d._result != 1) return null;
        if (app != null)
        {
            app.RecentColors.RemoveAll(c => string.Equals(c, panel.Value, StringComparison.OrdinalIgnoreCase));
            app.RecentColors.Insert(0, panel.Value);
            if (app.RecentColors.Count > 10) app.RecentColors.RemoveRange(10, app.RecentColors.Count - 10);
        }
        return panel.Value;
    }

    /// <summary>Pede um texto. Devolve null se cancelado.</summary>
    public static string? Input(Window? owner, string eyebrow, string message, string value)
    {
        var tb = new TextBox { Style = (Style)Application.Current.FindResource("Input"), Text = value, Height = 34 };
        tb.Loaded += (_, _) => tb.SelectAll();
        var d = new ArcaDialog(owner, eyebrow, message, null, new[] { "Cancelar", "OK" }, 1, 0, tb, false);
        d.ShowDialog();
        return d._result == 1 ? tb.Text : null;
    }
}
