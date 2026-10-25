using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Arca.App.Controls;
using Arca.App.ViewModels;
using Arca.Core.Storage;
using Microsoft.Win32;

namespace Arca.App.Services;

/// <summary>
/// Janela de prompter num monitor (tela cheia, sem moldura) ou janela flutuante. Só desenha; as teclas
/// pressionadas aqui (clicker apontado para o monitor do talento) vão para a janela principal.
/// </summary>
public sealed class OutputWindow : Window
{
    public PrompterSurface Surface { get; } = new();
    public DisplayVM Display { get; }

    public OutputWindow(DisplayVM d)
    {
        Display = d;
        Title = "ARCA Teleprompter · " + d.Nick;
        Background = Brushes.Black;
        // Mesma barra de controle da janela do talento; aparece ao mexer o mouse e some sozinha.
        Bar = new PrompterControlBar { Visibility = Visibility.Collapsed };
        var root = new Grid(); root.Children.Add(Surface); root.Children.Add(Bar);
        Content = root;
        ShowActivated = false;
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/icon-64.png"));
        if (d.Windowed)
        {
            Width = 960; Height = 540; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        else
        {
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = false;
            Left = d.Bounds.X; Top = d.Bounds.Y; Width = Math.Max(320, d.Bounds.Width); Height = Math.Max(240, d.Bounds.Height);
            SourceInitialized += (_, _) => Native.PlaceWindow(this, d.Bounds);
            Loaded += (_, _) => { Native.PlaceWindow(this, d.Bounds); Cursor = Cursors.None; };
        }
        PreviewKeyDown += (_, e) => { if (e.Key == Key.F11 && d.Windowed) ToggleFull(); else MainWindow.Instance.HandleKey(e, fromOutput: true); };
        MouseDoubleClick += (_, _) => { if (d.Windowed && !Bar.IsMouseOver) ToggleFull(); };
        MouseMove += (_, e) =>
        {
            var p = e.GetPosition(this);
            if (Math.Abs(p.X - _last.X) + Math.Abs(p.Y - _last.Y) < 3) return;   // ignora o "movimento" ao abrir a janela
            _last = p; Cursor = null; Bar.Visibility = Visibility.Visible; _hide.Stop(); _hide.Start();
        };
        _hide.Tick += (_, _) =>
        {
            if (Bar.IsMouseOver) return;   // mantém enquanto o operador usa a barra
            _hide.Stop(); Cursor = Cursors.None; Bar.Visibility = Visibility.Collapsed;
        };
    }

    public PrompterControlBar Bar { get; }
    private Point _last = new(-100, -100);
    private readonly System.Windows.Threading.DispatcherTimer _hide = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _full;
    private void ToggleFull()
    {
        _full = !_full;
        WindowStyle = _full ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        WindowState = _full ? WindowState.Maximized : WindowState.Normal;
    }
}

/// <summary>
/// Lista os monitores (Win32 + nomes comerciais via DisplayConfig), guarda as preferências de cada tela
/// (apelido, espelho, alinhamento) e abre/fecha uma janela de prompter por tela marcada.
/// </summary>
public sealed class OutputManager
{
    private readonly MainWindow _main;
    private readonly Dictionary<string, OutputWindow> _windows = new();
    private readonly System.Windows.Threading.DispatcherTimer _blink = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool _blinkOn;
    private MainViewModel Vm => _main.Vm;

    public OutputManager(MainWindow main)
    {
        _main = main;
        _blink.Tick += (_, _) => { _blinkOn = !_blinkOn; foreach (var w in _windows.Values) w.Surface.Blink(w.Display.Find && _blinkOn); _main.PreviewBlink(Vm.Displays.Any(d => d.Find) && _blinkOn); };
        _blink.Start();
        SystemEvents.DisplaySettingsChanged += (_, _) => _main.Dispatcher.BeginInvoke(Refresh);
    }

    public IEnumerable<PrompterSurface> Surfaces => _windows.Values.Select(w => w.Surface);
    public int OpenCount => _windows.Count;
    public IEnumerable<OutputWindow> Windows => _windows.Values;

