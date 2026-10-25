using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.App.Services;
using Arca.App.ViewModels;

namespace Arca.App.Views;

/// <summary>Inspetor › Roteiro: aparência, apresentadores, palavras por linha, velocidade, duração automática.</summary>
public partial class InspRoteiro : UserControl
{
    private MainViewModel Vm => (MainViewModel)DataContext;
    private static MainWindow Main => MainWindow.Instance;

    public static readonly string[] BgPresets = { "#000000", "#1c1c1c", "#f4f1ea", "#ffffff" };
    public static readonly string[] FgPresets = { "#ffffff", "#ffe14a", "#d9d9d9", "#111111" };

    public InspRoteiro()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm) return;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(MainViewModel.Background) or nameof(MainViewModel.Foreground)) BuildSwatches();
                if (e.PropertyName is nameof(MainViewModel.PresenterOnly)) BuildChips();
                if (e.PropertyName is nameof(MainViewModel.FontKey)) SelectFont();
            };
            vm.Presenters.CollectionChanged += (_, _) => BuildChips();
            foreach (var p in vm.Presenters) p.PropertyChanged += (_, _) => BuildChips();
            BuildSwatches(); BuildFonts(); BuildChips();
        };
    }

    // ——— Cores ———
    private void BuildSwatches()
    {
        Swatches(BgSwatches, BgPresets, Vm.Background, v =>
        {
            // Fundo claro com texto branco (ou escuro com texto quase preto): troca o texto junto, como no protótipo.
            var light = v is "#f4f1ea" or "#ffffff";
            if (light && Vm.Foreground == "#ffffff") Vm.Foreground = "#111111";
            else if (!light && Vm.Foreground == "#111111") Vm.Foreground = "#ffffff";
            Vm.Background = v;
        });
        Swatches(FgSwatches, FgPresets, Vm.Foreground, v => Vm.Foreground = v);
    }

    /// <summary>Quatro predefinições + "Outra cor…" (seletor do Windows) + campo HEX.</summary>
    public static void Swatches(Panel host, string[] presets, string current, Action<string> set)
    {
        host.Children.Clear();
        foreach (var v in presets)
        {
            var b = new Button { Style = (Style)Application.Current.FindResource("Swatch"), Background = ScriptFlow.ParseBrush(v, Brushes.Black), Tag = string.Equals(v, current, StringComparison.OrdinalIgnoreCase), ToolTip = v.ToUpperInvariant(), Focusable = false };
            b.Click += (_, _) => set(v);
            host.Children.Add(b);
        }
        var custom = !presets.Contains(current, StringComparer.OrdinalIgnoreCase);
        var pick = new Button
        {
            Style = (Style)Application.Current.FindResource("Swatch"), Background = Brushes.Transparent, Tag = custom, ToolTip = "Outra cor…", Focusable = false,
            Content = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), BorderBrush = (Brush)Application.Current.FindResource("Line"), Child = new Icon { Data = Icons.Plus, Size = 11, StrokeWidth = 1.6, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)Application.Current.FindResource("Muted") } }
        };
        pick.Click += (_, _) => { var c = ArcaDialog.PickColor(Main, current); if (c != null) set(c); };
        host.Children.Add(pick);
        var hex = new TextBox { Style = (Style)Application.Current.FindResource("FieldSmall"), Width = 90, Text = current.ToUpperInvariant(), MaxLength = 7, CharacterCasing = CharacterCasing.Upper, Margin = new Thickness(0, 0, 0, 0) };
        void Commit()
        {
            var v = hex.Text.Trim(); if (!v.StartsWith('#')) v = "#" + v;
            if (System.Text.RegularExpressions.Regex.IsMatch(v, "^#[0-9a-fA-F]{3}$")) v = "#" + string.Concat(v[1..].Select(ch => $"{ch}{ch}"));
            if (System.Text.RegularExpressions.Regex.IsMatch(v, "^#[0-9a-fA-F]{6}$")) set(v.ToLowerInvariant()); else hex.Text = current.ToUpperInvariant();
        }
        hex.LostFocus += (_, _) => Commit();
        hex.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Commit(); Keyboard.ClearFocus(); } };
        host.Children.Add(hex);
    }

    // ——— Fontes: as empacotadas e todas as do Windows, automaticamente ———
    private bool _fontLoading;
    private void BuildFonts()
    {
        _fontLoading = true;
        FontBox.Items.Clear();
        foreach (var f in FontCatalog.Bundled) FontBox.Items.Add(new ComboBoxItem { Content = f.Name, Tag = f.Key, FontFamily = FontCatalog.Family(f.Key) });
        FontBox.Items.Add(new ComboBoxItem { Content = "FONTES DO COMPUTADOR", IsEnabled = false, FontFamily = (FontFamily)Application.Current.FindResource("FNum"), FontSize = 10 });
        foreach (var f in FontCatalog.SystemFonts) FontBox.Items.Add(new ComboBoxItem { Content = f.Name, Tag = f.Key, FontFamily = new FontFamily(f.Name) });
        _fontLoading = false;
        SelectFont();
    }
    private void SelectFont()
    {
        _fontLoading = true;
        FontBox.SelectedItem = FontBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string?)i.Tag == Vm.FontKey) ?? FontBox.Items[0];
        _fontLoading = false;
    }
    private void Font_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_fontLoading || FontBox.SelectedItem is not ComboBoxItem { Tag: string key }) return;
        Vm.FontKey = key;
    }

    // ——— Apresentadores ———
    private void BuildChips()
    {
        if (DataContext is not MainViewModel vm) return;
        OnlyChips.Children.Clear();
        void Chip(string n, string name, string? color)
        {
            var on = vm.PresenterOnly == n;
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (color != null) sp.Children.Add(new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = ScriptFlow.ParseBrush(color, Brushes.Gray), Margin = new Thickness(0, 0, 6, 0) });
            sp.Children.Add(new TextBlock { Text = name });
            var b = new Button
            {
                Style = (Style)Application.Current.FindResource(on ? "Primary" : "Pill"), Height = 26, FontSize = 12, Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 4, 4), Content = sp, Focusable = false
            };
            b.Click += (_, _) => vm.PresenterOnly = n;
            OnlyChips.Children.Add(b);
        }
        Chip("0", "Todas", null);
        for (var i = 0; i < vm.Presenters.Count; i++) Chip((i + 1).ToString(), string.IsNullOrWhiteSpace(vm.Presenters[i].Name) ? "Apresentador " + (i + 1) : vm.Presenters[i].Name, vm.Presenters[i].Color);
    }

    private void AddPres_Click(object sender, RoutedEventArgs e) => Main.AddPresenter();
    private void RemovePres_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: PresenterVM p }) Main.RemovePresenter(p); }
    private void PresColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PresenterVM p }) return;
        var c = ArcaDialog.PickColor(Main, p.Color, "Cor do apresentador"); if (c != null) p.Color = c;
    }

    private void ResetLook_Click(object sender, MouseButtonEventArgs e) { Vm.Background = "#000000"; Vm.Foreground = "#ffffff"; Vm.FontKey = "atk"; Vm.Align = "left"; Vm.EdFlipH = false; Vm.EdFlipV = false; }
    private void ResetLayout_Click(object sender, MouseButtonEventArgs e) { Vm.WordsPerLine = 5; Vm.Margin = 0; Vm.MarginPos = -1; Vm.LineHeight = 1.3; }
    private void ResetMPos_Click(object sender, MouseButtonEventArgs e) => Vm.MarginPos = -1;
}
