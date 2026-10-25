using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.App.Services;
using Arca.App.ViewModels;
using Arca.App.Views;
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Storage;

namespace Arca.App;

/// <summary>
/// Janela principal: editor + saídas. Este arquivo cuida da inicialização, do laço de rolagem (um quadro
/// por atualização do monitor), das superfícies de prompter, das leituras e do rundown. Ver também os
/// arquivos parciais MainWindow.Editor/Keys/Dialogs/Integrations.
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainViewModel Vm { get; }
    public ScrollEngine Engine { get; }
    public OutputManager Outputs { get; }
    public WebService Web { get; }
    public NdiService Ndi { get; }
    public VoiceService Voice { get; }
    public UpdateService Updates { get; }
    public VideoService Video { get; }
    public CaptionService Caption { get; }
    public MosService Mos { get; }

    /// <summary>Documento atual (modelo). Atualizado a partir do editor a cada pausa na digitação.</summary>
    public ScriptDocument Doc { get; private set; } = new();
    private PrompterLayout _layout;
    private FontFamily _font = FontCatalog.Atkinson;

    private readonly PrompterSurface _measure = new() { ShowFlashAndCount = false };
    private readonly PrompterSurface _preview = new() { ShowFlashAndCount = false };
    private readonly InspRoteiro _inspRoteiro = new();
    private readonly InspOverlay _inspOverlay = new();
    private readonly InspTelas _inspTelas = new();
    private readonly InspVoz _inspVoz = new();
    private readonly InspVideo _inspVideo = new();
    public Image VideoPreview => _inspVideo.PreviewImage;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastFrame, _tick, _lastY = -1;
    private readonly DispatcherTimer _flashT = new() { Interval = TimeSpan.FromMilliseconds(1100) };
    private readonly DispatcherTimer _countT = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _rehT = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Dictionary<int, double> _rehearsal = new();
    private List<BlockBox> _bmBoxes = new();
    private int _cur = -1;

    /// <summary>Grava a janela (e a do talento, se aberta) em PNG — suporte e testes visuais.</summary>
    public void SaveSnapshot(string path)
    {
        try
        {
            // Diálogos abertos (janelas filhas) também: arquivo-1.png, arquivo-2.png…
            var n = 0;
            foreach (Window ow in OwnedWindows) if (ow.Content is FrameworkElement oc && oc.ActualWidth > 0) Render(oc, ow, Path.ChangeExtension(path, null) + $"-{++n}.png");
            if (_talentWin?.Content is FrameworkElement tc && tc.ActualWidth > 0) Render(tc, _talentWin, Path.ChangeExtension(path, null) + "-talento.png");
            var t = 0;
            foreach (var w in Outputs.Windows) if (w.Content is FrameworkElement wc && wc.ActualWidth > 0) Render(wc, w, Path.ChangeExtension(path, null) + $"-tela-{++t}.png");
            if (Content is not FrameworkElement root || root.ActualWidth < 1) return;
            Render(root, this, path);
        }
        catch (Exception e) { Log.Error("Captura da janela", e); }
    }

    private static void Render(FrameworkElement root, Window w, string path)
    {
        try
        {
            var dpi = VisualTreeHelper.GetDpi(w);
            var rtb = new RenderTargetBitmap((int)(root.ActualWidth * dpi.DpiScaleX), (int)(root.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen()) dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, root.ActualWidth, root.ActualHeight));
            rtb.Render(dv);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Create(path); enc.Save(fs);
        }
        catch (Exception e) { Log.Error("Captura da janela", e); }
    }

    /// <summary>Janela normal (não maximizada) ocupando ~90% da área útil, centralizada.</summary>
    private void FitToWorkArea()
    {
        var wa = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, wa.Width); MinHeight = Math.Min(MinHeight, wa.Height);
        Width = Math.Clamp(wa.Width * 0.9, MinWidth, 1600); Height = Math.Clamp(wa.Height * 0.9, MinHeight, 1000);
        Left = wa.Left + (wa.Width - Width) / 2; Top = wa.Top + (wa.Height - Height) / 2;
    }

    public MainWindow(AppSettings settings)
    {
        Instance = this;
        Vm = new MainViewModel(settings);
        Engine = new ScrollEngine(settings.Prompter);
        _layout = PrompterLayout.Compute(settings.Prompter, 1);
        InitializeComponent();
        InitLicense();
        FitToWorkArea();
        InitCaret();
        DataContext = Vm;
        Outputs = new OutputManager(this);
        Web = new WebService(this);
        Ndi = new NdiService(this);
        Voice = new VoiceService(this);
        Updates = new UpdateService(this);
        Video = new VideoService(this);
        Caption = new CaptionService(this);
        Mos = new MosService(this);
        ApplyHudOptions();

        InspRoteiro.Content = _inspRoteiro; InspOverlay.Content = _inspOverlay; InspTelas.Content = _inspTelas; InspVoz.Content = _inspVoz; InspVideo.Content = _inspVideo;
        _inspTelas.Host.Children.Add(_preview);
        MeasureHost.Children.Add(_measure);
        _measure.Width = PrompterLayout.Width; _measure.Height = 10;
        _measure.LayoutChanged += () => Dispatcher.BeginInvoke(OnMeasureLayout, DispatcherPriority.Loaded);
        TalentSurface.LayoutChanged += () => { };

        Vm.Theme = settings.Theme; ApplyTheme(settings.Theme);
        Vm.TalentHud = settings.TalentHud; Vm.ShowBookmarks = settings.ShowBookmarks; Vm.ShowInspector = settings.ShowInspector; Vm.SpeedUnit = settings.SpeedUnit;
        Vm.LookChanged += layout => { if (layout) _lookT.Stop(); _lookT.Start(); _lookLayout |= layout; };
        Vm.PropertyChanged += Vm_PropertyChanged;
        _lookT.Tick += (_, _) => { _lookT.Stop(); ApplyLook(_lookLayout); _lookLayout = false; };

        Engine.PlayStateChanged += OnPlayState;
        Engine.SpeedChanged += (t, dir) => { Vm.SpeedT = t; Vm.Reverse = dir < 0; FlashSpeed(); };
        Engine.StopReached += _ => Flash("Pausa");
        _flashT.Tick += (_, _) => { _flashT.Stop(); ShowFlash(null, null, -1); };
        _countT.Tick += CountTick;
        _rehT.Tick += (_, _) => { if (Vm.Rehearse && Engine.Playing && _cur >= 0) { _rehearsal[_cur] = _rehearsal.GetValueOrDefault(_cur) + 0.5; Vm.HasRehearsal = true; } };
        _rehT.Start();

        BuildToolbarExtras();
        InitEditor();
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += (_, e) => HandleKey(e, false);
        // Depois do passe de layout: só então a área de rolagem tem a largura nova.
        EdHost.SizeChanged += (_, _) => { LayoutEditor(); Dispatcher.BeginInvoke(LayoutEditor, DispatcherPriority.Loaded); };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(LayoutEditor, DispatcherPriority.Loaded);
        StateChanged += (_, _) => { if (WindowState == WindowState.Maximized) Root.Margin = new Thickness(7); else Root.Margin = new Thickness(0); };
        Log.Info("ARCA Teleprompter iniciado · " + typeof(MainWindow).Assembly.GetName().Version);
    }

    private readonly DispatcherTimer _lookT = new() { Interval = TimeSpan.FromMilliseconds(60) };
    private bool _lookLayout;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Outputs.Refresh();
        RestoreSession();
        if (IsDemo) DemoTrimTabs();
        CompositionTarget.Rendering += OnRender;
        Voice.CheckModel();
        // Na demonstração, recursos da versão licenciada não ligam sozinhos ao abrir.
        if (Vm.App.WebOnStart && !IsDemo) _ = ToggleWeb(true);
        if (Vm.App.MosListen && !IsDemo) Mos.Start();
        RegisterHotkeys();
        UpdatePreviewTarget();
        Editor.Focus();
    }

    // ═══ Tema ═══
    public void ApplyTheme(string theme)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var old = dicts.FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/Light") == true || d.Source?.OriginalString.Contains("Themes/Dark") == true);
        var next = new ResourceDictionary { Source = new Uri(theme == "dark" ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative) };
        if (old != null) dicts[dicts.IndexOf(old)] = next; else dicts.Insert(0, next);
        Vm.App.Theme = theme; Vm.Theme = theme;
    }

    // ═══ Aparência e diagramação ═══

    /// <summary>Recalcula fonte/margens e reconstrói editor e saídas (layout) ou só repinta.</summary>
    private void ApplyLook(bool layout)
    {
        var s = Vm.S;
        _font = FontCatalog.Family(s.FontKey);
        _layout = PrompterLayout.Compute(s, FontCatalog.Factor(s.FontKey));
        var anchor = _measure.AnchorAt(Engine.Y);
        if (Editor.Document != null)
        {
            using (SuppressEdits())
            {
                Editor.Document.FontFamily = _font;
                ScriptFlow.ApplyPresentation(Editor.Document, s, _layout);
                DrawEditorNames();
            }
            LayoutEditor();
        }
        EdBg.Background = ScriptFlow.ParseBrush(s.Background, Brushes.Black);
        EdOverlay.Apply(s, _layout);
        EdSizer.RenderTransform = new ScaleTransform(Vm.EdFlipH ? -1 : 1, Vm.EdFlipV ? -1 : 1);
        var img = BackgroundImage();
        EdBgImage.Background = img == null ? null : new ImageBrush(img) { Stretch = Stretch.UniformToFill };
        EdBgImage.Opacity = s.BackgroundImageOpacity;
        if (layout) RebuildOutputs(anchor);
        else { foreach (var sf in AllSurfaces()) { sf.SetLook(s, _layout); sf.SetBackgroundImage(img, s.BackgroundImageOpacity); } Ndi.PushContent(); }
        Engine.LineHeight = _layout.LineHeightPx;
        UpdateSpeedLabels();
        Web.PublishState();
        if (layout) Web.PublishHtml();
        _forceSync = true;
    }

    private string? _bgPath; private ImageSource? _bgImg;
    private ImageSource? BackgroundImage()
    {
        if (Vm.BgImagePath == _bgPath) return _bgImg;
        _bgPath = Vm.BgImagePath; _bgImg = null;
        if (_bgPath != null)
        {
            try { var b = new BitmapImage(); b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.UriSource = new Uri(_bgPath); b.EndInit(); b.Freeze(); _bgImg = b; }
            catch (Exception e) { Flash("Imagem inválida: " + e.Message); _bgPath = null; Vm.BgImagePath = null; }
        }
        return _bgImg;
    }

    private IEnumerable<PrompterSurface> AllSurfaces()
    {
        yield return _measure; yield return _preview; yield return TalentSurface;
        foreach (var s in Outputs.Surfaces) yield return s;
    }

    /// <summary>Reconstrói uma superfície com o documento e a aparência atuais.</summary>
    public void Rebuild(PrompterSurface sf, string? alignOverride = null)
    {
        var s = Vm.S;
        var st = alignOverride != null && alignOverride != s.Align ? WithAlign(s, alignOverride) : s;
        var fd = ScriptFlow.BuildPrompter(Doc, st, _layout, _font, out var who);
        sf.SetContent(fd, who, st, _layout);
        sf.SetBackgroundImage(BackgroundImage(), s.BackgroundImageOpacity);
        sf.Y = Engine.Y;
    }
    private static PrompterSettings WithAlign(PrompterSettings s, string align) { var c = s.Clone(); c.Align = align; return c; }

    /// <summary>Texto ou layout mudaram: todas as saídas são reconstruídas mantendo a âncora de leitura.</summary>
    private void RebuildOutputs((int Block, double Frac)? keep = null)
    {
        var anchor = keep ?? _measure.AnchorAt(Engine.Y);
        _pendingAnchor = anchor;
        Rebuild(_measure);
        var td = Outputs.PreviewTarget;
        Rebuild(_preview, td?.Align);
        if (TalentLayer.Visibility == Visibility.Visible) Rebuild(TalentSurface, _talentAlign);
        Outputs.RebuildAll();
        Ndi.PushContent();
        Web.PublishHtml();
        Voice.Reload();
    }
    private (int Block, double Frac)? _pendingAnchor;

    /// <summary>Medição pronta: posições dos blocos, fim do texto, pausas, rundown.</summary>
    private void OnMeasureLayout()
    {
        _measure.InvalidateMap();
        var map = _measure.Map;
        if (map.Count == 0) return;
        Engine.LineHeight = _layout.LineHeightPx;
        Engine.EndY = Math.Max(0, _measure.ContentHeight - _layout.LineHeightPx);
        Engine.Stops = map.Where(b => Doc.Blocks.ElementAtOrDefault(b.Block)?.Kind == BlockKind.Stop).Select(b => b.Top).ToList();
        _bmBoxes = map.Where(b => Doc.Blocks.ElementAtOrDefault(b.Block)?.Kind == BlockKind.Bookmark).ToList();
        if (_pendingAnchor is { } a) { Engine.SetY(_measure.YOf(a.Block, a.Frac)); _pendingAnchor = null; }
        else Engine.SetY(Engine.Y);
        _editorMapDirty = true;
        _forceSync = true;
        UpdateRundown();
        if (Vm.AutoRun) ApplyAutoRun();
    }

    // ═══ Laço de quadros ═══
    private bool _forceSync;
    /// <summary>Livre para mostrar um aviso: prompt parado, voz desligada e nenhum diálogo aberto.</summary>
    public bool IsIdleForPrompt => !Engine.Live && !Voice.Active && DialogLayer.Visibility != Visibility.Visible && !OwnedWindows.Cast<Window>().Any(w => w.IsVisible) && IsActive;

    /// <summary>Velocidade do texto agora: a do motor ou, deslizando à mão (roda, linha), a da mola — o NDI e o navegador
    /// preveem a posição entre os quadros do app com ela (sem isso o deslize chegava neles em degraus).</summary>
    public double ShownVelocity => _scrollTarget != null ? _scrollV : Engine.Velocity;

    /// <summary>Cada quadro desenhado (relógio, posição) — usado pelo autoteste de suavidade.</summary>
    internal Action<double, double>? FrameProbe;

    private void OnRender(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed.TotalSeconds;
        var dt = now - _lastFrame; _lastFrame = now;
        if (dt <= 0) return;
        Engine.Tick(dt);
        DemoTick(dt);
        StepManualScroll(dt);
        var y = Engine.Y;
        var moved = Math.Abs(y - _lastY) > 0.001;
        if (moved || _forceSync)
        {
            _preview.Y = y;
            if (TalentLayer.Visibility == Visibility.Visible) TalentSurface.Y = y;
            foreach (var s in Outputs.Surfaces) s.Y = y;
            if (_skipEditorSync) _skipEditorSync = false;
            else if (Vm.Follow) SyncEditorScroll(y);
            _lastY = y; _forceSync = false;
        }
        FrameProbe?.Invoke(now, y);
        Ndi.SetPosition(y, ShownVelocity);
        Web.PublishPosition(moved);
        _tick += dt;
        if (_tick >= 0.2) { _tick = 0; SlowTick(); }
    }

    /// <summary>~5×/s: leituras, marcador atual, HUD, estado web, legenda.</summary>
    private void SlowTick()
    {
        LicenseSlowTick();
        var y = Engine.Y; var s = Vm.S;
        var pos = Engine.SecondsAt(y); var total = Engine.SecondsAt(Engine.EndY);
        var nowT = DateTime.Now;
        Vm.Readouts[0].Label = Vm.StampMode == 1 ? "RESTANTE" : "POSIÇÃO";
        Vm.Readouts[0].Value = TextUtil.FormatMmSs(Vm.StampMode == 1 ? total - pos : pos);
        Vm.Readouts[1].Value = TextUtil.FormatMmSs(total);
        Vm.Readouts[2].Value = nowT.ToString("HH:mm:ss");
        // Fim previsto só faz sentido rodando; parado não acompanha o relógio.
        Vm.Readouts[3].Value = Engine.Playing && total > pos ? nowT.AddSeconds(total - pos).ToString("HH:mm:ss") : "--:--:--";
        var lh = Math.Max(1, _layout.LineHeightPx);
        Vm.Readouts[4].Value = $"{(int)(y / lh) + 1} / {(int)Math.Ceiling(Engine.EndY / lh) + 1}";
        Vm.TotalLabel = TextUtil.FormatMmSs(total);
        TalentStampText.Text = (Vm.StampMode == 1 ? "−" : "") + TextUtil.FormatMmSs(Vm.StampMode == 1 ? total - pos : pos);

        // Marcador atual e HUD do talento.
        var cur = -1;
        for (var i = 0; i < _bmBoxes.Count; i++) if (_bmBoxes[i].Top <= y + lh * 0.5) cur = i;
        if (cur != _cur) { _cur = cur; for (var i = 0; i < Vm.Bookmarks.Count; i++) Vm.Bookmarks[i].Current = i == cur; }
        var next = cur + 1 < _bmBoxes.Count ? _bmBoxes[cur + 1].Top : Engine.EndY;
        var left = Engine.SecondsAt(Math.Max(0, next - y));
        var block = cur >= 0 && cur < Vm.Bookmarks.Count ? Vm.Bookmarks[cur].Name : (Vm.Bookmarks.FirstOrDefault()?.Name ?? "Início");
        var last = _bmBoxes.Count > 1 && cur == _bmBoxes.Count - 1;
        foreach (var sf in HudSurfaces()) sf.SetHud(block, TextUtil.FormatMmSs(left), left < 10, last, nowT.ToString("HH:mm:ss"));
        if (_cur >= 0 && _cur < Vm.Bookmarks.Count) UpdateBookmarkSub(_cur);
        Web.PublishState();
        if (Vm.App.CaptionOut && Engine.Live) Caption.Send(CurrentLineText());
    }

    /// <summary>Aplica as escolhas de "Informações na tela" ao talento e às telas do prompter.</summary>
    public void ApplyHudOptions()
    {
        var a = Vm.App;
        // Talento: com a barra de controle visível as informações sobem para cima dela (não ficam escondidas atrás);
        // sem a barra, ficam na borda de baixo, antes do contador de tempo (canto direito).
        var bar = TalentCtrl.Visibility == Visibility.Visible;
        TalentSurface.SetHudOptions(a.HudBlock, a.HudTime, a.HudClock, a.HudBottom, bar ? 84 : 22, bar ? 20 : 140);
        Outputs.SetHudOptions(a.HudBlock, a.HudTime, a.HudClock, a.HudBottom);
    }

    private IEnumerable<PrompterSurface> HudSurfaces() { yield return TalentSurface; foreach (var s in Outputs.Surfaces) yield return s; }

    // ═══ Reprodução ═══
    public void TogglePlay()
    {
        if (Voice.Active) Voice.Stop();
        if (Engine.Live) Pause(); else Play();
    }

    public void Play()
    {
        if (!DemoCanPlay()) return;
        if (InlineEdit.Visibility == Visibility.Visible) FinishInline(true);
        if (Vm.App.Countdown && !Engine.Live) { Engine.SetCountdown(3); Vm.Count = 3; _countT.Start(); ShowCount(3); return; }
        Go();
    }
    private void Go()
    {
        Engine.Play();
        if (Vm.App.RecordWithPrompt && Vm.CamOn && !Vm.Recording) _ = Video.StartRec();
    }
    private void CountTick(object? s, EventArgs e)
    {
        var n = Engine.Countdown - 1;
        if (n <= 0) { _countT.Stop(); ShowCount(0); Go(); }
        else { Engine.SetCountdown(n); ShowCount(n); }
    }
    private void ShowCount(int n)
    {
        Vm.Count = n;
        EdCount.Visibility = n > 0 && TalentLayer.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var sf in HudSurfaces()) sf.SetCount(n);
        Web.PublishState();
    }

    public void Pause()
    {
        _countT.Stop(); ShowCount(0);
        Engine.Pause();
        if (Vm.App.RecordWithPrompt && Vm.Recording) _ = Video.StopRec();
    }

    private bool _wasLive;
    private void OnPlayState()
    {
        var live = Engine.Live;
        // Pausa para exatamente onde foi pausado (o "encaixe" na linha mais próxima dava saltos).
        _wasLive = live;
        Vm.Playing = Engine.Playing; Vm.Live = live;
        if (live && Vm.App.LockOnPlay || Voice.Active) SetLocked(true);
        else if (!live && !Voice.Active) SetLocked(false);
        var icon = live ? Icons.Pause : Icons.Play;
        PromptIcon.Data = live ? Icons.Stop : Icons.Play; PromptLabel.Text = live ? "Parar" : "Prompt";
        TransportPlayIcon.Data = icon;
        EdBar.Visibility = live && TalentLayer.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        Native.KeepAwake(live);
        if (!live) TalentBar.Opacity = 1;
        Vm.Follow = true; _forceSync = true;
        UpdateStatus();
        Web.PublishState();
    }

    /// <summary>Editor bloqueado durante a reprodução (README §5.2).</summary>
    public void SetLocked(bool on)
    {
        Vm.Locked = on;
        Editor.IsReadOnly = on; UpdateCaret();
        LockIcon.Data = on ? Icons.Lock : Icons.Unlock;
        LockLabel.Text = Engine.Live ? "Bloqueado · no ar" : on ? "Bloqueado" : "Editável";
        Editor.Cursor = on ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.IBeam;
    }

    public void UpdateStatus()
    {
        Vm.StatusLabel = Engine.Live ? "No ar" : Voice.Active ? "Acompanhando por voz" : "Pronto";
        StatusDot.Fill = (Brush)FindResource(Engine.Live ? "Live" : Voice.Active ? "Text" : "Faint");
        TalentVoice.Visibility = Voice.Active ? Visibility.Visible : Visibility.Collapsed;
        TalentVoiceDot.Fill = Voice.State == VoiceState.Improvising ? (Brush)FindResource("Live") : Brushes.White;
    }

    public void UpdateNetLabel()
    {
        Vm.NetLabel = string.Join(" · ", new[] { Ndi.Running ? "NDI ●" : "", Web.Running ? $"Web {Vm.WebViewers}" : "", Mos.Running ? "MOS ●" : "" }.Where(x => x.Length > 0));
    }

    // ═══ Velocidade ═══
    public void UpdateSpeedLabels()
    {
        var s = Vm.S; var u = (SpeedUnit)Vm.SpeedUnit;
        var (v, unit) = ScrollEngine.Format(s.LinesPerSecond, u, s.WordsPerLine);
        Vm.SpeedValue = v; Vm.UnitName = unit;
        Vm.MinLabel = ScrollEngine.Format(s.MinSpeed, u, s.WordsPerLine).Value;
        Vm.MaxLabel = ScrollEngine.Format(s.MaxSpeed, u, s.WordsPerLine).Value;
        Vm.Reverse = Engine.Direction < 0;
        UpdateRundown();
    }

    public void FlashSpeed()
    {
        var s = Vm.S;
        var (v, unit) = ScrollEngine.Format(s.LinesPerSecond, (SpeedUnit)Vm.SpeedUnit, s.WordsPerLine);
        Flash($"{v} {unit}", Engine.Direction < 0 ? "Ré" : "Frente", s.SpeedT);
    }

    public void AdjustSpeed(double d)
    {
        if (Engine.SpeedLock && TalentLayer.Visibility == Visibility.Visible) { Flash("Velocidade travada"); return; }
        Vm.AutoRun = false;
        Engine.AdjustSpeed(d);
    }

    private void ApplyAutoRun()
    {
        var sec = TextUtil.ParseDuration(Vm.RunTarget);
        if (sec is null or <= 0) return;
        var t = Engine.SpeedForDuration(sec.Value);
        if (Math.Abs(t - Vm.SpeedT) > 0.005) Vm.SpeedT = t;
    }

    // ═══ Avisos ═══
    public void Flash(string text, string? sub = null, double pct = -1)
    {
        ShowFlash(text, sub, pct);
        _flashT.Stop(); _flashT.Start();
        Web.Flash(text, sub);
    }

    private void ShowFlash(string? text, string? sub, double pct)
    {
        var talent = TalentLayer.Visibility == Visibility.Visible;
        if (text != null && !talent) EdFlash.Show(text, sub, pct); else EdFlash.Hide();
        // Aviso de velocidade/marcador é para o operador: só no editor e na janela do talento, nunca nas telas do prompter.
        TalentSurface.SetFlash(text, sub, pct);
    }

    // ═══ Rundown (lista de marcadores e pausas) ═══
    private void UpdateRundown()
    {
        var s = Vm.S;
        var rd = Doc.Rundown();
        var lh = Math.Max(1, _layout.LineHeightPx);
        // Lista de marcadores: recria só se a estrutura mudou.
        var sig = string.Join("|", rd.Select(b => b.Name + (b.Skip ? "*" : "")));
        if (sig != _bmSig)
        {
            _bmSig = sig;
            Vm.Bookmarks.Clear();
            for (var i = 0; i < rd.Count; i++) Vm.Bookmarks.Add(new BookmarkVM { Index = i, Block = rd[i].BookmarkIndex, Name = rd[i].Name.Length > 0 ? rd[i].Name : "Sem nome", Skip = rd[i].Skip, Current = i == _cur });
        }
        for (var i = 0; i < Vm.Bookmarks.Count; i++) UpdateBookmarkSub(i);
        var stops = Doc.Blocks.Select((b, i) => (b, i)).Where(x => x.b.Kind == BlockKind.Stop).ToList();
        Vm.Stops.Clear();
        for (var i = 0; i < stops.Count; i++)
        {
            var top = _measure.TopOf(stops[i].i);
            Vm.Stops.Add(new StopVM { Index = i, Block = stops[i].i, Name = stops[i].b.PlainText.Length > 0 ? stops[i].b.PlainText : "Pausa", Time = double.IsNaN(top) ? "—" : TextUtil.FormatMmSs(Engine.SecondsAt(top)) });
        }
        StopsHeader.Visibility = stops.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        var total = Rundown.TotalSeconds(rd, s);
        var over = total - s.ProgramSeconds;
        Vm.RundownTotal = TextUtil.FormatMmSs(total);
        Vm.Over = over > 0;
        Vm.OverLabel = (over > 0 ? "+" : "−") + TextUtil.FormatMmSs(Math.Abs(over));
        Vm.OverTitle = (over > 0 ? "Estoura o programa" : "Sobra no programa") + " · estimado " + Vm.RundownTotal;
        Vm.Words = Doc.WordCount;
    }
    private string? _bmSig;

    private void UpdateBookmarkSub(int i)
    {
        var rd = Doc.Rundown(); if (i >= rd.Count || i >= Vm.Bookmarks.Count) return;
        var b = rd[i]; var vm = Vm.Bookmarks[i];
        if (b.Skip) { vm.Sub = "Pulado"; return; }
        var top = _measure.TopOf(b.BookmarkIndex);
        var est = Rundown.BlockSeconds(b, Vm.S);
        vm.Sub = (double.IsNaN(top) ? "—" : TextUtil.FormatMmSs(Engine.SecondsAt(top))) + " · " + TextUtil.FormatMmSs(est)
                 + (_rehearsal.TryGetValue(i, out var r) ? " · E " + TextUtil.FormatMmSs(r) : "");
    }

    /// <summary>Bloco do documento sob a linha de leitura.</summary>
    public int CurrentBlock() => _measure.AnchorAt(Engine.Y).Block;

    /// <summary>Y da palavra no layout do prompter (para o acompanhamento por voz).</summary>
    public double WordY(int block, int charIndex) => _measure.WordY(block, charIndex);
    public (int Block, double Frac) AnchorAt(double y) => _measure.AnchorAt(y);
    public double ReadingY(int block, int charIndex, int length, double lead)
        => _measure.ReadingY(block, charIndex, length, lead, IsReadable);

    /// <summary>Texto da linha sob a linha de leitura (saída de legenda).</summary>
    public string CurrentLineText()
    {
        try
        {
            var tb = _measure.TextBox;
            var p = tb.GetPositionFromPoint(new Point(_layout.PaddingLeft + 2, Engine.Y + _layout.LineHeightPx * 0.5), true);
            if (p == null) return "";
            var start = p.GetLineStartPosition(0) ?? p; var end = p.GetLineStartPosition(1) ?? tb.Document.ContentEnd;
            return new TextRange(start, end).Text.Trim();
        }
        catch { return ""; }
    }

    // ═══ Prévia do inspetor ═══
    public void UpdatePreviewTarget()
    {
        var d = Outputs.PreviewTarget;
        Vm.PreviewLabel = "SAÍDA · " + (d?.Nick ?? "JANELA DO TALENTO").ToUpperInvariant() + (d != null && (d.FlipH || d.FlipV) ? " (ESPELHADA)" : "");
        _preview.FlipH = d?.FlipH ?? _talentFlipH; _preview.FlipV = d?.FlipV ?? _talentFlipV;
        Rebuild(_preview, d?.Align);
    }
    public void PreviewBlink(bool on) => _preview.Blink(on && Outputs.PreviewTarget?.Find == true);

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.SpeedT): Vm.S.SpeedT = Vm.SpeedT; UpdateSpeedLabels(); break;
            case nameof(MainViewModel.SpeedUnit): UpdateSpeedLabels(); break;
            case nameof(MainViewModel.AutoRun): case nameof(MainViewModel.RunTarget): if (Vm.AutoRun) ApplyAutoRun(); break;
            case nameof(MainViewModel.ShowBookmarks): ColBm.MinWidth = Vm.ShowBookmarks ? 176 : 0; ColBm.Width = Vm.ShowBookmarks ? new GridLength(0.2, GridUnitType.Star) : new GridLength(0); BmPanel.Visibility = Vm.ShowBookmarks ? Visibility.Visible : Visibility.Collapsed; break;
            case nameof(MainViewModel.ShowInspector): ColInsp.MinWidth = Vm.ShowInspector ? 316 : 0; ColInsp.Width = Vm.ShowInspector ? new GridLength(0.3, GridUnitType.Star) : new GridLength(0); InspPanel.Visibility = Vm.ShowInspector ? Visibility.Visible : Visibility.Collapsed; break;
            case nameof(MainViewModel.TalentHud): Vm.App.TalentHud = Vm.TalentHud; TalentSurface.ShowHud = Vm.TalentHud; Outputs.SetHud(Vm.TalentHud); break;
            case nameof(MainViewModel.Rehearse): if (!Vm.Rehearse) { } break;
            // Interruptores: o serviço é a fonte da verdade; só age quando o pedido difere do estado real.
            case nameof(MainViewModel.NdiOn): if (Vm.NdiOn != Ndi.Running) { if (Vm.NdiOn) Ndi.Start(); else Ndi.Stop(); } break;
            case nameof(MainViewModel.WebOn): if (Vm.WebOn != Web.Running && !Web.Busy) _ = ToggleWeb(Vm.WebOn); break;
            case nameof(MainViewModel.Follow): CursorToPrompterBtn.Visibility = Vm.Follow ? Visibility.Collapsed : Visibility.Visible; break;
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            if (TalentLayer.Visibility == System.Windows.Visibility.Visible) ToggleTalent();
            SaveSession();
            Stores.SaveSettings(Vm.App);
            Voice.Stop();
            Ndi.Stop();
            _ = Video.Off();
            Mos.Stop();
            _ = Web.Stop();
            Outputs.CloseAll();
            UnregisterHotkeys();
            Native.KeepAwake(false);
        }
        catch (Exception ex) { Log.Error("Fechar", ex); }
        Log.Info("ARCA Teleprompter encerrado");
    }
}
