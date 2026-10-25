using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Arca.App.ViewModels;

namespace Arca.App.Views;

/// <summary>Inspetor › Vídeo: câmera e gravação (MP4) junto com o prompt.</summary>
public partial class InspVideo : UserControl
{
    private static MainWindow Main => MainWindow.Instance;
    public InspVideo()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm) return;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(MainViewModel.CamOn) or nameof(MainViewModel.Recording) or nameof(MainViewModel.LastRecording)) Refresh(vm);
            };
            Refresh(vm);
        };
        IsVisibleChanged += async (_, _) =>
        {
            if (!IsVisible || CamBox.Items.Count > 0) return;
            foreach (var (id, name) in await Main.Video.ListCameras()) CamBox.Items.Add(new ComboBoxItem { Content = name, Tag = id });
            if (CamBox.Items.Count == 0) CamBox.Items.Add(new ComboBoxItem { Content = "Nenhuma câmera encontrada", IsEnabled = false });
            CamBox.SelectedIndex = 0;
        };
    }

    public Image PreviewImage => Preview;

    private void Refresh(MainViewModel vm)
    {
        CamBtn.Content = vm.CamOn ? "Desligar câmera" : "Ligar câmera";
        RecBtn.Content = vm.Recording ? "Parar gravação" : "Gravar";
        RecBtn.Background = vm.Recording ? (Brush)FindResource("Live") : (Brush)FindResource("Accent");
        RecBtn.Foreground = vm.Recording ? Brushes.White : (Brush)FindResource("AccentInk");
        OpenRec.Visibility = vm.LastRecording != null ? Visibility.Visible : Visibility.Collapsed;
        OpenRec.Content = vm.LastRecording != null ? "Abrir gravação · " + System.IO.Path.GetFileName(vm.LastRecording) : "";
    }

    private void Cam_Click(object sender, RoutedEventArgs e) => _ = Main.Video.ToggleCamera((CamBox.SelectedItem as ComboBoxItem)?.Tag as string);
    private void Rec_Click(object sender, RoutedEventArgs e) => _ = Main.Video.ToggleRecording();
    private void OpenRec_Click(object sender, RoutedEventArgs e)
    {
        var f = ((MainViewModel)DataContext).LastRecording;
        if (f != null && System.IO.File.Exists(f)) Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{f}\"") { UseShellExecute = true });
    }
}
