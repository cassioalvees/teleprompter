using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.App.Services;
using Arca.App.ViewModels;
using Arca.Core.Engine;
using Arca.Core.Model;

namespace Arca.App;

/// <summary>Teclado (README §5.7), ações, janela do talento e botões da interface.</summary>
public partial class MainWindow
{
    /// <summary>Ação sendo remapeada em Preferências › Atalhos (próxima tecla vira o atalho).</summary>
    public string? CaptureAction { get; set; }
    public event Action<string>? KeyCaptured;
    private bool _big;

    public void HandleKey(KeyEventArgs e, bool fromOutput, ModifierKeys? modifiers = null)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var mods = modifiers ?? Keyboard.Modifiers;   // modifiers: só no autoteste
        var ctrl = mods.HasFlag(ModifierKeys.Control); var alt = mods.HasFlag(ModifierKeys.Alt); var shift = mods.HasFlag(ModifierKeys.Shift);
        if (CaptureAction != null)
        {
            e.Handled = true;
            if (key != Key.Escape && key is not (Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt))
            { Vm.App.Keymap[CaptureAction] = key.ToString(); KeyCaptured?.Invoke(key.ToString()); }
            CaptureAction = null;
            return;
        }
        Vm.LastKey = "Última entrada: " + Keymap.Display(key.ToString());
        if (key == Key.Escape)
        {
            if (DialogLayer.Visibility == Visibility.Visible) { CloseDialog(); e.Handled = true; return; }
            if (InlineEdit.Visibility == Visibility.Visible) { FinishInline(false); e.Handled = true; return; }
            // Esc fecha a tela do talento (que abre em tela cheia); sair só da tela cheia: botão □ ou F11.
            if (TalentLayer.Visibility == Visibility.Visible) { ToggleTalent(); e.Handled = true; return; }
            if (_fullscreen) { ToggleFullscreen(); e.Handled = true; return; }
            return;
        }
        if (DialogLayer.Visibility == Visibility.Visible) return;
        var focus = Keyboard.FocusedElement as DependencyObject;
        var inEditor = focus == Editor || (focus is FrameworkContentElement fce && Editor.Document != null && fce.Parent != null);
        var typing = !fromOutput && ((focus is TextBox or PasswordBox or ComboBox) || (inEditor && !Vm.Locked));
        if (InlineEdit.Visibility == Visibility.Visible && !fromOutput) return;

