using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Arca.App.ViewModels;
using Arca.Ndi;

namespace Arca.App.Views;

/// <summary>Inspetor › Telas: prévia da saída, monitores, NDI e monitor web / iPad.</summary>
public partial class InspTelas : UserControl
{
    private static MainWindow Main => MainWindow.Instance;
    public InspTelas()
    {
        InitializeComponent();
        NdiFormat.ItemsSource = Arca.Ndi.NdiFormat.All;
        Loaded += (_, _) => UpdateWebWarn();
        Loaded += (_, _) =>
        {
            if (_hudInit) return;
            (Main.Vm.App.HudBottom ? HudBottomBtn : HudTopBtn).IsChecked = true;
            _hudInit = true;
        };
    }

    /// <summary>Área onde a janela principal coloca a superfície de prévia.</summary>
    public Grid Host => PreviewHost;

    private void Preview_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) Main.ToggleTalent(); }
    private void Align_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: DisplayVM d }) d.Align = d.Align == "center" ? "left" : "center"; }
    private void NewWindow_Click(object sender, RoutedEventArgs e) => Main.Outputs.AddWindow();

    private bool _hudInit;
    private void Hud_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) Main.ApplyHudOptions(); }
    private void HudPos_Checked(object sender, RoutedEventArgs e)
    {
        if (!_hudInit) return;
        Main.Vm.App.HudBottom = sender == HudBottomBtn;
        Main.ApplyHudOptions();
    }
    private void NdiOption_Click(object sender, RoutedEventArgs e) => Main.Ndi.UpdateOptions();
    private void WebOption_Click(object sender, RoutedEventArgs e) { Main.Web.UpdateOptions(); UpdateWebWarn(); }
    private void WebPass_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateWebWarn();
    /// <summary>Aviso em vermelho enquanto o controle pelo navegador estiver liberado sem senha.</summary>
    private void UpdateWebWarn()
    {
        if (WebOpenWarn == null || WebPassBox == null || WebCtrlBox == null) return;
        WebOpenWarn.Visibility = WebCtrlBox.IsChecked == true && WebPassBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void OpenMonitor_Click(object sender, RoutedEventArgs e) => Main.ShowMonitorDialog();
    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = ((MainViewModel)DataContext).WebUrl;
        if (string.IsNullOrEmpty(url) || !url.StartsWith("http")) { Main.Flash("Ligue o monitor web primeiro"); return; }
        try { Clipboard.SetText(url); CopyBtn.Content = "Copiado"; } catch { /* área de transferência ocupada */ }
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        t.Tick += (_, _) => { CopyBtn.Content = "Copiar"; t.Stop(); }; t.Start();
    }
}
