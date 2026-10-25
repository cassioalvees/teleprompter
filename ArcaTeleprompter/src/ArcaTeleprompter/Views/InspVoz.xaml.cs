using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Arca.App.ViewModels;
using Arca.Audio;
using Microsoft.Win32;

namespace Arca.App.Views;

/// <summary>Inspetor › Voz: acompanhamento por voz (Vosk ou Whisper, offline) com entrada WASAPI ou ASIO (canal).</summary>
public partial class InspVoz : UserControl
{
    private static MainWindow Main => MainWindow.Instance;
    private MainViewModel Vm => (MainViewModel)DataContext;
    private List<AudioDeviceInfo> _devices = new();
    private bool _loading;

    public InspVoz()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm) return;
            Dispatcher.BeginInvoke(() => Sens_Changed(this, null!), System.Windows.Threading.DispatcherPriority.Loaded);
            vm.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(MainViewModel.LevelDb): Meter_Update(); break;
                    case nameof(MainViewModel.VoiceMode): case nameof(MainViewModel.VoiceCode): Buttons(); break;
                    case nameof(MainViewModel.VoiceError): ErrText.Visibility = string.IsNullOrEmpty(vm.VoiceError) ? Visibility.Collapsed : Visibility.Visible; break;
                    case nameof(MainViewModel.ModelMissing): ModelBtn.Visibility = vm.ModelMissing ? Visibility.Visible : Visibility.Collapsed; break;
                }
            };
            vm.Heard.CollectionChanged += (_, _) => HeardEmpty.Visibility = vm.Heard.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ErrText.Visibility = Visibility.Collapsed;
            ModelBtn.Visibility = vm.ModelMissing ? Visibility.Visible : Visibility.Collapsed;
            Buttons();
            EngineUi();
            LoadDevices();
        };
        Meter.SizeChanged += (_, _) => Meter_Update();
    }

    private void Buttons()
    {
        var mic = Vm.VoiceMode == "mic" || Vm.VoiceMode == "file"; var sim = Vm.VoiceMode == "sim";
        MicBtn.Style = (Style)FindResource(mic ? "Primary" : "Primary");
        MicBtn.Background = mic ? (Brush)FindResource("Live") : (Brush)FindResource("Accent");
        MicBtn.Foreground = mic ? Brushes.White : (Brush)FindResource("AccentInk");
        MicBtn.Content = mic ? "Parar" : "Usar microfone";
        SimBtn.Style = (Style)FindResource("Pill");
        SimBtn.Content = sim ? "Parar simulação" : "Simular leitura";
        var dot = Vm.VoiceCode switch { "IMPROVISO" => "Live", "DESLIGADO" => "Faint", _ => "Text" };
        StateDot.Fill = (Brush)FindResource(dot);
    }

    private void Meter_Update()
    {
        var w = Meter.ActualWidth; if (w <= 0) return;
        var lv = Math.Clamp((Vm.LevelDb + 70) / 70, 0, 1);
        LevelBar.Width = lv * w;
        LevelBar.Background = (Brush)FindResource(Vm.LevelDb > Vm.App.NoiseGateDb ? "Text" : "Faint");
        GateMark.Margin = new Thickness(Math.Clamp((Vm.App.NoiseGateDb + 70) / 70, 0, 1) * w, -3, 0, -3);
    }

    /// <summary>Lista os dispositivos do driver escolhido (WASAPI: microfones do Windows; ASIO: drivers das interfaces).</summary>
    public void LoadDevices()
    {
        _loading = true;
        var asio = Vm.App.AudioDriver == "asio";
        _devices = asio ? AudioInput.ListAsio() : AudioInput.ListWasapi();
        DeviceBox.Items.Clear();
        if (!asio) DeviceBox.Items.Add(new ComboBoxItem { Content = "Microfone padrão do Windows", Tag = "" });
        foreach (var d in _devices) DeviceBox.Items.Add(new ComboBoxItem { Content = asio && d.Channels == 0 ? d.Name + " (sem entradas)" : d.Name, Tag = d.Id });
        if (DeviceBox.Items.Count == 0) DeviceBox.Items.Add(new ComboBoxItem { Content = "Nenhum driver ASIO instalado", IsEnabled = false });
        DeviceBox.SelectedItem = DeviceBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string?)i.Tag == Vm.App.AudioDevice) ?? DeviceBox.Items[0];
        ChLabel.Visibility = ChannelBox.Visibility = asio ? Visibility.Visible : Visibility.Collapsed;
        LoadChannels();
        _loading = false;
    }

    private void LoadChannels()
    {
        ChannelBox.Items.Clear();
        var d = _devices.FirstOrDefault(x => x.Id == Vm.App.AudioDevice);
        var n = d?.Channels > 0 ? d.Channels : 32;
        for (var i = 0; i < n; i++) ChannelBox.Items.Add(new ComboBoxItem { Content = $"Canal {i + 1}", Tag = i });
        ChannelBox.SelectedIndex = Math.Clamp(Vm.App.AsioChannel, 0, n - 1);
    }

    private void Driver_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string drv } || DataContext is not MainViewModel vm || vm.App.AudioDriver == drv && _devices.Count > 0) return;
        vm.App.AudioDriver = drv; vm.App.AudioDevice = "";
        LoadDevices();
        Main.Voice.RestartIfRunning();
    }
    private void Device_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || DeviceBox.SelectedItem is not ComboBoxItem { Tag: string id }) return;
        Vm.App.AudioDevice = id; _loading = true; LoadChannels(); _loading = false;
        Main.Voice.RestartIfRunning();
    }
    private void Channel_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ChannelBox.SelectedItem is not ComboBoxItem { Tag: int ch }) return;
        Vm.App.AsioChannel = ch; Main.Voice.RestartIfRunning();
    }
    private void Gain_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { if (DataContext is MainViewModel) Main.Voice.SetInputGain(e.NewValue); }
    private void Gain_Reset(object sender, System.Windows.Input.MouseButtonEventArgs e) => GainSlider.Value = 0;
    private void Agc_Changed(object sender, RoutedEventArgs e) { if (DataContext is MainViewModel vm) Main.Voice.SetAutoGain(vm.App.VoiceAgc); }
    private void Gate_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { if (DataContext is MainViewModel) { Main.Voice.SetGate(e.NewValue); Meter_Update(); } }
    private void Sens_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is not MainViewModel vm) return;
        WinLabel.Text = vm.App.VoiceWindow.ToString(); TolLabel.Text = vm.App.VoiceTolerance.ToString();
        if (LeadLabel != null) LeadLabel.Text = vm.App.VoiceLead.ToString("0.00");
        if (SmoothLabel != null) SmoothLabel.Text = vm.App.VoiceSmooth.ToString("0.00") + " s";
        if (StrictLabel != null) StrictLabel.Text = (vm.App.VoiceStrict * 100).ToString("0") + "%";
        Main.Voice.ApplySensitivity();
    }
    private void Lang_Changed(object sender, SelectionChangedEventArgs e) { if (DataContext is MainViewModel) Main.Voice.CheckModel(); }
    private void Engine_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string eng } || DataContext is not MainViewModel vm) return;
        var changed = vm.App.VoiceEngine != eng;
        vm.App.VoiceEngine = eng;
        EngineUi();
        Main.Voice.CheckModel();
        if (changed) Main.Voice.RestartIfRunning();
    }
    private void WModel_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel || e.RemovedItems.Count == 0) return;
        Main.Voice.CheckModel(); Main.Voice.RestartIfRunning();
    }
    private void Gpu_Changed(object sender, RoutedEventArgs e) { if (DataContext is MainViewModel && IsLoaded) Main.Voice.RestartIfRunning(); }
    /// <summary>Opções do Whisper (modelo, GPU) só aparecem com ele escolhido.</summary>
    private void EngineUi()
    {
        var w = DataContext is MainViewModel vm && vm.App.VoiceEngine == "whisper";
        WModelLabel.Visibility = WModelBox.Visibility = GpuBox.Visibility = w ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Mic_Click(object sender, RoutedEventArgs e) => Main.Voice.ToggleMic();
    private void Sim_Click(object sender, RoutedEventArgs e) => Main.Voice.ToggleSim();
    private void Diag_Click(object sender, RoutedEventArgs e) => Main.Voice.RecordDiagnostic();
    private void Model_Click(object sender, RoutedEventArgs e) => _ = Main.Voice.DownloadModel();
    private void File_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Áudio para testar o acompanhamento", Filter = "Áudio|*.wav;*.mp3;*.m4a;*.aiff|Todos os arquivos|*.*" };
        if (d.ShowDialog(Main) == true) Main.Voice.StartFile(d.FileName);
    }
}
