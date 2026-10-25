using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.Audio;
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Storage;
using Arca.Ndi;
using QRCoder;

namespace Arca.App;

/// <summary>Diálogos (camada sobre a janela, como no protótipo) e impressão.</summary>
public partial class MainWindow
{
    // ═══ Infraestrutura ═══
    private Action? _onDialogClose;
    private void ShowDialog(string title, double width, UIElement content, Action? onClose = null)
    {
        DialogTitle.Text = title; DialogCard.Width = width; DialogContent.Content = content;
        DialogLayer.Visibility = Visibility.Visible; _onDialogClose = onClose;
    }
    public void CloseDialog()
    {
        DialogLayer.Visibility = Visibility.Collapsed; DialogContent.Content = null; CaptureAction = null;
        var c = _onDialogClose; _onDialogClose = null; c?.Invoke();
        Focus();
    }
    private void DialogClose_Click(object s, RoutedEventArgs e) => CloseDialog();
    private void DialogLayer_MouseDown(object s, MouseButtonEventArgs e) { if (e.OriginalSource == DialogLayer) CloseDialog(); }
    private void DialogCard_MouseDown(object s, MouseButtonEventArgs e) { }

    private Style St(string k) => (Style)FindResource(k);
    private Brush Br(string k) => (Brush)FindResource(k);
    private TextBlock Eyebrow(string t, Thickness? m = null) => new() { Text = t.ToUpperInvariant(), Style = St("Eyebrow"), Margin = m ?? new Thickness(0, 0, 0, 8) };
    private TextBlock Muted(string t, double size = 12) => new() { Text = t, Foreground = Br("Muted"), FontSize = size, TextWrapping = TextWrapping.Wrap };
    /// <summary>Controle deslizante com o valor escrito ao lado.</summary>
    private UIElement SliderRow(double min, double max, double tick, double value, Action<double> set, Func<double, string> label)
    {
        var g = new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        var sl = new Slider { Minimum = min, Maximum = max, TickFrequency = tick, IsSnapToTickEnabled = true, Value = Math.Clamp(value, min, max), Margin = new Thickness(0, 0, 10, 0) };
        var t = new TextBlock { Text = label(sl.Value), Foreground = Br("Muted"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(t, 1);
        sl.ValueChanged += (_, e) => { set(e.NewValue); t.Text = label(e.NewValue); };
        g.Children.Add(sl); g.Children.Add(t);
        return g;
    }

    private Button Btn(string t, Action a, string style = "Pill") { var b = new Button { Content = t, Style = St(style), Focusable = false }; b.Click += (_, _) => a(); return b; }
    private CheckBox Check(string label, bool on, Action<bool> set) { var c = new CheckBox { Content = label, IsChecked = on, Margin = new Thickness(0, 0, 0, 10) }; c.Click += (_, _) => set(c.IsChecked == true); return c; }
    private static Grid Form(double labelW = 110)
    {
        var g = new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelW) }); g.ColumnDefinitions.Add(new ColumnDefinition());
        return g;
    }
    private void Row(Grid g, string label, UIElement c)
    {
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var r = g.RowDefinitions.Count - 1;
        var l = new TextBlock { Text = label, Foreground = Br("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 8, 4) };
        Grid.SetRow(l, r); g.Children.Add(l);
        if (c is FrameworkElement fe) fe.Margin = new Thickness(0, 4, 0, 4);
        Grid.SetRow(c, r); Grid.SetColumn(c, 1); g.Children.Add(c);
    }
    private TextBox Field(string value, Action<string> set, double? width = null)
    {
        var t = new TextBox { Style = St("Input"), Text = value };
        if (width != null) { t.Width = width.Value; t.HorizontalAlignment = HorizontalAlignment.Left; }
        t.TextChanged += (_, _) => set(t.Text);
        return t;
    }
    private static Border Pad(UIElement c, double p = 16) => new() { Padding = new Thickness(p), Child = c };

    /// <summary>Caixa de texto modal (renomear marcador etc.).</summary>
    public string? Prompt(string title, string value) => ArcaDialog.Input(this, "ARCA Teleprompter", title, value);

    // ═══ Localizar e substituir ═══
    private string _findQ = "", _replQ = ""; private int _findI = -1;
    public void ShowFind()
    {
        var q = Field(_findQ, v => { _findQ = v; _findI = -1; });
        var r = Field(_replQ, v => _replQ = v);
        var info = Muted("");
        q.KeyDown += (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; FindNext(info); } };
        var g = Form(100); Row(g, "Localizar", q); Row(g, "Substituir por", r);
        var bar = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        var next = Btn("Próximo", () => FindNext(info), "Primary"); DockPanel.SetDock(next, Dock.Right);
        var all = Btn("Substituir tudo", () => ReplaceAll(info)); all.Margin = new Thickness(0, 0, 8, 0); DockPanel.SetDock(all, Dock.Right);
        bar.Children.Add(next); bar.Children.Add(all); bar.Children.Add(info);
        var sp = new StackPanel(); sp.Children.Add(g); sp.Children.Add(bar);
        ShowDialog("Localizar e substituir", 460, Pad(sp));
        Dispatcher.BeginInvoke(() => q.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void FindNext(TextBlock info)
    {
        if (_findQ.Length == 0) return;
        var hits = new List<TextRange>();
        var fd = Editor.Document;
        for (var p = fd.ContentStart; p != null && p.CompareTo(fd.ContentEnd) < 0; p = p.GetNextContextPosition(LogicalDirection.Forward))
        {
            if (p.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text) continue;
            var run = p.GetTextInRun(LogicalDirection.Forward);
            var i = run.IndexOf(_findQ, StringComparison.CurrentCultureIgnoreCase);
            while (i >= 0) { hits.Add(new TextRange(p.GetPositionAtOffset(i)!, p.GetPositionAtOffset(i + _findQ.Length)!)); i = run.IndexOf(_findQ, i + _findQ.Length, StringComparison.CurrentCultureIgnoreCase); }
        }
        if (hits.Count == 0) { info.Text = "Nenhuma ocorrência"; return; }
        _findI = (_findI + 1) % hits.Count;
        var h = hits[_findI];
        Editor.Selection.Select(h.Start, h.End);
        var blk = ScriptFlow.TopBlock(h.Start, fd);
        if (blk != null && !Engine.Live) GoTo(_measure.YOf(ScriptFlow.IndexOf(fd, blk), 0), textLine: true);
        info.Text = $"{_findI + 1} de {hits.Count}";
    }

    private void ReplaceAll(TextBlock info)
    {
        if (_findQ.Length == 0 || Vm.Locked) return;
        var doc = ScriptFlow.Read(Editor.Document); var n = 0;
        foreach (var b in doc.Blocks)
            for (var i = 0; i < b.Runs.Count; i++)
            {
                var t = b.Runs[i].Text; var c = 0; var idx = t.IndexOf(_findQ, StringComparison.CurrentCultureIgnoreCase);
                if (idx < 0) continue;
                var sb = new System.Text.StringBuilder(); var last = 0;
                while (idx >= 0) { sb.Append(t, last, idx - last).Append(_replQ); last = idx + _findQ.Length; c++; idx = t.IndexOf(_findQ, last, StringComparison.CurrentCultureIgnoreCase); }
                sb.Append(t, last, t.Length - last);
                b.Runs[i] = b.Runs[i] with { Text = sb.ToString() }; n += c;
            }
        if (n == 0) { info.Text = "Nenhuma ocorrência"; return; }
        var anchor = _measure.AnchorAt(Engine.Y);
        using (SuppressEdits()) { var fd = ScriptFlow.BuildEditor(doc, Vm.S, _layout, _font); fd.PagePadding = Editor.Document.PagePadding; Editor.Document = fd; }
        Doc = doc; MarkDirty(); RebuildOutputs(anchor); UpdateRundown();
        info.Text = $"{n} substituída(s)";
    }

    // ═══ Preferências ═══
    public void ShowPrefs(string tab)
    {
        var app = Vm.App;
        var content = new ContentControl();
        var nav = new StackPanel { Margin = new Thickness(10) };
        var pages = new Dictionary<string, Func<UIElement>>
        {
            ["geral"] = () =>
            {
                var sp = new StackPanel();
                var themes = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (var (v, l) in new[] { ("dark", "Escuro"), ("light", "Claro") })
                {
                    var rb = new RadioButton { Style = St("Seg"), Content = l, GroupName = "theme", IsChecked = Vm.Theme == v, Padding = new Thickness(16, 0, 16, 0) };
                    rb.Checked += (_, _) => ApplyTheme(v);
                    themes.Children.Add(rb);
                }
                var g = Form(); Row(g, "Aparência", new Border { Style = St("SegBox"), HorizontalAlignment = HorizontalAlignment.Left, Child = themes });
                sp.Children.Add(g);
                sp.Children.Add(new Border { Height = 1, Background = Br("Line"), Margin = new Thickness(0, 12, 0, 14) });
                sp.Children.Add(Check("Bloquear o editor ao iniciar o prompt", app.LockOnPlay, v => app.LockOnPlay = v));
                sp.Children.Add(Check("Procurar atualizações ao abrir o app", app.UpdateCheck, v => app.UpdateCheck = v));
                sp.Children.Add(Check("Contagem regressiva de 3 s antes de rolar", app.Countdown, v => app.Countdown = v));
                sp.Children.Add(Check("Gravar vídeo junto com o prompt", app.RecordWithPrompt, v => app.RecordWithPrompt = v));
                sp.Children.Add(Check("Informações (bloco, tempo, relógio) na tela do talento", Vm.TalentHud, v => Vm.TalentHud = v));
                sp.Children.Add(Check("Atalhos globais: clicker e pedal funcionam com o app em segundo plano", app.GlobalHotkeys, v => { app.GlobalHotkeys = v; RegisterHotkeys(); }));
                sp.Children.Add(Muted("Os atalhos globais usam as teclas de Play, próximo/anterior marcador e mais rápido/devagar. Enquanto ligados, essas teclas deixam de funcionar em outros programas.", 11.5));
                sp.Children.Add(new Border { Height = 1, Background = Br("Line"), Margin = new Thickness(0, 14, 0, 14) });
                // Mouse e trackpad: sensibilidade da roda (rolagem parada e velocidade rodando) e sentido.
                sp.Children.Add(Eyebrow("Mouse e trackpad"));
                var mg = Form();
                Row(mg, "Rolagem (parado)", SliderRow(0.25, 3, 0.25, app.WheelLines, v => app.WheelLines = v, v => v.ToString("0.##") + (v == 1 ? " linha" : " linhas") + " por giro"));
                Row(mg, "Velocidade (rodando)", SliderRow(0.005, 0.06, 0.005, app.WheelSpeedStep, v => app.WheelSpeedStep = v, v => (v * 100).ToString("0.#") + "% por giro"));
                sp.Children.Add(mg);
                sp.Children.Add(Check("Inverter o sentido da roda (rolar para cima = mais rápido / avançar)", app.WheelInvert, v => app.WheelInvert = v));
                sp.Children.Add(Muted("No trackpad o deslizar é proporcional: movimentos curtos mudam pouco. Ctrl + roda muda as palavras por linha.", 11.5));
                sp.Children.Add(new Border { Height = 1, Background = Br("Line"), Margin = new Thickness(0, 14, 0, 14) });
                var data = new StackPanel { Orientation = Orientation.Horizontal };
                data.Children.Add(Btn("Abrir pasta de dados", () => OpenFolder(AppPaths.Data)));
                var logs = Btn("Abrir registros", () => OpenFolder(AppPaths.Logs)); logs.Margin = new Thickness(8, 0, 0, 0); data.Children.Add(logs);
                sp.Children.Add(data);
                return sp;
            },
            ["atalhos"] = () =>
            {
                var sp = new StackPanel();
                var rows = new List<(string Id, Button B)>();
                foreach (var a in Keymap.Actions)
                {
                    var dp = new DockPanel { Height = 36 };
                    var b = new Button { Style = St("Pill"), MinWidth = 120, Height = 26, FontFamily = (FontFamily)FindResource("FNum"), FontSize = 12, Content = Keymap.Display(app.Keymap.GetValueOrDefault(a.Id, a.DefaultKey)), ToolTip = "Clique e pressione a nova tecla", Focusable = false };
                    var id = a.Id;
                    b.Click += (_, _) => { CaptureAction = id; foreach (var (_, x) in rows) x.Background = Br("Soft"); b.Content = "Pressione uma tecla…"; b.Background = Br("Sunk"); };
                    DockPanel.SetDock(b, Dock.Right); dp.Children.Add(b);
                    dp.Children.Add(new TextBlock { Text = a.Label, VerticalAlignment = VerticalAlignment.Center });
                    sp.Children.Add(new Border { BorderBrush = Br("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Child = dp });
                    rows.Add((id, b));
                }
                void Refresh(string _) { foreach (var (id, b) in rows) { b.Content = Keymap.Display(app.Keymap.GetValueOrDefault(id, "")); b.Background = Br("Soft"); } RegisterHotkeys(); }
                KeyCaptured += Refresh;
                sp.Unloaded += (_, _) => KeyCaptured -= Refresh;
                sp.Children.Add(new TextBlock { Text = "Shift + tecla = passo maior · 1–9 = velocidade predefinida · Backspace = início · Roda do mouse: rola (pausado) ou ajusta a velocidade (rodando) · Ctrl + roda = palavras por linha · Arrastar na tela do talento = rolar · Ctrl+1…9 = ir para o marcador · Alt+1…8 = apresentador", Foreground = Br("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0), LineHeight = 19 });
                var reset = Btn("Restaurar padrão", () => { app.Keymap = Keymap.Defaults(); Refresh(""); }); reset.HorizontalAlignment = HorizontalAlignment.Right; reset.Margin = new Thickness(0, 12, 0, 0);
                sp.Children.Add(reset);
                return sp;
            },
            ["disp"] = () =>
            {
                var sp = new StackPanel();
                void Item(string n, string st) { var dp = new DockPanel { Height = 36 }; var t = new TextBlock { Text = st, Foreground = Br("Muted"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center }; DockPanel.SetDock(t, Dock.Right); dp.Children.Add(t); dp.Children.Add(new TextBlock { Text = n, VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new Border { BorderBrush = Br("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Child = dp }); }
                Item("Teclado", "Conectado");
                Item("Mouse e roda de rolagem", "Conectado");
                Item("Clicker de apresentação", "Usa Page Up / Page Down");
                Item("Controle pelo navegador / iPad", Web.Running ? (app.WebControl ? $"Ativo · {Vm.WebViewers} conectado(s)" : "Somente visualização") : "Monitor web desligado");
                Item("NDI Runtime", NdiSender.IsAvailable ? "Versão " + (NdiSender.RuntimeVersion?.Split(' ').LastOrDefault() ?? "?") : "Não instalado (ndi.video/tools)");
                Item("Drivers ASIO", string.Join(", ", AudioInput.ListAsio().Select(a => a.Name)) is { Length: > 0 } s ? s : "Nenhum");
                Item("Microfones (WASAPI)", AudioInput.ListWasapi().Count + " dispositivo(s)");
                Item("Monitores", Vm.Displays.Count(d => !d.Windowed) + " conectado(s)");
                var test = new Border { Margin = new Thickness(0, 16, 0, 0), Padding = new Thickness(14), CornerRadius = new CornerRadius(8), Background = Br("Field"), BorderBrush = Br("Line"), BorderThickness = new Thickness(1) };
                var dp2 = new DockPanel();
                var last = new TextBlock { FontFamily = (FontFamily)FindResource("FNum"), FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
                last.SetBinding(TextBlock.TextProperty, new Binding(nameof(Vm.LastKey)) { Source = Vm });
                DockPanel.SetDock(last, Dock.Right); dp2.Children.Add(last);
                dp2.Children.Add(Muted("Teste de entrada — pressione qualquer botão do controle", 13));
                test.Child = dp2; sp.Children.Add(test);
                return sp;
            },
            ["integ"] = () =>
            {
                var sp = new StackPanel();
                sp.Children.Add(Eyebrow("Newsroom (MOS)"));
                var sys = new ComboBox { ItemsSource = new[] { "ENPS", "Avid iNEWS", "Octopus", "Dalet", "Ross Inception" }, SelectedItem = app.MosSystem };
                sys.SelectionChanged += (_, _) => app.MosSystem = (string)sys.SelectedItem;
                var g = Form(); Row(g, "Sistema", sys); Row(g, "Servidor (NCS)", Field(app.MosHost, v => app.MosHost = v)); Row(g, "ID MOS", Field(app.MosId, v => app.MosId = v));
                sp.Children.Add(g);
                var st = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
                var dot = new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                var stt = Muted("");
                void Upd() { dot.Fill = Br(Vm.MosOn ? "Ok" : "Faint"); stt.Text = Vm.MosStatus; }
                var connect = Btn(Vm.MosOn ? "Desconectar" : "Ouvir o NCS", () => { });
                connect.Click += (_, _) => { if (Mos.Running) { Mos.Stop(); app.MosListen = false; } else { app.MosListen = Mos.Start(); } connect.Content = Vm.MosOn ? "Desconectar" : "Ouvir o NCS"; Upd(); };
                DockPanel.SetDock(connect, Dock.Right); st.Children.Add(connect); st.Children.Add(dot); st.Children.Add(stt);
                Upd(); Vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Vm.MosStatus)) Upd(); };
                sp.Children.Add(st);
                sp.Children.Add(Muted("O ARCA atua como dispositivo MOS de prompter (MOS 2.8): o sistema de redação conecta nas portas 10540/10541 deste computador e envia o espelho. Cada matéria vira um marcador com o texto de leitura.", 11.5));
                sp.Children.Add(new Border { Height = 1, Background = Br("Line"), Margin = new Thickness(0, 14, 0, 14) });
                sp.Children.Add(Eyebrow("Estúdio · legenda (closed caption)"));
                sp.Children.Add(Check("Enviar o texto lido como legenda", app.CaptionOut, v => { app.CaptionOut = v; Caption.Reset(); }));
                var mode = new ComboBox { SelectedValuePath = "Tag" };
                mode.Items.Add(new ComboBoxItem { Content = "vMix (título via API HTTP)", Tag = "vmix" }); mode.Items.Add(new ComboBoxItem { Content = "TCP (uma linha por mensagem)", Tag = "tcp" });
                mode.SelectedValue = app.CaptionMode; mode.SelectionChanged += (_, _) => { app.CaptionMode = (string)mode.SelectedValue; Caption.Reset(); };
                var g2 = Form(); Row(g2, "Destino", mode); Row(g2, "Endereço", Field(app.CaptionHost, v => app.CaptionHost = v));
                Row(g2, "Porta", Field(app.CaptionPort.ToString(), v => { if (int.TryParse(v, out var p)) app.CaptionPort = p; }, 90));
                Row(g2, "Título (vMix)", Field(app.CaptionInput, v => app.CaptionInput = v)); Row(g2, "Campo (vMix)", Field(app.CaptionField, v => app.CaptionField = v));
                sp.Children.Add(g2);
                var cs = Muted(Vm.CaptionStatus); Vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Vm.CaptionStatus)) cs.Text = Vm.CaptionStatus; };
                cs.Margin = new Thickness(0, 6, 0, 0); sp.Children.Add(cs);
                return sp;
            }
        };
        var names = new[] { ("geral", "Geral"), ("atalhos", "Atalhos"), ("disp", "Dispositivos"), ("integ", "Integrações") };
        var buttons = new List<RadioButton>();
        foreach (var (id, l) in names)
        {
            var rb = new RadioButton { Style = St("Seg"), Content = l, GroupName = "prefs", HorizontalContentAlignment = HorizontalAlignment.Left, Height = 34, Margin = new Thickness(0, 0, 0, 2), IsChecked = id == tab };
            rb.Checked += (_, _) => { CaptureAction = null; content.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 560, Content = Pad(pages[id](), 20) }; };
            nav.Children.Add(rb); buttons.Add(rb);
        }
        content.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 560, Content = Pad(pages[tab](), 20) };
        var grid = new Grid { MinHeight = 400 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); grid.ColumnDefinitions.Add(new ColumnDefinition());
        var navB = new Border { BorderBrush = Br("Line"), BorderThickness = new Thickness(0, 0, 1, 0), Child = nav };
        grid.Children.Add(navB); Grid.SetColumn(content, 1); grid.Children.Add(content);
        ShowDialog("Preferências", 700, grid, () => Stores.SaveSettings(app));
    }

    private static void OpenFolder(string path) { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true }); }

    // ═══ Versões ═══
    private void Versions_Click(object s, RoutedEventArgs e)
    {
        _versions = Stores.LoadVersions();
        var sp = new StackPanel();
        if (_versions.Count == 0) sp.Children.Add(new Border { Padding = new Thickness(12, 24, 12, 24), Child = Muted("Nenhuma versão ainda. Versões são criadas ao salvar, a cada 2 minutos de edição e ao fechar uma aba sem salvar.", 13) });
        foreach (var v in _versions)
        {
            var dp = new DockPanel { Margin = new Thickness(10, 8, 10, 8) };
            var b = Btn("Restaurar", () => { OpenInTab(Path.GetFileNameWithoutExtension(v.Name) + " (restaurado).tps", new ScriptDocument { Blocks = v.Blocks }, null); CloseDialog(); });
            b.Height = 28; b.FontSize = 12; DockPanel.SetDock(b, Dock.Right); dp.Children.Add(b);
            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = v.Name, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            info.Children.Add(new TextBlock { Text = $"{v.Time:dd/MM/yyyy HH:mm} · {v.Kind} · {v.Words} palavras", FontFamily = (FontFamily)FindResource("FNum"), FontSize = 11, Foreground = Br("Muted") });
            dp.Children.Add(info);
            sp.Children.Add(dp);
        }
        ShowDialog("Histórico de versões", 480, new ScrollViewer { MaxHeight = 420, Content = Pad(sp, 6) });
    }

    // ═══ Perfis de apresentador ═══
    private void Profiles_Click(object s, RoutedEventArgs e) => ShowProfiles();
    private void ShowProfiles()
    {
        var profiles = Stores.LoadProfiles();
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "Um perfil guarda fonte, palavras por linha, margem, entrelinha, cores, alinhamento, indicador e velocidade de cada apresentador.", Foreground = Br("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        var name = new TextBox { Style = St("Input") };
        var save = Btn("Salvar atual", () =>
        {
            var n = name.Text.Trim().Length > 0 ? name.Text.Trim() : "Perfil " + (profiles.Count + 1);
            profiles.RemoveAll(p => p.Name == n);
            profiles.Insert(0, new Profile { Name = n, Time = DateTime.Now, Settings = Vm.S.Clone() });
            Stores.SaveProfiles(profiles); Flash("Perfil salvo"); ShowProfiles();
        }, "Primary");
        save.Margin = new Thickness(6, 0, 0, 0);
        var top = new DockPanel(); DockPanel.SetDock(save, Dock.Right); top.Children.Add(save); top.Children.Add(name);
        sp.Children.Add(top);
        sp.Children.Add(new Border { Height = 1, Background = Br("Line"), Margin = new Thickness(0, 10, 0, 6) });
        if (profiles.Count == 0) sp.Children.Add(new TextBlock { Text = "Nenhum perfil salvo.", Foreground = Br("Muted"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 14) });
        foreach (var p in profiles)
        {
            var dp = new DockPanel { Margin = new Thickness(8, 7, 8, 7) };
            var del = new Button { Style = St("TinyBtn"), Width = 26, Height = 26, Content = new Icon { Data = Icons.X, Size = 10, StrokeWidth = 1.7 }, ToolTip = "Excluir perfil", Focusable = false };
            del.Click += (_, _) => { profiles.Remove(p); Stores.SaveProfiles(profiles); ShowProfiles(); };
            var apply = Btn("Aplicar", () =>
            {
                var keepPres = Vm.S.Presenters.ToList(); Vm.S.CopyFrom(p.Settings); Vm.LoadFromSettings(); ApplyLook(true);
                CloseDialog(); Flash($"Perfil \"{p.Name}\" aplicado");
            });
            apply.Height = 28; apply.FontSize = 12; apply.Margin = new Thickness(6, 0, 4, 0);
            DockPanel.SetDock(del, Dock.Right); DockPanel.SetDock(apply, Dock.Right);
            dp.Children.Add(del); dp.Children.Add(apply);
            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = p.Name, FontWeight = FontWeights.SemiBold });
            info.Children.Add(new TextBlock { Text = $"{FontCatalog.Label(p.Settings.FontKey)} · {p.Settings.WordsPerLine:0.##} palavras/linha · margem {p.Settings.Margin}", FontFamily = (FontFamily)FindResource("FNum"), FontSize = 11, Foreground = Br("Muted") });
            dp.Children.Add(info);
            sp.Children.Add(dp);
        }
        ShowDialog("Perfis de apresentador", 480, Pad(sp));
    }

    // ═══ Monitor web e iPad (QR code) ═══
    private void Monitor_Click(object s, RoutedEventArgs e) => ShowMonitorDialog();
    public async void ShowMonitorDialog()
    {
        if (!Web.Running && !await Web.Start()) { ShowDialog("Monitor web", 460, Pad(Muted(Vm.WebStatus, 13))); return; }
        var sp = new StackPanel();
        var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var prev = new PrompterSurface { Height = 230, ShowFlashAndCount = false };
        var box = new Border { CornerRadius = new CornerRadius(11), ClipToBounds = true, Child = prev, Margin = new Thickness(0, 0, 16, 0) };
        grid.Children.Add(box);
        var qrPanel = new StackPanel { Width = 200 };
        qrPanel.Children.Add(new Image { Source = Qr(Vm.WebUrl), Width = 180, Height = 180, HorizontalAlignment = HorizontalAlignment.Center });
        qrPanel.Children.Add(new TextBlock { Text = "Aponte a câmera do iPad", FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 2) });
        qrPanel.Children.Add(new TextBlock { Text = "Depois: Compartilhar → Adicionar à Tela de Início", Foreground = Br("Muted"), FontSize = 11, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
        Grid.SetColumn(qrPanel, 1); grid.Children.Add(qrPanel);
        sp.Children.Add(grid);
        var urls = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        void Url(string label, string url)
        {
            var dp = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var open = Btn("Abrir", () => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })); open.Height = 28; DockPanel.SetDock(open, Dock.Right);
            var copy = Btn("Copiar", () => { try { Clipboard.SetText(url); Flash("Endereço copiado"); } catch { } }); copy.Height = 28; copy.Margin = new Thickness(6, 0, 6, 0); DockPanel.SetDock(copy, Dock.Right);
            dp.Children.Add(open); dp.Children.Add(copy);
            dp.Children.Add(new TextBlock { Text = label, Width = 70, Foreground = Br("Muted"), VerticalAlignment = VerticalAlignment.Center });
            dp.Children.Add(new TextBox { Style = St("Input"), IsReadOnly = true, Text = url, FontFamily = (FontFamily)FindResource("FNum"), FontSize = 12 });
            urls.Children.Add(dp);
        }
        Url("Rede", Vm.WebUrl);
        Url("Seguro", Vm.WebUrlHttps);
        sp.Children.Add(urls);
        sp.Children.Add(Muted("O endereço “Seguro” (https) permite instalar o app no iPad com modo offline. Na primeira vez, baixe e confie no certificado: abra " + Vm.WebUrl + "certificado.cer no iPad → Ajustes › Geral › VPN e Gerenciamento de Dispositivos → instalar; depois Ajustes › Geral › Sobre › Ajustes de Confiança de Certificados.", 11.5));
        var ctl = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        var play = Btn(Engine.Live ? "Pausar" : "Play", TogglePlay, "Primary"); DockPanel.SetDock(play, Dock.Right);
        ctl.Children.Add(play);
        ctl.Children.Add(Check("Permitir controlar o prompter pelo navegador / iPad", Vm.App.WebControl, v => { Vm.App.WebControl = v; Web.UpdateOptions(); }));
        sp.Children.Add(ctl);
        Rebuild(prev);
        EventHandler? tick = null;
        tick = (_, _) => { prev.Y = Engine.Y; play.Content = Engine.Live ? "Pausar" : "Play"; };
        CompositionTarget.Rendering += tick;
        ShowDialog("Monitor web e iPad", 820, Pad(sp), () => CompositionTarget.Rendering -= tick);
    }

    private static BitmapImage? Qr(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(8);
        var b = new BitmapImage(); b.BeginInit(); b.StreamSource = new MemoryStream(png); b.CacheOption = BitmapCacheOption.OnLoad; b.EndInit(); b.Freeze();
        return b;
    }

    // ═══ Controle remoto (na própria janela) ═══
    private void Remote_Click(object s, RoutedEventArgs e)
    {
        var sp = new StackPanel();
        sp.Children.Add(Muted(Web.Running ? (Vm.App.WebControl ? "Celular/tablet: abra " + Vm.WebUrl + " (mesma rede)" : "Ative “Permitir controlar pelo navegador” para usar o celular") : "Ligue o Monitor web para controlar pelo celular ou iPad", 12));
        var info = new Border { Margin = new Thickness(0, 14, 0, 14), Padding = new Thickness(14), CornerRadius = new CornerRadius(10), Background = Br("Field"), BorderBrush = Br("Line"), BorderThickness = new Thickness(1) };
        var cur = new TextBlock { FontWeight = FontWeights.SemiBold, FontSize = 15 }; var pos = new TextBlock { FontFamily = (FontFamily)FindResource("FNum"), FontSize = 12, Foreground = Br("Muted") };
        var isp = new StackPanel(); isp.Children.Add(Muted("No prompter")); isp.Children.Add(cur); isp.Children.Add(pos); info.Child = isp;
        sp.Children.Add(info);
        var play = new Button { Style = St("Primary"), Height = 64, Content = new Icon { Data = Icons.Play, Size = 22, Filled = true }, Focusable = false };
        play.Click += (_, _) => TogglePlay(); sp.Children.Add(play);
        var g = new UniformGrid { Columns = 2, Margin = new Thickness(0, 8, 0, 8) };
        Button B(string t, Action a, double h = 48) { var b = Btn(t, a); b.Height = h; b.Margin = new Thickness(4); b.FontSize = t.Length == 1 ? 18 : 13; return b; }
        g.Children.Add(B("−", () => AdjustSpeed(-0.05))); g.Children.Add(B("+", () => AdjustSpeed(0.05)));
        g.Children.Add(B("Marcador anterior", () => DoAction("prevBm"), 44)); g.Children.Add(B("Próximo marcador", () => DoAction("nextBm"), 44));
        sp.Children.Add(g);
        sp.Children.Add(Btn("Voltar ao início", () => DoAction("rewind")));
        EventHandler? tick = null;
        tick = (_, _) =>
        {
            cur.Text = _cur >= 0 && _cur < Vm.Bookmarks.Count ? Vm.Bookmarks[_cur].Name : "—";
            pos.Text = $"{Vm.Readouts[0].Value} · {Vm.SpeedValue} {Vm.UnitName}";
            ((Icon)play.Content).Data = Engine.Live ? Icons.Pause : Icons.Play;
        };
        CompositionTarget.Rendering += tick;
        ShowDialog("Controle remoto", 360, Pad(sp, 18), () => CompositionTarget.Rendering -= tick);
    }

    // ═══ Registro, ajuda, sobre ═══
    private void Log_Click(object s, RoutedEventArgs e)
    {
        var f = Path.Combine(AppPaths.Logs, $"arca-{DateTime.Now:yyyy-MM-dd}.log");
        string text;
        try { using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var r = new StreamReader(fs); text = string.Join("\n", r.ReadToEnd().Split('\n').TakeLast(300)); }
        catch { text = "Sem registros hoje."; }
        var tb = new TextBox { Text = text, IsReadOnly = true, FontFamily = new FontFamily("Consolas"), FontSize = 11.5, Background = Br("Field"), Foreground = Br("Text"), BorderThickness = new Thickness(0), TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 420, Padding = new Thickness(8) };
        tb.Loaded += (_, _) => tb.ScrollToEnd();
        var sp = new StackPanel(); sp.Children.Add(tb);
        var open = Btn("Abrir pasta de registros", () => OpenFolder(AppPaths.Logs)); open.HorizontalAlignment = HorizontalAlignment.Right; open.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(open);
        ShowDialog("Registro de eventos", 760, Pad(sp));
    }
    private void Shortcuts_Click(object s, RoutedEventArgs e) => ShowPrefs("atalhos");
    private void Prefs_Click(object s, RoutedEventArgs e) => ShowPrefs("geral");
    private void Manual_Click(object s, RoutedEventArgs e)
    {
        var f = Path.Combine(AppContext.BaseDirectory, "docs", "Manual do Operador.html");
        if (File.Exists(f)) Process.Start(new ProcessStartInfo(f) { UseShellExecute = true });
        else Flash("Manual não encontrado na pasta do programa");
    }
    private void About_Click(object s, RoutedEventArgs e)
    {
        var sp = new StackPanel();
        var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        head.Children.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Assets/icon-64.png")), Width = 48, Height = 48, Margin = new Thickness(0, 0, 14, 0) });
        var t = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        t.Children.Add(new TextBlock { Text = "ARCA Teleprompter", FontFamily = (FontFamily)FindResource("FNum"), FontWeight = FontWeights.SemiBold, FontSize = 18 });
        t.Children.Add(Muted($"Versão {typeof(MainWindow).Assembly.GetName().Version?.ToString(3)} · Windows · .NET {Environment.Version.ToString(2)}", 13));
        head.Children.Add(t); sp.Children.Add(head);
        sp.Children.Add(Muted("Editor e prompter sincronizados, marcadores e rundown, várias telas com espelho, saída NDI, monitor web e iPad, acompanhamento por voz offline (WASAPI/ASIO) e integração MOS com a redação.", 12));
        sp.Children.Add(new TextBlock { Margin = new Thickness(0, 12, 0, 0), Foreground = Br("Faint"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Text = "Componentes: Arca.Core, Arca.Import, Arca.Audio, Arca.Ndi, Arca.Server, Arca.Mos. Bibliotecas de terceiros: NAudio (MIT), Vosk (Apache 2.0), DocumentFormat.OpenXml (MIT), PdfPig (Apache 2.0), CommunityToolkit.Mvvm (MIT), QRCoder (MIT), ASP.NET Core (MIT). NDI® é marca registrada da Vizrt NDI AB. Fontes sob SIL Open Font License." });
        ShowDialog("Sobre", 440, Pad(sp, 22));
    }

    // ═══ Impressão / PDF ═══

    /// <summary>Roteiro formatado com blocos e tempos, apresentadores, pausas e blocos pulados riscados.</summary>
    public void PrintScript(bool pdf)
    {
        if (!Allow("imprimir")) return;
        SnapshotActive();
        var s = Vm.S; var rd = Doc.Rundown();
        var fd = new FlowDocument { FontFamily = new FontFamily("Georgia"), FontSize = 12 * 96 / 72.0, PagePadding = new Thickness(72), ColumnWidth = 9999, Foreground = Brushes.Black, Background = Brushes.White, LineHeight = 12 * 96 / 72.0 * 1.55 };
        var total = Rundown.TotalSeconds(rd, s);
        var head = new Paragraph { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(0, 0, 0, 8), Margin = new Thickness(0, 0, 0, 20) };
        head.Inlines.Add(new Run(Path.GetFileNameWithoutExtension(CurrentDocName())) { FontFamily = new FontFamily("Segoe UI"), FontSize = 24, FontWeight = FontWeights.SemiBold });
        head.Inlines.Add(new LineBreak());
        head.Inlines.Add(new Run($"{Doc.WordCount} palavras · estimado {TextUtil.FormatMmSs(total)} · programa {TextUtil.FormatMmSs(s.ProgramSeconds)}") { FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Foreground = Brushes.DimGray });
        fd.Blocks.Add(head);
        var skipped = Doc.SkippedMask(); var starts = Doc.WhoStartMask(); var bi = -1;
        for (var i = 0; i < Doc.Blocks.Count; i++)
        {
            var b = Doc.Blocks[i];
            if (b.Kind == BlockKind.Bookmark)
            {
                bi++; var rb = rd[bi];
                var p = new Paragraph { FontFamily = new FontFamily("Segoe UI"), FontSize = 13, FontWeight = FontWeights.SemiBold, BorderBrush = Brushes.Silver, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 10, 0, 0), Margin = new Thickness(0, 26, 0, 10) };
                p.Inlines.Add(new Run(b.PlainText.ToUpperInvariant() + (rb.Skip ? " — PULADO" : "")));
                p.Inlines.Add(new Run("   " + TextUtil.FormatMmSs(Rundown.BlockSeconds(rb, s))) { Foreground = Brushes.DimGray, FontWeight = FontWeights.Normal });
                if (rb.Skip) { p.TextDecorations = TextDecorations.Strikethrough; p.Foreground = Brushes.Gray; }
                fd.Blocks.Add(p); continue;
            }
            if (b.Kind == BlockKind.Stop) { fd.Blocks.Add(new Paragraph(new Run("PAUSA · " + b.PlainText)) { FontFamily = new FontFamily("Segoe UI"), FontSize = 12, FontWeight = FontWeights.SemiBold, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(0, 4, 0, 12) }); continue; }
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
            if (b.Who > 0 && starts[i] && b.Who <= s.Presenters.Count)
            {
                para.Inlines.Add(new Run(s.Presenters[b.Who - 1].Name.ToUpperInvariant()) { FontFamily = new FontFamily("Segoe UI"), FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ScriptFlow.ParseBrush(s.Presenters[b.Who - 1].Color, Brushes.Black) });
                para.Inlines.Add(new LineBreak());
            }
            foreach (var inl in ScriptFlow.Inlines(b))
            {
                // Cores claras do prompter (amarelo em fundo preto) ficariam ilegíveis no papel.
                if (inl.ReadLocalValue(TextElement.ForegroundProperty) != DependencyProperty.UnsetValue) inl.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0x60, 0x00));
                para.Inlines.Add(inl);
            }
            if (skipped[i]) { para.TextDecorations = TextDecorations.Strikethrough; para.Foreground = Brushes.Gray; }
            fd.Blocks.Add(para);
        }
        var dlg = new PrintDialog();
        if (pdf)
        {
            try
            {
                var q = new LocalPrintServer().GetPrintQueues().FirstOrDefault(x => x.Name.Contains("PDF", StringComparison.OrdinalIgnoreCase));
                if (q != null) dlg.PrintQueue = q;
            }
            catch { /* spooler indisponível */ }
        }
        if (dlg.ShowDialog() != true) return;
        fd.PageHeight = dlg.PrintableAreaHeight; fd.PageWidth = dlg.PrintableAreaWidth;
        dlg.PrintDocument(((IDocumentPaginatorSource)fd).DocumentPaginator, CurrentDocName());
        Flash(pdf ? "PDF gerado" : "Enviado para a impressora");
    }
}