    /// <summary>Monitores atuais; preserva o estado das telas já conhecidas.</summary>
    public void Refresh()
    {
        var mons = Native.Monitors();
        var known = Vm.Displays.ToDictionary(d => d.Id);
        var next = new List<DisplayVM>();
        var n = 0;
        foreach (var m in mons.OrderBy(m => m.Primary ? 0 : 1).ThenBy(m => m.Bounds.X))
        {
            var id = m.Device;
            if (!m.Primary) n++;
            if (known.TryGetValue(id, out var cur)) { cur.Bounds = m.Bounds; next.Add(cur); continue; }
            var cfg = Vm.App.Displays.TryGetValue(id, out var c) ? c : null;
            var name = string.IsNullOrWhiteSpace(m.FriendlyName) ? id.Replace(@"\\.\", "") : m.FriendlyName;
            var d = new DisplayVM
            {
                Id = id, DeviceName = id, Operator = m.Primary, Bounds = m.Bounds,
                Nick = cfg?.Nick ?? (m.Primary ? "Tela do operador" : "Prompter " + n),
                Desc = $"{name} · {m.Bounds.Width} × {m.Bounds.Height}" + (m.Primary ? " · operador" : ""),
                // Telas externas vão para o vidro do prompter: espelhadas por padrão (configurável por tela).
                FlipH = cfg?.FlipH ?? !m.Primary, FlipV = cfg?.FlipV ?? false, Align = cfg?.Align ?? "left"
            };
            Hook(d); next.Add(d);
        }
        // Janelas flutuantes continuam; monitores desconectados saem (e fecham a janela).
        foreach (var d in Vm.Displays.Where(d => d.Windowed)) next.Add(d);
        foreach (var gone in Vm.Displays.Where(d => !d.Windowed && next.All(x => x.Id != d.Id)).ToList()) Close(gone);
        Vm.Displays.Clear();
        foreach (var d in next) Vm.Displays.Add(d);
        UpdateLabels();
    }

    /// <summary>"Nova janela de prompter": janela redimensionável, útil para testar ou para um 2º operador.</summary>
    public void AddWindow()
    {
        var n = Vm.Displays.Count(d => d.Windowed) + 1;
        var d = new DisplayVM { Id = "win" + DateTime.Now.Ticks, Windowed = true, Nick = "Janela flutuante " + n, Desc = "Janela · redimensionável", Align = Vm.Align };
        Hook(d); Vm.Displays.Add(d);
        d.On = true;
    }

    private void Hook(DisplayVM d) => d.PropertyChanged += (_, e) => OnDisplayChanged(d, e);

    private void OnDisplayChanged(DisplayVM d, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DisplayVM.On): if (d.On) Open(d); else Close(d); break;
            case nameof(DisplayVM.FlipH): case nameof(DisplayVM.FlipV):
                if (_windows.TryGetValue(d.Id, out var w)) { w.Surface.FlipH = d.FlipH; w.Surface.FlipV = d.FlipV; }
                break;
            case nameof(DisplayVM.Align): if (_windows.TryGetValue(d.Id, out var w2)) _main.Rebuild(w2.Surface, d.Align); break;
            case nameof(DisplayVM.Nick): if (_windows.TryGetValue(d.Id, out var w3)) w3.Title = "ARCA Teleprompter · " + d.Nick; break;
        }
        if (!d.Windowed) Vm.App.Displays[d.Id] = new DisplayConfig { Nick = d.Nick, FlipH = d.FlipH, FlipV = d.FlipV, Align = d.Align };
        _main.UpdatePreviewTarget();
        UpdateLabels();
    }

    private void Open(DisplayVM d)
    {
        if (_windows.ContainsKey(d.Id)) return;
        // Demonstração: uma tela de prompter (janela do talento ou uma janela/monitor).
        if (_main.IsDemo && (_windows.Count > 0 || _main.TalentOpen) && !_main.Allow("telas")) { d.On = false; return; }
        if (d.Operator && !d.Windowed && ArcaDialog.Ask(_main, "Tela do operador", "Abrir o prompter em tela cheia neste monitor?", "Ele vai cobrir o editor. Para ler neste monitor sem perder o editor, use a Janela do talento (F5).", new[] { "Cancelar", "Abrir em tela cheia" }, primary: 1, cancel: 0) != 1)
        { d.On = false; return; }
        var w = new OutputWindow(d);
        w.Surface.FlipH = d.FlipH; w.Surface.FlipV = d.FlipV;
        w.Surface.ShowHud = Vm.TalentHud;
        var a = Vm.App; w.Surface.SetHudOptions(a.HudBlock, a.HudTime, a.HudClock, a.HudBottom);
        w.Closed += (_, _) => { _windows.Remove(d.Id); d.On = false; UpdateLabels(); };
        _windows[d.Id] = w;
        _main.Rebuild(w.Surface, d.Align);
        w.Show();
        _main.Activate();
        Arca.Core.Storage.Log.Info($"Saída aberta: {d.Nick} ({d.Desc})");
    }

    private void Close(DisplayVM d)
    {
        if (_windows.Remove(d.Id, out var w)) w.Close();
        if (d.Windowed && !d.On) Vm.Displays.Remove(d);
    }

    public void CloseAll() { foreach (var w in _windows.Values.ToList()) w.Close(); _windows.Clear(); }

    public void SetHud(bool on) { foreach (var w in _windows.Values) w.Surface.ShowHud = on; }
    public void SetHudOptions(bool block, bool time, bool clock, bool bottom) { foreach (var w in _windows.Values) w.Surface.SetHudOptions(block, time, clock, bottom); }

    public void RebuildAll() { foreach (var w in _windows.Values) _main.Rebuild(w.Surface, w.Display.Align); }

    private void UpdateLabels() => Vm.ScreensLabel = $"{_windows.Count} tela(s) de prompter";

    /// <summary>Tela usada na prévia do inspetor: a primeira saída aberta fora do operador.</summary>
    public DisplayVM? PreviewTarget => Vm.Displays.FirstOrDefault(d => d.On && !d.Operator) ?? Vm.Displays.FirstOrDefault(d => d.On);
}