        if (key == Key.F5) { e.Handled = true; ToggleTalent(); return; }
        if (key == Key.F11) { e.Handled = true; if (TalentLayer.Visibility == Visibility.Visible) ToggleTalentFullscreen(); else ToggleFullscreen(); return; }
        if (alt && !ctrl && key is >= Key.D0 and <= Key.D8 && TalentLayer.Visibility != Visibility.Visible) { e.Handled = true; SetWho(key - Key.D0); return; }
        if (ctrl)
        {
            Action? a = key switch
            {
                Key.P => () => PrintScript(shift),
                Key.W => () => { if (_active != null) CloseTab(_active); },
                Key.Enter => TogglePlay,
                Key.E => ToggleLock,
                Key.K => () => InsertBlock(BlockKind.Bookmark),
                Key.J => CursorToPrompter,
                Key.L => () => { Vm.Follow = true; _forceSync = true; },
                Key.F => ShowFind,
                Key.S => () => Save(shift),
                Key.O => OpenFileDialog,
                Key.N => NewTab,
                Key.OemComma => () => ShowPrefs("geral"),
                _ => null
            };
            if (a != null) { e.Handled = true; a(); return; }
            if (key is >= Key.D1 and <= Key.D9) { e.Handled = true; JumpBookmark(key - Key.D1); return; }
            return;
        }
        if (typing) return;
        var name = key.ToString();
        // Compara pela tecla, não pelo nome: o Windows tem dois nomes para a mesma tecla (PageDown = "Next", ] = "Oem6"…).
        var act = Vm.App.Keymap.FirstOrDefault(kv => SameKey(kv.Value, key)).Key;
        if (act == null && key is >= Key.D1 and <= Key.D9) { e.Handled = true; Vm.AutoRun = false; Engine.Preset(key - Key.D0); return; }
        if (act == null && key is >= Key.NumPad1 and <= Key.NumPad9) { e.Handled = true; Vm.AutoRun = false; Engine.Preset(key - Key.NumPad0); return; }
        if (act == null && key == Key.Back) { e.Handled = true; GoTo(0, textLine: true); return; }
        if (act == null) return;
        e.Handled = true;
        _big = shift; DoAction(act); _big = false;
    }

    private static bool SameKey(string mapped, Key key) =>
        Enum.TryParse<Key>(mapped, true, out var k) ? k == key : string.Equals(mapped, key.ToString(), StringComparison.OrdinalIgnoreCase);

    public void DoAction(string a)
    {
        var lh = _layout.LineHeightPx; var st = _big ? 0.15 : 0.05; var run = Engine.Live;
        switch (a)
        {
            case "play": TogglePlay(); break;
            case "faster": if (run) AdjustSpeed(st); else LineStep(_big ? 5 : 1); break;
            case "slower": if (run) AdjustSpeed(-st); else LineStep(_big ? -5 : -1); break;
            case "faster2": AdjustSpeed(st); break;
            case "slower2": AdjustSpeed(-st); break;
            case "reverse": Engine.Reverse(); Vm.Reverse = Engine.Direction < 0; break;
            case "stop": if (Engine.Live) Pause(); Flash("Parado"); break;
            case "rewind": GoTo(0, textLine: true); break;
            case "toEnd": GoTo(Engine.EndY); break;
            case "nextBm": JumpBookmark(_cur + 1); break;
            case "prevBm": JumpBookmark(Math.Max(0, _cur - 1)); break;
            case "nextPara": JumpParagraph(1); break;
            case "prevPara": JumpParagraph(-1); break;
            case "editHere": if (TalentLayer.Visibility == Visibility.Visible) EditHere(); break;
            case "voice":
                Voice.ToggleMic();
                Flash(Voice.Active ? "Voz ligada" : "Voz desligada");
                break;
            case "mirror":
                var d = Outputs.PreviewTarget;
                if (d != null) { d.FlipH = !d.FlipH; Flash(d.FlipH ? "Espelhado" : "Espelho desligado"); }
                else { _talentFlipH = !_talentFlipH; ApplyTalentFlip(); }
                break;
        }
    }

    private void Nudge(double dy) { Engine.Nudge(dy); Vm.Follow = true; _forceSync = true; }

    /// <summary>
    /// Linha anterior/próxima: a linha de leitura cai exatamente sobre uma linha de <b>texto</b> — título de
    /// marcador e aviso de pausa são informação, não linha de leitura, e são pulados.
    /// </summary>
    public void LineStep(int n)
    {
        var dir = Math.Sign(n);
        // Parado: desliza até a linha (como a roda do mouse) — no app, no talento, nas telas e no NDI. Rodando: vai direto.
        var from = Engine.Live ? Engine.Y : _scrollTarget ?? Engine.Y;
        var y = from;
        for (var i = 0; i < Math.Abs(n); i++)
        {
            var at = y; var t = double.NaN;
            for (var guard = 0; guard < 8; guard++)
            {
                t = _measure.LineTop(at, dir);
                // Linha em branco (parágrafo vazio) não tem linha anterior/próxima para o RichTextBox: vai direto ao
                // bloco com texto mais perto. Antes parava no fim do parágrafo dizendo "Última linha"/"Primeira linha".
                if (double.IsNaN(t) || Math.Abs(t - at) < 0.5 || (dir > 0 ? t < at : t > at))
                {
                    var block = _measure.AnchorAt(at + 1).Block;
                    t = dir > 0 ? NextReadableTop(block) : PrevReadableLastTop(block);
                    break;
                }
                if (IsTextLine(t)) break;
                at = t;
            }
            if (double.IsNaN(t)) { Flash(dir > 0 ? "Última linha" : "Primeira linha"); break; }
            y = t;
        }
        if (Math.Abs(y - from) < 0.01) return;
        if (Engine.Live) GoTo(y);
        else { _scrollTarget = y; if (Voice.Active) Voice.JumpTo(y); }
        Vm.Follow = true; _forceSync = true;
    }

    /// <summary>A linha em <paramref name="y"/> é texto do roteiro (não marcador nem pausa)?</summary>
    private bool IsTextLine(double y)
    {
        var (block, _) = _measure.AnchorAt(y + 1);
        return IsReadable(block);
    }

    /// <summary>Bloco com texto para ler (não é marcador, pausa nem linha em branco).</summary>
    public bool IsReadable(int block) => Doc.Blocks.ElementAtOrDefault(block) is not { } b || (b.Kind == BlockKind.Text && !string.IsNullOrWhiteSpace(b.PlainText));

    /// <summary>Ao pausar, encaixa a linha de leitura na linha de texto mais próxima.</summary>
    private void SnapToLine()
    {
        var y = Engine.Y;
        var t0 = _measure.LineTop(y, 0); if (double.IsNaN(t0)) return;
        var t1 = _measure.LineTop(y, 1);
        var target = !double.IsNaN(t1) && y - t0 > (t1 - t0) / 2 ? t1 : t0;
        // Não para sobre o título de um marcador: vai para a primeira linha de texto depois dele.
        for (var guard = 0; guard < 8 && !IsTextLine(target); guard++) { var nx = _measure.LineTop(target, 1); if (double.IsNaN(nx)) break; target = nx; }
        if (Math.Abs(target - y) < _layout.LineHeightPx * 3) { Engine.SetY(target); _forceSync = true; }
    }

    /// <summary>Comando vindo do monitor web / iPad.</summary>
    public void RemoteCommand(string name, double? value)
    {
        switch (name)
        {
            case "togglePlay": TogglePlay(); break;
            case "play": if (!Engine.Live) Play(); break;
            case "pause": if (Engine.Live) Pause(); break;
            case "faster": AdjustSpeed(0.05); break;
            case "slower": AdjustSpeed(-0.05); break;
            case "speed": if (value is { } v) { Vm.AutoRun = false; Vm.SpeedT = Math.Clamp(v, 0, 1); } break;
            case "lineUp": LineStep(-1); break;
            case "lineDown": LineStep(1); break;
            case "nudge": if (value is { } dy && !Engine.Live) Nudge(Math.Clamp(dy, -20000, 20000)); break;
            case "voice": DoAction("voice"); break;
            case "newWindow": Outputs.AddWindow(); break;   // só pela linha de comando (o monitor web não envia este)
            case "ndi": ToggleNdi(); break;   // idem
            case "licenca": _ = ShowLicense(); break;   // idem (testes da tela de licença)
            case "gotoBm": if (value is { } i) JumpBookmark((int)i); break;
            default: DoAction(name); break;
        }
    }

    // ═══ Palavras por linha (Ctrl + roda) ═══
    private void ChangeWpl(int d)
    {
        var n = Math.Clamp(Math.Round(Vm.WordsPerLine) + d, 2, 12);
        if (n != Vm.WordsPerLine) { Vm.WordsPerLine = n; Flash($"{n} palavras por linha"); }
    }

    // ═══ Janela do talento ═══
    private bool _talentFlipH, _talentFlipV; private string _talentAlign = "left";
    private ToggleButton? _tCtrl, _tStamp, _tFlipH, _tFlipV, _tLock;

    // Janela do talento: janela própria (o editor continua visível; pode ir para outro monitor e para tela cheia).
    private Window? _talentWin;
    private bool _talentFull;
    private WindowState _talentPrevState;

    public void ToggleTalent()
    {
        if (InlineEdit.Visibility == Visibility.Visible) FinishInline(false);
        var show = TalentLayer.Visibility != Visibility.Visible;
        if (show && IsDemo && Outputs.OpenCount > 0 && !Allow("telas")) return;
        TalentLayer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            TalentSurface.ShowHud = Vm.TalentHud;
            ApplyHudOptions();
            OpenTalentWindow();
            Rebuild(TalentSurface, _talentAlign);
            ApplyTalentFlip();
            TalentBar.Opacity = 1;
        }
        else CloseTalentWindow();
        Vm.Talent = show;
        EdBar.Visibility = Engine.Live && !show ? Visibility.Visible : Visibility.Collapsed;
        _forceSync = true;
    }

    private void OpenTalentWindow()
    {
        if (TalentLayer.Parent is Panel p) p.Children.Remove(TalentLayer);
        if (_talentWin == null)
        {
            var wa = SystemParameters.WorkArea;
            _talentWin = new Window
            {
                Title = "Janela do talento — ARCA Teleprompter", Background = System.Windows.Media.Brushes.Black, Icon = Icon,
                Width = Math.Min(1280, wa.Width * 0.75), Height = Math.Min(760, wa.Height * 0.75),
                WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = true, ShowActivated = true,
                // Uma barra só: a barra do talento é a barra de título (sem a moldura do Windows por cima).
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.CanResize,
                DataContext = Vm // a janela nova não herda o contexto da principal (controles de velocidade, opacidade…)
            };
            System.Windows.Shell.WindowChrome.SetWindowChrome(_talentWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0), UseAeroCaptionButtons = false });
            _talentWin.PreviewKeyDown += (_, e) => HandleKey(e, false);
            // Fechar pelo X da janela = sair do modo talento.
            _talentWin.Closed += (_, _) => { _talentWin = null; if (TalentLayer.Visibility == Visibility.Visible) ToggleTalent(); };
        }
        _talentWin.Content = TalentLayer;
        _talentWin.Show();
        _talentWin.Activate();
        // A tela do talento é para ler: abre em tela cheia de verdade (cobre a barra de tarefas do Windows) no monitor
        // em que abriu. Esc ou o botão da barra voltam para janela.
        if (!_talentFull) ToggleTalentFullscreen();
    }

    /// <summary>A barra do talento é a barra de título: arrastar move a janela; duplo clique maximiza/restaura.</summary>
    private void TalentBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_talentWin == null || e.OriginalSource is DependencyObject d && FindParent<System.Windows.Controls.Primitives.ButtonBase>(d) != null) return;
        if (FindParent<Slider>(e.OriginalSource as DependencyObject) != null) return;
        if (e.ClickCount == 2) { ToggleTalentFullscreen(); return; }
        if (_talentFull) return;   // em tela cheia a janela não se arrasta
        try { _talentWin.DragMove(); } catch { /* botão já solto */ }
    }

    // Minimizar e maximizar a janela do talento (ela não tem a barra do Windows).
    private void TalentMin_Click(object s, RoutedEventArgs e) { if (_talentWin != null) _talentWin.WindowState = WindowState.Minimized; }
    // Maximizar = tela cheia de verdade: o "maximizado" do Windows deixa a barra de tarefas por cima do texto.
    private void TalentMax_Click(object s, RoutedEventArgs e) { if (_talentWin != null) ToggleTalentFullscreen(); }

    private static T? FindParent<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d != null) { if (d is T t) return t; d = d is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D ? System.Windows.Media.VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d); }
        return null;
    }

    private void CloseTalentWindow()
    {
        var w = _talentWin; _talentWin = null; _talentFull = false;
        if (w == null) return;
        w.Content = null;
        w.Close();
        Activate();
    }

    /// <summary>Tela cheia da janela do talento no monitor em que ela está (F11 / botão da barra).</summary>
    private void ToggleTalentFullscreen()
    {
        var w = _talentWin; if (w == null) return;
        _talentFull = !_talentFull;
        if (_talentFull)
        {
            _talentPrevState = w.WindowState;
            w.WindowStyle = WindowStyle.None; w.ResizeMode = ResizeMode.NoResize; w.WindowState = WindowState.Normal;
            Native.PlaceWindow(w, Native.MonitorFor(new System.Windows.Interop.WindowInteropHelper(w).Handle));
        }
        else
        {
            w.WindowStyle = WindowStyle.None; w.ResizeMode = ResizeMode.CanResize; w.WindowState = _talentPrevState;
            if (_talentPrevState == WindowState.Normal) { var wa = SystemParameters.WorkArea; w.Width = Math.Min(1280, wa.Width * 0.75); w.Height = Math.Min(760, wa.Height * 0.75); w.Left = wa.Left + (wa.Width - w.Width) / 2; w.Top = wa.Top + (wa.Height - w.Height) / 2; }
        }
    }

    private void ApplyTalentFlip()
    {
        TalentSurface.FlipH = _talentFlipH; TalentSurface.FlipV = _talentFlipV;
        TalentTitle.Text = _talentFlipH || _talentFlipV ? "Janela do operador · espelhada" : "Janela do operador · leitura normal";
        if (_tFlipH != null) { _tFlipH.IsChecked = _talentFlipH; _tFlipV!.IsChecked = _talentFlipV; }
        if (Outputs.PreviewTarget == null) UpdatePreviewTarget();
    }

    private void BuildTalentTools()
    {
        ToggleButton T(string icon, string tip, bool on, Action<bool> set)
        {
            var b = new ToggleButton { Style = (Style)FindResource("ToolToggle"), ToolTip = tip, IsChecked = on, Focusable = false, Content = new Icon { Data = icon, Size = 15 } };
            b.Click += (_, _) => set(b.IsChecked == true);
            TalentTools.Children.Add(b);
            return b;
        }
        _tCtrl = T(Icons.Controls, "Mostrar controles", true, v => { TalentCtrl.Visibility = v ? Visibility.Visible : Visibility.Collapsed; ApplyHudOptions(); });
        _tStamp = T(Icons.Clock, "Mostrar tempo", true, v => TalentStamp.Visibility = v ? Visibility.Visible : Visibility.Collapsed);
        _tFlipH = T(Icons.FlipH, "Espelhar esta janela (horizontal)", false, v => { _talentFlipH = v; ApplyTalentFlip(); });
        _tFlipV = T(Icons.FlipV, "Espelhar esta janela (vertical)", false, v => { _talentFlipV = v; ApplyTalentFlip(); });
        _tLock = T(Icons.Lock, "Travar velocidade", false, v => { Engine.SpeedLock = v; TalentCtrl.Speed.IsEnabled = !v; });
        var al = new Button { Style = (Style)FindResource("IconBtn"), ToolTip = "Alinhamento desta janela", Focusable = false, Content = new Icon { Data = Icons.AlignL, Size = 15 } };
        al.Click += (_, _) => { _talentAlign = _talentAlign == "center" ? "left" : "center"; ((Icon)al.Content).Data = _talentAlign == "center" ? Icons.AlignC : Icons.AlignL; Rebuild(TalentSurface, _talentAlign); };
        TalentTools.Children.Add(al);
        // Tela cheia: botão de maximizar da barra (□), duplo clique na barra ou F11 — sem botão repetido aqui.
    }

    private void Talent_MouseMove(object sender, MouseEventArgs e)
    {
        // A barra superior some enquanto o texto rola; volta ao levar o mouse ao topo.
        var y = e.GetPosition(TalentLayer).Y;
        TalentBar.Opacity = !Engine.Live || y < 70 ? 1 : 0;
        TalentBar.IsHitTestVisible = TalentBar.Opacity > 0;
        TalentLayer.Cursor = Engine.Live && y >= 70 ? Cursors.None : null;
    }

    private void Talent_Wheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { ChangeWpl(e.Delta < 0 ? 1 : -1); return; }
        if (Engine.Live) { WheelInput(e.Delta); return; }
        if (!Engine.SpeedLock) WheelInput(e.Delta);
    }

    private Point? _drag; private double _dragY; private bool _dragMoved;
    private void Talent_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { StartInline(); e.Handled = true; return; }
        _drag = e.GetPosition(TalentSurface); _dragY = Engine.Y; _dragMoved = false;
        TalentSurface.CaptureMouse();
    }
    private void Talent_DragMove(object sender, MouseEventArgs e)
    {
        if (_drag is not { } p || e.LeftButton != MouseButtonState.Pressed || Engine.Live) return;
        var dy = e.GetPosition(TalentSurface).Y - p.Y;
        if (Math.Abs(dy) > 4) _dragMoved = true;
        if (_dragMoved) { var k = TalentSurface.ActualWidth / PrompterLayout.Width; Engine.SetY(_dragY - dy / k * (_talentFlipV ? -1 : 1)); _forceSync = true; }
    }
    private void Talent_MouseUp(object sender, MouseButtonEventArgs e) { _drag = null; TalentSurface.ReleaseMouseCapture(); }

    /// <summary>E / "Editar trecho": pausa, fecha o talento e põe o cursor no parágrafo que estava sendo lido.</summary>
    public void EditHere()
    {
        var block = CurrentBlock();
        if (Engine.Live) Pause();
        if (Voice.Active) Voice.Stop();
        if (TalentLayer.Visibility == Visibility.Visible) ToggleTalent();
        SetLocked(false);
        var fd = Editor.Document;
        var target = fd.Blocks.Skip(block).FirstOrDefault(b => b is Paragraph) as Paragraph;
        if (target == null) return;
        Editor.Focus();
        Editor.CaretPosition = target.ContentEnd;
        target.BringIntoView();
        _forceSync = true;
    }

    // Edição rápida dentro do talento (duplo clique, pausado): o parágrafo atual, sem perder a posição.
    private int _inlineBlock = -1;
    private void StartInline()
    {
        if (Engine.Live || Engine.Countdown > 0) return;
        var b = CurrentBlock();
        var blk = Doc.Blocks.ElementAtOrDefault(b);
        if (blk is not { Kind: BlockKind.Text }) { Flash("Posicione o texto num parágrafo para editar"); return; }
        _inlineBlock = b;
        InlineText.Text = blk.PlainText;
        InlineEdit.Visibility = Visibility.Visible;
        InlineText.Focus(); InlineText.CaretIndex = InlineText.Text.Length;
    }

    public void FinishInline(bool apply)
    {
        if (InlineEdit.Visibility != Visibility.Visible) return;
        InlineEdit.Visibility = Visibility.Collapsed;
        if (!apply || _inlineBlock < 0) { Focus(); return; }
        var fd = Editor.Document;
        if (fd.Blocks.ElementAtOrDefault(_inlineBlock) is Paragraph p)
        {
            var old = Doc.Blocks[_inlineBlock];
            if (InlineText.Text != old.PlainText)
            {
                // Se o parágrafo tinha um só estilo, preserva-o; senão vira texto simples.
                var f = old.Runs.Count == 1 ? old.Runs[0] : new TextRun("");
                p.Inlines.Clear();
                p.Inlines.AddRange(ScriptFlow.Inlines(new ScriptBlock { Runs = { f with { Text = InlineText.Text } } }));
                MarkDirty(); CommitEdits();
                Flash("Trecho atualizado");
            }
        }
        Focus();
    }
    private void InlineText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; FinishInline(true); }
        else if (e.Key == Key.Escape) { e.Handled = true; FinishInline(false); }
    }
    private void InlineCancel_Click(object sender, RoutedEventArgs e) => FinishInline(false);
    private void InlineDone_Click(object sender, RoutedEventArgs e) => FinishInline(true);

    // ═══ Tela cheia ═══
    private bool _fullscreen; private WindowState _prevState;
    public void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        if (_fullscreen)
        {
            _prevState = WindowState;
            WindowState = WindowState.Normal;
            var h = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var mon = Native.MonitorFor(h);
            Topmost = true;
            Native.PlaceWindow(this, mon);
        }
        else { Topmost = false; WindowState = _prevState; if (_prevState == WindowState.Normal) FitToWorkArea(); }
    }

    // ═══ Handlers da interface ═══
    private void WinMin_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void WinMax_Click(object s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void WinClose_Click(object s, RoutedEventArgs e) => Close();
    private void New_Click(object s, RoutedEventArgs e) => NewTab();
    private void NewTab_Click(object s, RoutedEventArgs e) => NewTab();
    private void Open_Click(object s, RoutedEventArgs e) => OpenFileDialog();
    private void Save_Click(object s, RoutedEventArgs e) => Save(false);
    private void SaveAs_Click(object s, RoutedEventArgs e) => Save(true);
    private void CloseTab_Click(object s, RoutedEventArgs e) { if (_active != null) CloseTab(_active); }
    private void ExportTxt_Click(object s, RoutedEventArgs e) => ExportTxt();
    private void Print_Click(object s, RoutedEventArgs e) => PrintScript(false);
    private void Pdf_Click(object s, RoutedEventArgs e) => PrintScript(true);
    private void Undo_Click(object s, RoutedEventArgs e) => Undo();
    private void Redo_Click(object s, RoutedEventArgs e) => Redo();
    private void Find_Click(object s, RoutedEventArgs e) => ShowFind();
    private void Lock_Click(object s, RoutedEventArgs e) => ToggleLock();
    private void Bold_Click(object s, RoutedEventArgs e) => Exec(EditingCommands.ToggleBold);
    private void Italic_Click(object s, RoutedEventArgs e) => Exec(EditingCommands.ToggleItalic);
    private void Underline_Click(object s, RoutedEventArgs e) => Exec(EditingCommands.ToggleUnderline);
    private void InsertBm_Click(object s, RoutedEventArgs e) => InsertBlock(BlockKind.Bookmark);
    private void InsertStop_Click(object s, RoutedEventArgs e) => InsertBlock(BlockKind.Stop);
    private void Talent_Click(object s, RoutedEventArgs e) => ToggleTalent();
    private void Play_Click(object s, RoutedEventArgs e) => TogglePlay();
    private void Rewind_Click(object s, RoutedEventArgs e) => GoTo(0, textLine: true);
    private void Reverse_Click(object s, RoutedEventArgs e) { Engine.Reverse(); Vm.Reverse = Engine.Direction < 0; }
    // Botões da barra de baixo: mesmos comandos do controle remoto (Tag = nome do comando).
    private void Cmd_Click(object s, RoutedEventArgs e) { if (s is FrameworkElement { Tag: string c }) RemoteCommand(c, null); }
    private void ParaPrev_Click(object s, RoutedEventArgs e) => JumpParagraph(-1);
    private void ParaNext_Click(object s, RoutedEventArgs e) => JumpParagraph(1);
    private void LineUp_Click(object s, RoutedEventArgs e) => LineStep(-1);
    private void LineDown_Click(object s, RoutedEventArgs e) => LineStep(1);
    private void EditHere_Click(object s, RoutedEventArgs e) => EditHere();
    private void Readout_Click(object s, MouseButtonEventArgs e) => Vm.StampMode = 1 - Vm.StampMode;
    private void CursorToPrompter_Click(object s, RoutedEventArgs e) => CursorToPrompter();
    private void PauseEdit_Click(object s, RoutedEventArgs e) { Pause(); if (TalentLayer.Visibility == Visibility.Visible) ToggleTalent(); SetLocked(false); Editor.Focus(); }
    private void ClearReh_Click(object s, RoutedEventArgs e) { _rehearsal.Clear(); Vm.HasRehearsal = false; UpdateRundown(); }
    private void ToggleHud_Click(object s, RoutedEventArgs e) => Vm.TalentHud = !Vm.TalentHud;
    private void ThemeDark_Click(object s, RoutedEventArgs e) => ApplyTheme("dark");
    private void ThemeLight_Click(object s, RoutedEventArgs e) => ApplyTheme("light");
    private void ToggleBm_Click(object s, RoutedEventArgs e) => Vm.ShowBookmarks = !Vm.ShowBookmarks;
    private void ToggleInsp_Click(object s, RoutedEventArgs e) => Vm.ShowInspector = !Vm.ShowInspector;
    private void Fullscreen_Click(object s, RoutedEventArgs e) => ToggleFullscreen();
    private void TabTelas_Click(object s, RoutedEventArgs e) { Vm.ShowInspector = true; Vm.Tab = "telas"; }
    private void TabVideo_Click(object s, RoutedEventArgs e) { Vm.ShowInspector = true; Vm.Tab = "video"; }
    private void ToggleNdi_Click(object s, RoutedEventArgs e) => ToggleNdi();

    public void ToggleLock()
    {
        if (Engine.Live || Engine.Countdown > 0) { Flash("Pause o prompt para editar"); return; }
        if (Voice.Active) { Flash("Desligue o acompanhamento por voz para editar"); return; }
        SetLocked(!Vm.Locked);
        if (!Vm.Locked) Editor.Focus();
        Vm.Follow = true;
    }

    private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Delete/Backspace junto a um marcador remove o bloco inteiro (como contenteditable=false).
        if (Vm.Locked) return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key is Key.Z or Key.Y)
        {
            e.Handled = true;
            if (e.Key == Key.Y || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) Redo(); else Undo();
            return;
        }
        if (e.Key is Key.Back or Key.Delete && Editor.Selection.IsEmpty)
        {
            var fd = Editor.Document;
            var blk = ScriptFlow.TopBlock(Editor.CaretPosition, fd);
            if (blk is Paragraph p)
            {
                var atStart = Editor.CaretPosition.GetOffsetToPosition(p.ContentStart) == 0;
                var atEnd = Editor.CaretPosition.GetOffsetToPosition(p.ContentEnd) == 0;
                var other = e.Key == Key.Back && atStart ? p.PreviousBlock : e.Key == Key.Delete && atEnd ? p.NextBlock : null;
                if (other is BlockUIContainer { Child: MarkChip }) { fd.Blocks.Remove(other); e.Handled = true; MarkDirty(); CommitEdits(); }
            }
        }
    }

    // ═══ Lista de marcadores: clique, duplo clique, botão direito, arrastar ═══
    private Point _bmDown; private BookmarkVM? _bmDrag;
    private void Bm_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: BookmarkVM b }) return;
        if (e.ClickCount == 2) { JumpBookmark(b.Index); e.Handled = true; return; }
        _bmDown = e.GetPosition(this); _bmDrag = b;
    }
    private void Bm_MouseMove(object sender, MouseEventArgs e)
    {
        if (_bmDrag == null || e.LeftButton != MouseButtonState.Pressed) return;
        if ((e.GetPosition(this) - _bmDown).Length < 6) return;
        var b = _bmDrag; _bmDrag = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("arca-bm", b.Index), DragDropEffects.Move);
        foreach (var x in Vm.Bookmarks) x.DropTarget = false;
    }
    private void Bm_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("arca-bm") || sender is not FrameworkElement { Tag: BookmarkVM b }) return;
        e.Effects = DragDropEffects.Move; e.Handled = true;
        foreach (var x in Vm.Bookmarks) x.DropTarget = x == b;
    }
    private void Bm_DragLeave(object sender, DragEventArgs e) { if (sender is FrameworkElement { Tag: BookmarkVM b }) b.DropTarget = false; }
    private void Bm_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("arca-bm") || sender is not FrameworkElement { Tag: BookmarkVM b }) return;
        var from = (int)e.Data.GetData("arca-bm");
        foreach (var x in Vm.Bookmarks) x.DropTarget = false;
        MoveBlock(from, b.Index);
    }
    private void Bm_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: BookmarkVM b }) return;
        var m = new ContextMenu();
        void Add(string h, Action a) { var mi = new MenuItem { Header = h }; mi.Click += (_, _) => a(); m.Items.Add(mi); }
        Add("Ir para o marcador", () => JumpBookmark(b.Index));
        Add("Renomear…", () => { var c = Chips(BlockKind.Bookmark).ElementAtOrDefault(b.Index)?.Child as MarkChip; if (c != null) RenameChip(c); });
        Add(b.Skip ? "Voltar o bloco ao roteiro" : "Pular este bloco", () => ToggleSkip(b.Index));
        m.Items.Add(new Separator());
        Add("Excluir marcador", () => RemoveBlock(BlockKind.Bookmark, b.Index));
        m.IsOpen = true;
        e.Handled = true;
    }
    private void BmSkip_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: BookmarkVM b }) ToggleSkip(b.Index); }
    private void BmRemove_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: BookmarkVM b }) RemoveBlock(BlockKind.Bookmark, b.Index); }
    private void Stop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not FrameworkElement { Tag: StopVM s }) return;
        var top = _measure.TopOf(s.Block); if (!double.IsNaN(top)) GoTo(top);
    }
    private void StopRemove_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: StopVM s }) RemoveBlock(BlockKind.Stop, s.Index); }
    private void Tab_Click(object sender, MouseButtonEventArgs e) { if (sender is FrameworkElement { Tag: DocTabVM t } && t != _active) Activate(t); }
    private void TabClose_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: DocTabVM t }) CloseTab(t); e.Handled = true; }

    private void Prog_LostFocus(object sender, RoutedEventArgs e) => CommitProgram();
    private void Prog_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { CommitProgram(); Keyboard.ClearFocus(); } }
    private void CommitProgram()
    {
        var sec = TextUtil.ParseDuration(Vm.ProgramInput) ?? Vm.S.ProgramSeconds;
        Vm.S.ProgramSeconds = sec; Vm.ProgramInput = TextUtil.FormatMmSs(sec);
        UpdateRundown();
    }

    // ═══ Seletor de apresentador (barra de ferramentas) ═══
    private void PresMenu_Click(object sender, RoutedEventArgs e)
    {
        PresList.Children.Clear();
        void Row(string name, string color, string key, int n)
        {
            var g = new Grid { Height = 30, Background = System.Windows.Media.Brushes.Transparent, Cursor = Cursors.Hand };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) }); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(new System.Windows.Shapes.Ellipse { Width = 10, Height = 10, Fill = ScriptFlow.ParseBrush(color, System.Windows.Media.Brushes.Transparent), Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(153, 128, 128, 128)), Margin = new Thickness(6, 0, 0, 0) });
            var t = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 16, 0) }; Grid.SetColumn(t, 1); g.Children.Add(t);
            var k = new TextBlock { Text = key, Foreground = (System.Windows.Media.Brush)FindResource("Faint"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) }; Grid.SetColumn(k, 2); g.Children.Add(k);
            // Botão de verdade: funciona com teclado e leitores de tela (antes era só uma área clicável).
            var bd = new Button { Style = (Style)FindResource("Ghost"), Content = g, Padding = new Thickness(0), Height = 32, HorizontalContentAlignment = HorizontalAlignment.Stretch, Focusable = false };
            System.Windows.Automation.AutomationProperties.SetName(bd, name);
            bd.Click += (_, _) => { PresPopup.IsOpen = false; SetWho(n); };
            PresList.Children.Add(bd);
        }
        for (var i = 0; i < Vm.Presenters.Count; i++) Row(string.IsNullOrWhiteSpace(Vm.Presenters[i].Name) ? "Apresentador " + (i + 1) : Vm.Presenters[i].Name, Vm.Presenters[i].Color, "Alt+" + (i + 1), i + 1);
        Row("Sem apresentador", "Transparent", "Alt+0", 0);
        PresPopup.IsOpen = true;
    }

    // ═══ Menus dinâmicos ═══
    private static MenuItem Mi(string header, Action click, string gesture = "", bool check = false) { var m = new MenuItem { Header = header, InputGestureText = gesture, IsChecked = check }; m.Click += (_, _) => click(); return m; }

    private void FormatMenu_Opened(object sender, RoutedEventArgs e)
    {
        FormatMenu.Items.Clear();
        for (var i = 0; i < Vm.Presenters.Count; i++) { var n = i + 1; FormatMenu.Items.Add(Mi(string.IsNullOrWhiteSpace(Vm.Presenters[i].Name) ? "Apresentador " + n : Vm.Presenters[i].Name, () => SetWho(n), "Alt+" + n, Vm.CurrentWho == n)); }
        FormatMenu.Items.Add(Mi("Sem apresentador", () => SetWho(0), "Alt+0", Vm.CurrentWho == 0));
        FormatMenu.Items.Add(new Separator());
        FormatMenu.Items.Add(Mi("Negrito", () => Exec(EditingCommands.ToggleBold), "Ctrl+B"));
        FormatMenu.Items.Add(Mi("Itálico", () => Exec(EditingCommands.ToggleItalic), "Ctrl+I"));
        FormatMenu.Items.Add(Mi("Sublinhado", () => Exec(EditingCommands.ToggleUnderline), "Ctrl+U"));
        FormatMenu.Items.Add(new Separator());
        FormatMenu.Items.Add(Mi("Converter para MAIÚSCULAS", UpperSelection));
        FormatMenu.Items.Add(new Separator());
        foreach (var (c, n) in new[] { ("▶", "Seta de ação"), ("●", "Ponto de destaque"), ("★", "Estrela"), ("⚠", "Atenção"), ("↓", "Seta para baixo"), ("✓", "Visto") })
            FormatMenu.Items.Add(Mi($"Inserir símbolo {c}  {n}", () => InsertSymbol(c)));
        FormatMenu.Items.Add(Mi("Mais símbolos e emojis (Win + .)", () => { Editor.Focus(); }));
        FormatMenu.Items.Add(new Separator());
        FormatMenu.Items.Add(Mi("Limpar formatação", () => SetColor(null)));
    }

    private void MarksMenu_Opened(object sender, RoutedEventArgs e)
    {
        MarksMenu.Items.Clear();
        MarksMenu.Items.Add(Mi("Inserir marcador", () => InsertBlock(BlockKind.Bookmark), "Ctrl+K"));
        MarksMenu.Items.Add(Mi("Inserir ponto de pausa", () => InsertBlock(BlockKind.Stop)));
        MarksMenu.Items.Add(Mi("Excluir marcador atual", () => { if (_cur >= 0) RemoveBlock(BlockKind.Bookmark, _cur); }));
        MarksMenu.Items.Add(Mi("Excluir todas as pausas", RemoveAllStops));
        MarksMenu.Items.Add(new Separator());
        for (var i = 0; i < Math.Min(9, Vm.Bookmarks.Count); i++) { var n = i; MarksMenu.Items.Add(Mi(Vm.Bookmarks[i].Name, () => JumpBookmark(n), "Ctrl+" + (i + 1), i == _cur)); }
        MarksMenu.Items.Add(new Separator());
        MarksMenu.Items.Add(Mi("Lista de marcadores", () => Vm.ShowBookmarks = !Vm.ShowBookmarks, "", Vm.ShowBookmarks));
    }

    private void PrompterMenu_Opened(object sender, RoutedEventArgs e)
    {
        var km = Vm.App.Keymap;
        PrompterMenu.Items.Clear();
        PrompterMenu.Items.Add(Mi(Engine.Live ? "Parar prompt" : "Iniciar prompt", TogglePlay, "Ctrl+Enter"));
        PrompterMenu.Items.Add(Mi("Voltar ao início", () => DoAction("rewind"), Keymap.Display(km.GetValueOrDefault("rewind", "Home"))));
        PrompterMenu.Items.Add(Mi("Parágrafo anterior", () => JumpParagraph(-1), Keymap.Display(km.GetValueOrDefault("prevPara", "["))));
        PrompterMenu.Items.Add(Mi("Próximo parágrafo", () => JumpParagraph(1), Keymap.Display(km.GetValueOrDefault("nextPara", "]"))));
        PrompterMenu.Items.Add(Mi("Prompter no cursor do editor", CursorToPrompter, "Ctrl+J"));
        PrompterMenu.Items.Add(new Separator());
        PrompterMenu.Items.Add(Mi("Janela do talento", ToggleTalent, "F5", TalentLayer.Visibility == Visibility.Visible));
        PrompterMenu.Items.Add(Mi("Editar trecho atual", EditHere, Keymap.Display(km.GetValueOrDefault("editHere", "E"))));
        PrompterMenu.Items.Add(new Separator());
        var d = Outputs.PreviewTarget;
        PrompterMenu.Items.Add(Mi("Espelhar horizontal · " + (d?.Nick ?? "talento"), () => { if (d != null) d.FlipH = !d.FlipH; else { _talentFlipH = !_talentFlipH; ApplyTalentFlip(); } }, "", d?.FlipH ?? _talentFlipH));
        PrompterMenu.Items.Add(Mi("Espelhar vertical · " + (d?.Nick ?? "talento"), () => { if (d != null) d.FlipV = !d.FlipV; else { _talentFlipV = !_talentFlipV; ApplyTalentFlip(); } }, "", d?.FlipV ?? _talentFlipV));
        PrompterMenu.Items.Add(new Separator());
        PrompterMenu.Items.Add(Mi("Acompanhamento por voz (microfone)", () => Voice.ToggleMic(), "", Vm.VoiceMode == "mic"));
        PrompterMenu.Items.Add(Mi("Simular acompanhamento por voz", () => Voice.ToggleSim(), "", Vm.VoiceMode == "sim"));
    }
}
