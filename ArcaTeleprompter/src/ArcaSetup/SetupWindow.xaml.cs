using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using Shapes = System.Windows.Shapes;
using Microsoft.Win32;

namespace Arca.Setup;

/// <summary>
/// Assistente de instalação/desinstalação (desenho de Instalador.dc.html).
/// Instalação: Boas-vindas → Licença → Local → Componentes → Instalação → Concluído.
/// Desinstalação: Confirmar → Motivo → Remoção → Concluído.
/// </summary>
public partial class SetupWindow : Window
{
    private readonly bool _install;
    private int _step;
    private readonly InstallOptions _o = new();
    private bool _accepted, _busy, _failed, _launch = true, _notes;
    private CancellationTokenSource _cts = new();
    private readonly List<(string Text, bool Warn)> _log = new();
    private string _task = "", _file = ""; private double _pct;
    private readonly (long App, long Voice, long Samples) _sizes;
    private Manifest? _man;
    private bool _keepScripts = true, _keepPrefs = true, _keepProfiles = true, _keepLogs;
    private string _reason = "", _feedback = "";
    private string? _error;

    private static readonly string[] InstallSteps = { "Boas-vindas", "Licença", "Local", "Componentes", "Instalação", "Concluído" };
    private static readonly string[] UnSteps = { "Confirmar", "Motivo", "Remoção", "Concluído" };

    private readonly bool _update;

    public SetupWindow(bool install, InstallOptions? resume, bool update = false)
    {
        _install = install;
        InitializeComponent();
        VersionText.Text = $"Versão {Installer.Version} · 64 bits\n© 2026 ARCA";
        if (install)
        {
            _sizes = Installer.PayloadSizes();
            _o.Scope = Scope.AllUsers; _o.Path = Installer.DefaultPath(Scope.AllUsers);
            var prev = Installer.Installed();
            if (prev != null) { _o.Path = prev.Path; _o.Scope = prev.Scope; }
            if (resume != null) { Copy(resume, _o); _accepted = true; _step = 4; _update = update; Loaded += (_, _) => _ = RunInstall(); }
            // Atualização pedida pelo app: mesma pasta, mesmo escopo e os mesmos componentes da instalação existente;
            // vai direto para a instalação (o Windows pede permissão se a pasta exigir) e reabre o app no fim.
            else if (update && prev != null)
            {
                FromManifest(prev, _o); _accepted = true; _launch = true; _update = true; _step = 4;
                Loaded += (_, _) => _ = StartInstall();
            }
        }
        else
        {
            Title = "Desinstalar ARCA Teleprompter";
            _man = Installer.Installed();
            if (_man == null && File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "install.json")))
                _man = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "install.json")));
        }
        Render();
    }

    private static void Copy(InstallOptions a, InstallOptions b) { foreach (var p in typeof(InstallOptions).GetProperties()) p.SetValue(b, p.GetValue(a)); }

    /// <summary>Opções que reproduzem a instalação existente (atalhos, associação, firewall, exemplos e modelo de voz).</summary>
    private static void FromManifest(Manifest m, InstallOptions o)
    {
        o.Path = m.Path; o.Scope = m.Scope;
        var desktop = new[] { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) };
        bool OnDesktop(string l) => desktop.Any(d => d.Length > 0 && l.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        o.Desktop = m.Shortcuts.Any(OnDesktop);
        o.StartMenu = m.Shortcuts.Any(l => !OnDesktop(l));
        o.Associate = m.Associated;
        o.Web = m.Firewall;
        o.Samples = m.SamplesDir != null;
        o.Voice = Directory.Exists(System.IO.Path.Combine(m.Path, "models"));
        o.Ndi = false;
    }

    private double SizeMb => (_sizes.App + (_o.Voice ? _sizes.Voice : 0) + (_o.Samples ? _sizes.Samples : 0)) / 1048576.0;
    private bool Busy => _install ? _step == 4 : _step == 2;
    private bool Done => _install ? _step == 5 : _step == 3;

    // ═══ Desenho ═══
    private void Render()
    {
        var names = _install ? InstallSteps : UnSteps;
        SideCaption.Text = _install ? "ASSISTENTE DE INSTALAÇÃO" : "DESINSTALADOR";
        WinTitle.Text = _install ? "Instalação do ARCA Teleprompter" : "Desinstalar ARCA Teleprompter";
        Steps.Children.Clear();
        for (var i = 0; i < names.Length; i++)
        {
            bool past = i < _step || (Done && i == _step), cur = i == _step && !Done;
            var dot = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Background = past ? Hex("#FF5A4A") : Brushes.Transparent, BorderBrush = cur ? Hex("#FF5A4A") : Hex("#3A3A40"), BorderThickness = new Thickness(past ? 0 : cur ? 1.5 : 1) };
            dot.Child = new TextBlock { Text = past ? "✓" : (i + 1).ToString(), FontFamily = (FontFamily)FindResource("Num"), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = past ? Hex("#1A0705") : cur ? Hex("#ECECEF") : Hex("#6E6E76"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Height = 34 };
            row.Children.Add(dot);
            row.Children.Add(new TextBlock { Text = names[i], Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5, FontWeight = cur ? FontWeights.SemiBold : FontWeights.Medium, Foreground = cur ? Brushes.White : past ? Hex("#C8C8CE") : Hex("#6E6E76") });
            Steps.Children.Add(row);
        }
        if (_install) RenderInstall(); else RenderUninstall();
        BackBtn.Visibility = !Busy && !Done && _step > 0 ? Visibility.Visible : Visibility.Hidden;
        CancelBtn.Visibility = Done || (Busy && !_failed) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Head(string eyebrow, string title, string lead) { Eyebrow.Text = eyebrow.ToUpperInvariant(); TitleText.Text = title; Lead.Text = lead; }

    private void RenderInstall()
    {
        switch (_step)
        {
            case 0:
            {
                var prev = Installer.Installed();
                var kind = UpdateKind(prev);
                var sp = new StackPanel();
                if (kind == Kind.Fresh)
                {
                    Head("Boas-vindas", "Instalar o ARCA Teleprompter", "Este assistente instala o teleprompter no seu computador. Leva cerca de um minuto.");
                    sp.Children.Add(Table(new[] { ("Versão", Installer.Version + " (64 bits)"), ("Requisitos", "Windows 10 21H2 ou superior · 4 GB de RAM"), ("Espaço", $"≈ {SizeMb:0} MB"), ("Editor", "ARCA") }));
                    sp.Children.Add(Small("Feche outros programas antes de continuar. O instalador não reinicia o computador.", "#67675F"));
                }
                else
                {
                    var (title, sub) = kind switch
                    {
                        Kind.Upgrade => ("Atualizar o ARCA Teleprompter", $"Atualização da versão {prev!.Version} para a {Installer.Version}."),
                        Kind.Same => ("Reinstalar o ARCA Teleprompter", $"A versão {Installer.Version} já está instalada. Reinstalar repara os arquivos do programa."),
                        _ => ("Versão mais antiga", $"Está instalada a versão {prev!.Version}, mais nova que esta ({Installer.Version}).")
                    };
                    Head(kind == Kind.Upgrade ? "Atualização" : "Boas-vindas", title, sub);
                    sp.Children.Add(Table(new[] { ("Instalada", prev!.Version), ("Nova", Installer.Version + " (64 bits)"), ("Pasta", prev.Path), ("Seus dados", "roteiros, preferências e modelos de voz são mantidos") }));
                    if (kind == Kind.Downgrade)
                        sp.Children.Add(Check("Instalar a versão mais antiga mesmo assim", _downgradeOk, v => { _downgradeOk = v; NextBtn.IsEnabled = v; }, 14));
                    else
                        sp.Children.Add(Small("O que mudou nesta versão aparece ao concluir (Ver as novidades).", "#67675F"));
                }
                Body.Content = sp; NextBtn.Content = "Avançar"; NextBtn.IsEnabled = kind != Kind.Downgrade || _downgradeOk;
            }
                break;
            case 1:
                Head("Licença", "Contrato de licença", "Leia os termos antes de continuar.");
                var lic = new StackPanel();
                var box = new Border { Height = 190, BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Background = Hex("#F7F7F5") };
                box.Child = new ScrollViewer { Padding = new Thickness(16, 14, 16, 14), Content = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, LineHeight = 20, Foreground = Hex("#3A3A36"), Text = License } };
                lic.Children.Add(box);
                var acc = new CheckBox { Content = "Li e aceito os termos do contrato de licença", IsChecked = _accepted, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 14, 0, 0) };
                acc.Checked += (_, _) => { _accepted = true; NextBtn.IsEnabled = true; };
                acc.Unchecked += (_, _) => { _accepted = false; NextBtn.IsEnabled = false; };
                lic.Children.Add(acc);
                Body.Content = lic; NextBtn.Content = "Avançar"; NextBtn.IsEnabled = _accepted;
                break;
            case 2:
                Head("Local", "Onde instalar", "Escolha a pasta e para quem o aplicativo ficará disponível.");
                Body.Content = LocationPage(); NextBtn.Content = "Avançar"; NextBtn.IsEnabled = PathOk(_o.Path);
                break;
            case 3:
                Head("Componentes", "Escolha os componentes", _error ?? "Você pode adicionar ou remover componentes depois, instalando de novo.");
                Body.Content = ComponentsPage(); NextBtn.Content = UpdateKind(Installer.Installed()) == Kind.Upgrade ? "Atualizar" : "Instalar"; NextBtn.IsEnabled = true;
                break;
            case 4:
                Head(_update ? "Atualização" : "Instalação", _failed ? (_update ? "A atualização falhou" : "A instalação falhou") : _update ? $"Atualizando para a versão {Installer.Version}" : "Instalando",
                    _failed ? "As alterações foram desfeitas. " + _error : _update ? "O ARCA Teleprompter abre sozinho ao terminar. Seus roteiros e preferências são mantidos." : "Não desligue o computador durante a instalação.");
                Body.Content = ProgressPage(); NextBtn.Content = _failed ? "Tentar de novo" : "Instalando…"; NextBtn.IsEnabled = _failed;
                if (_failed) CancelBtn.Content = "Fechar";
                break;
            case 5:
                Head("Concluído", "Tudo pronto", _wasUpgrade ? $"O ARCA Teleprompter foi atualizado para a versão {Installer.Version}." : "O ARCA Teleprompter foi instalado.");
                var d = new StackPanel();
                d.Children.Add(DoneBox(_wasUpgrade ? $"Atualizado para a versão {Installer.Version}" : $"ARCA Teleprompter {Installer.Version} instalado", _o.Path));
                d.Children.Add(Check("Abrir o ARCA Teleprompter agora", _launch, v => { _launch = v; NextBtn.Content = v ? "Abrir o app" : "Concluir"; }, 16));
                d.Children.Add(Check("Ver as novidades desta versão", _notes, v => _notes = v, 8));
                if (_o.Ndi && !NdiInstalled()) d.Children.Add(Small("A saída NDI precisa do NDI Runtime (gratuito). Vamos abrir a página de download ao concluir.", "#8A1C13"));
                Body.Content = d; NextBtn.Content = _launch ? "Abrir o app" : "Concluir"; NextBtn.IsEnabled = true;
                break;
        }
    }

    private UIElement LocationPage()
    {
        var sp = new StackPanel();
        sp.Children.Add(Label("Pasta de destino"));
        var row = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
        var browse = new Button { Style = (Style)FindResource("Soft"), Content = "Procurar…", Margin = new Thickness(8, 0, 0, 0), FontSize = 12 };
        var tb = new TextBox { Style = (Style)FindResource("Field"), Text = _o.Path };
        var err = Small("Informe uma pasta válida, por exemplo C:\\Program Files\\ARCA Teleprompter", "#D42B1E");
        var admin = Small("", "#67675F");
        void Upd()
        {
            var ok = PathOk(_o.Path);
            err.Visibility = ok ? Visibility.Collapsed : Visibility.Visible; NextBtn.IsEnabled = ok;
            admin.Text = ok && Installer.NeedsAdmin(_o.Path, _o.Scope) ? (_o.Scope == Scope.AllUsers ? "Instalar para todos os usuários exige permissão de administrador: o Windows vai pedir confirmação." : "Esta pasta exige permissão de administrador: o Windows vai pedir confirmação ao instalar.") : "";
            UpdDisk();
        }
        tb.TextChanged += (_, _) => { _o.Path = tb.Text.Trim(); Upd(); };
        browse.Click += (_, _) =>
        {
            var dlg = new OpenFolderDialog { Title = "Pasta de destino", InitialDirectory = Directory.Exists(_o.Path) ? _o.Path : System.IO.Path.GetDirectoryName(_o.Path) ?? "" };
            if (dlg.ShowDialog(this) == true)
            {
                var p = dlg.FolderName;
                _o.Path = System.IO.Path.GetFileName(p).Equals(Installer.Product, StringComparison.OrdinalIgnoreCase) ? p : System.IO.Path.Combine(p, Installer.Product);
                tb.Text = _o.Path;
            }
        };
        DockPanel.SetDock(browse, Dock.Right); row.Children.Add(browse); row.Children.Add(tb);
        sp.Children.Add(row); sp.Children.Add(err); sp.Children.Add(admin);
        sp.Children.Add(Label("Instalar para", 16));
        var seg = new Border { Background = Hex("#EEEEEC"), CornerRadius = new CornerRadius(11), Padding = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
        var segp = new StackPanel { Orientation = Orientation.Horizontal }; seg.Child = segp;
        foreach (var (s, l) in new[] { (Scope.AllUsers, "Todos os usuários"), (Scope.CurrentUser, "Somente para mim") })
        {
            var on = _o.Scope == s;
            var b = new Button { Content = l, Height = 28, Padding = new Thickness(14, 0, 14, 0), FontSize = 12, FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand, Foreground = on ? Hex("#1A1A18") : Hex("#5A5A55"), Template = SegTemplate(on) };
            b.Click += (_, _) => { var wasDefault = _o.Path == Installer.DefaultPath(_o.Scope); _o.Scope = s; if (wasDefault) _o.Path = Installer.DefaultPath(s); Body.Content = LocationPage(); };
            segp.Children.Add(b);
        }
        sp.Children.Add(seg);
        _disk = new UniformGrid { Columns = 3, Margin = new Thickness(0, 16, 0, 0) };
        sp.Children.Add(new Border { BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = _disk, ClipToBounds = true });
        Upd();
        return sp;
    }
    private UniformGrid? _disk;
    private void UpdDisk()
    {
        if (_disk == null) return;
        _disk.Children.Clear();
        string free = "—", drive = "C:";
        try { var di = new DriveInfo(System.IO.Path.GetPathRoot(_o.Path) ?? "C:\\"); free = $"{di.AvailableFreeSpace / 1073741824.0:0.0} GB"; drive = di.Name.TrimEnd('\\'); } catch { }
        foreach (var (k, v) in new[] { ("Necessário", $"{SizeMb:0} MB"), ("Disponível", free), ("Unidade", drive) })
        {
            var c = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };
            c.Children.Add(new TextBlock { Text = k, FontSize = 11, Foreground = Hex("#67675F") });
            c.Children.Add(new TextBlock { Text = v, FontFamily = (FontFamily)FindResource("Num"), FontWeight = FontWeights.SemiBold });
            _disk.Children.Add(c);
        }
    }

    private UIElement ComponentsPage()
    {
        var sp = new StackPanel();
        var list = new StackPanel();
        var mb = (double b) => b <= 0 ? "incluído" : $"{b / 1048576.0:0} MB";
        void Comp(string name, string desc, string size, bool on, bool enabled, Action<bool>? set, bool first = false)
        {
            var g = new Grid { Margin = new Thickness(14, 11, 14, 11) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cb = new CheckBox { IsChecked = on, IsEnabled = enabled, VerticalAlignment = VerticalAlignment.Center };
            if (set != null) { cb.Checked += (_, _) => { set(true); }; cb.Unchecked += (_, _) => set(false); }
            g.Children.Add(cb);
            var t = new StackPanel { Margin = new Thickness(12, 0, 12, 0) }; Grid.SetColumn(t, 1);
            t.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold });
            t.Children.Add(new TextBlock { Text = desc, FontSize = 12, Foreground = Hex("#67675F"), TextWrapping = TextWrapping.Wrap, LineHeight = 17 });
            g.Children.Add(t);
            var s = new TextBlock { Text = size, FontFamily = (FontFamily)FindResource("Num"), FontSize = 11.5, Foreground = Hex("#5A5A55"), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(s, 2); g.Children.Add(s);
            list.Children.Add(new Border { BorderBrush = Hex("#EBEBEB"), BorderThickness = new Thickness(0, first ? 0 : 1, 0, 0), Background = enabled ? Brushes.White : Hex("#FAFAF8"), Child = g });
        }
        Comp("Aplicativo ARCA Teleprompter (obrigatório)", "Editor, janela do talento, saídas para monitores, monitor web/iPad e controle remoto. Inclui as fontes de leitura.", mb(_sizes.App), true, false, null, true);
        Comp("Acompanhamento por voz offline", "Modelo de reconhecimento de fala em português (Vosk), executado no computador, sem internet. Funciona com microfone WASAPI ou interface ASIO.", mb(_sizes.Voice), _o.Voice, _sizes.Voice > 0, v => { _o.Voice = v; });
        Comp("Monitor web e iPad", "Libera as portas 8420/8421 no firewall (rede privada) para acompanhar e controlar o prompter pelo navegador ou pelo iPad.", "—", _o.Web, true, v => _o.Web = v);
        var ndi = NdiInstalled();
        Comp("Saída NDI", ndi ? "NDI Runtime já instalado neste computador: a saída NDI funciona (vMix, OBS, Studio Monitor)." : "Envia o prompter ao vMix e outros softwares pela rede. Requer o NDI Runtime gratuito — abriremos a página de download ao concluir.", ndi ? "instalado" : "download", ndi || _o.Ndi, !ndi, v => _o.Ndi = v);
        Comp("Roteiros de exemplo", "Roteiros para testar marcadores, pausas e apresentadores, em Documentos › ARCA Teleprompter.", mb(_sizes.Samples), _o.Samples, true, v => _o.Samples = v);
        sp.Children.Add(new Border { BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), ClipToBounds = true, Child = list });
        sp.Children.Add(Check("Criar atalho no Menu Iniciar", _o.StartMenu, v => _o.StartMenu = v, 14));
        sp.Children.Add(Check("Criar atalho na área de trabalho", _o.Desktop, v => _o.Desktop = v, 8));
        sp.Children.Add(Check("Abrir arquivos .tps com o ARCA Teleprompter", _o.Associate, v => _o.Associate = v, 8));
        return sp;
    }

    private UIElement ProgressPage()
    {
        var sp = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var top = new DockPanel();
        var pct = new TextBlock { Text = $"{_pct:0}%", FontFamily = (FontFamily)FindResource("Num"), FontWeight = FontWeights.SemiBold, FontSize = 15 }; DockPanel.SetDock(pct, Dock.Right);
        top.Children.Add(pct); top.Children.Add(new TextBlock { Text = _task, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        sp.Children.Add(top);
        var bar = new Grid { Height = 6, Margin = new Thickness(0, 10, 0, 10) };
        bar.Children.Add(new Border { CornerRadius = new CornerRadius(3), Background = Hex("#EEEEEC") });
        bar.Children.Add(new Border { CornerRadius = new CornerRadius(3), Background = Hex("#D42B1E"), HorizontalAlignment = HorizontalAlignment.Left, Width = 4.5 * Math.Max(1, _pct) });
        sp.Children.Add(bar);
        sp.Children.Add(new TextBlock { Text = _file, FontFamily = (FontFamily)FindResource("Num"), FontSize = 11, Foreground = Hex("#67675F"), TextTrimming = TextTrimming.CharacterEllipsis });
        var log = new StackPanel();
        foreach (var (t, w) in _log)
        {
            var r = new StackPanel { Orientation = Orientation.Horizontal };
            r.Children.Add(new TextBlock { Text = w ? "!" : "✓", Foreground = Hex(w ? "#B26B00" : "#2F8F5B"), Width = 16 });
            r.Children.Add(new TextBlock { Text = t, TextWrapping = TextWrapping.Wrap, MaxWidth = 410 });
            log.Children.Add(r);
        }
        var sv = new ScrollViewer { Content = log, Padding = new Thickness(12, 10, 12, 10) };
        sv.Loaded += (_, _) => sv.ScrollToEnd();
        sp.Children.Add(new Border { Height = 150, Margin = new Thickness(0, 12, 0, 0), BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Background = Hex("#F7F7F5"), Child = sv });
        ((Border)sp.Children[^1]).SetValue(TextElement.FontFamilyProperty, FindResource("Num"));
        ((Border)sp.Children[^1]).SetValue(TextElement.FontSizeProperty, 11.0);
        ((Border)sp.Children[^1]).SetValue(TextElement.ForegroundProperty, Hex("#5A5A55"));
        return sp;
    }

    private void RenderUninstall()
    {
        switch (_step)
        {
            case 0:
                Head("Desinstalar", "Remover o ARCA Teleprompter", _man == null ? "Instalação não encontrada neste computador." : "Escolha o que manter. Os arquivos mantidos serão usados se você instalar de novo.");
                var sp = new StackPanel();
                if (_man != null)
                {
                    var card = new Border { BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 12, 14, 12) };
                    var cp = new StackPanel { Orientation = Orientation.Horizontal };
                    cp.Children.Add(new Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon-64.png")), Width = 36, Height = 36, Margin = new Thickness(0, 0, 12, 0) });
                    var ct = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    ct.Children.Add(new TextBlock { Text = $"ARCA Teleprompter {_man.Version}", FontWeight = FontWeights.SemiBold });
                    long size = 0; try { size = Directory.EnumerateFiles(_man.Path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); } catch { }
                    ct.Children.Add(new TextBlock { Text = $"{_man.Path} · {size / 1048576} MB", FontFamily = (FontFamily)FindResource("Num"), FontSize = 11, Foreground = Hex("#67675F") });
                    cp.Children.Add(ct); card.Child = cp; sp.Children.Add(card);
                    sp.Children.Add(Label("O que fazer com seus dados", 16));
                    var keep = new StackPanel();
                    void K(string n, string d, bool on, Action<bool> set, bool first = false)
                    {
                        var g = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(14, 11, 14, 11) };
                        var cb = new CheckBox { IsChecked = on, VerticalAlignment = VerticalAlignment.Center }; cb.Checked += (_, _) => set(true); cb.Unchecked += (_, _) => set(false);
                        g.Children.Add(cb);
                        var t = new StackPanel { Margin = new Thickness(12, 0, 0, 0) }; t.Children.Add(new TextBlock { Text = n, FontWeight = FontWeights.SemiBold }); t.Children.Add(new TextBlock { Text = d, FontSize = 12, Foreground = Hex("#67675F") });
                        g.Children.Add(t);
                        keep.Children.Add(new Border { BorderBrush = Hex("#EBEBEB"), BorderThickness = new Thickness(0, first ? 0 : 1, 0, 0), Child = g });
                    }
                    K("Manter roteiros e versões", "Abas abertas, histórico e salvamento automático.", _keepScripts, v => _keepScripts = v, true);
                    K("Manter preferências", "Atalhos, telas, monitor web, NDI e integrações.", _keepPrefs, v => _keepPrefs = v);
                    K("Manter perfis de apresentador", "Fonte, margem, cores e velocidade de cada perfil.", _keepProfiles, v => _keepProfiles = v);
                    K("Manter registros e cache", "Registros de diagnóstico e modelos de voz baixados.", _keepLogs, v => _keepLogs = v);
                    sp.Children.Add(new Border { Margin = new Thickness(0, 6, 0, 0), BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), ClipToBounds = true, Child = keep });
                    if (Installer.AppRunning())
                    {
                        var warn = new Border { Margin = new Thickness(0, 12, 0, 0), Padding = new Thickness(14, 10, 14, 10), CornerRadius = new CornerRadius(10), Background = Hex("#14D42B1E") };
                        var wd = new DockPanel();
                        var close = new Button { Style = (Style)FindResource("Soft"), Content = "Fechar agora", Height = 28, FontSize = 12 };
                        close.Click += async (_, _) => { await Installer.CloseApp(); Render(); };
                        DockPanel.SetDock(close, Dock.Right); wd.Children.Add(close);
                        wd.Children.Add(new TextBlock { Text = "O ARCA Teleprompter está aberto. Ele será fechado antes de remover os arquivos.", Foreground = Hex("#8A1C13"), FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
                        warn.Child = wd; sp.Children.Add(warn);
                    }
                }
                Body.Content = sp; NextBtn.Content = _man == null ? "Fechar" : "Desinstalar"; NextBtn.IsEnabled = true;
                break;
            case 1:
                Head("Motivo", "Por que está removendo?", "Opcional. Fica registrado só neste computador.");
                var m = new StackPanel();
                var chips = new WrapPanel();
                foreach (var l in new[] { "Vou reinstalar", "Instalei uma versão nova", "Não uso mais", "Problemas de desempenho", "Falta um recurso", "Outro" })
                {
                    var on = _reason == l;
                    var b = new Button { Content = l, Height = 30, Padding = new Thickness(14, 0, 14, 0), FontSize = 12, Margin = new Thickness(0, 0, 6, 6), Cursor = Cursors.Hand, Foreground = on ? Brushes.White : Hex("#1A1A18"), Template = ChipTemplate(on) };
                    b.Click += (_, _) => { _reason = _reason == l ? "" : l; Render(); };
                    chips.Children.Add(b);
                }
                m.Children.Add(chips);
                var fb = new TextBox { Style = (Style)FindResource("Field"), Height = 84, Text = _feedback, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalContentAlignment = VerticalAlignment.Top, FontFamily = (FontFamily)FindResource("Sans"), FontSize = 13, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 10, 0, 0) };
                fb.TextChanged += (_, _) => _feedback = fb.Text;
                m.Children.Add(fb);
                Body.Content = m; NextBtn.Content = "Remover"; NextBtn.IsEnabled = true;
                break;
            case 2:
                Head("Remoção", _failed ? "Remoção incompleta" : "Removendo", _failed ? _error ?? "" : "Isso leva alguns segundos.");
                Body.Content = ProgressPage(); NextBtn.Content = _failed ? "Fechar" : "Removendo…"; NextBtn.IsEnabled = _failed;
                break;
            case 3:
                Head("Concluído", "Desinstalação concluída", "O ARCA Teleprompter foi removido deste computador.");
                Body.Content = DoneBox("ARCA Teleprompter removido", _keepScripts || _keepPrefs || _keepProfiles ? "Seus dados mantidos ficam em %APPDATA%\\ARCA Teleprompter" : "Todos os dados foram apagados");
                NextBtn.Content = "Fechar"; NextBtn.IsEnabled = true;
                break;
        }
    }

    // ═══ Navegação ═══
    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_install)
        {
            if (_step == 3) { await StartInstall(); return; }
            if (_step == 4 && _failed) { CancelBtn.Content = "Cancelar"; await RunInstall(); return; }
            if (_step == 5) { Finish(); return; }
            _step++; Render();
            return;
        }
        if (_man == null || _step == 3 || (_step == 2 && _failed)) { Close(); return; }
        if (_step == 1) { await RunUninstall(); return; }
        _step++; Render();
    }
    private void Back_Click(object sender, RoutedEventArgs e) { if (_step > 0) { _step--; _error = null; Render(); } }

    private enum Kind { Fresh, Upgrade, Same, Downgrade }
    private bool _downgradeOk, _wasUpgrade;

    /// <summary>Instalação nova, atualização, reinstalação ou volta para uma versão mais antiga.</summary>
    private static Kind UpdateKind(Manifest? prev)
    {
        if (prev == null) return Kind.Fresh;
        if (!Version.TryParse(prev.Version, out var old) || !Version.TryParse(Installer.Version, out var cur)) return Kind.Upgrade;
        var c = cur.CompareTo(old);
        return c > 0 ? Kind.Upgrade : c == 0 ? Kind.Same : Kind.Downgrade;
    }

    private async Task StartInstall()
    {
        _wasUpgrade = UpdateKind(Installer.Installed()) == Kind.Upgrade;
        if (!PathOk(_o.Path)) return;
        // Pasta protegida (ex.: Arquivos de Programas) sem administrador: reabre o instalador elevado (UAC).
        if (Installer.NeedsAdmin(_o.Path, _o.Scope))
        {
            var f = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"arca-setup-{Guid.NewGuid():N}.json");
            File.WriteAllText(f, JsonSerializer.Serialize(_o));
            if (Installer.RelaunchElevated($"--resume \"{f}\"" + (_update ? " --update" : ""))) { Application.Current.Shutdown(); return; }
            File.Delete(f);
            _error = "O Windows não concedeu permissão de administrador. Escolha “Somente para mim” ou outra pasta para instalar sem ela.";
            _step = 3; Render(); return;
        }
        await RunInstall();
    }

    private async Task RunInstall()
    {
        if (_cts.IsCancellationRequested) _cts = new();
        _step = 4; _busy = true; _failed = false; _log.Clear(); _pct = 0; _task = "Preparando"; _file = "";
        Render();
        var prog = new Progress<Progress>(p =>
        {
            if (p.Task != null) _task = p.Task; if (p.File != null) _file = p.File; if (p.Pct is { } x) _pct = x;
            if (p.Log != null) _log.Add((p.Log, p.Warn));
            Render();
        });
        try
        {
            await Task.Run(() => Installer.Install(_o, prog, _cts.Token));
            _step = 5;
            // Atualização: reabre o app sozinho (ele mostra as novidades da versão nova ao abrir).
            if (_update) { _busy = false; Render(); await Task.Delay(900); Finish(); return; }
        }
        catch (OperationCanceledException) { _failed = true; _error = "Instalação cancelada. Nenhuma alteração foi mantida no computador."; }
        catch (UnauthorizedAccessException ex) { _failed = true; _error = "Sem permissão do Windows para concluir (" + ex.Message + "). Escolha “Somente para mim” ou outra pasta."; }
        catch (Exception ex) { _failed = true; _error = ex.Message; }
        if (_failed) try { File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arca-setup.log"), $"{DateTime.Now:s} instalação {Installer.Version} falhou em {_file}: {_error}{Environment.NewLine}"); } catch { }
        _busy = false;
        Render();
    }

    private async Task RunUninstall()
    {
        _step = 2; _busy = true; _log.Clear(); _pct = 0;
        Render();
        var prog = new Progress<Progress>(p =>
        {
            if (p.Task != null) _task = p.Task; if (p.File != null) _file = p.File; if (p.Pct is { } x) _pct = x;
            if (p.Log != null) _log.Add((p.Log, p.Warn));
            Render();
        });
        try
        {
            if (_reason.Length > 0 || _feedback.Length > 0)
                File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arca-teleprompter-desinstalacao.txt"), $"{DateTime.Now:s}\n{_reason}\n{_feedback}\n");
            await Task.Run(() => Installer.Uninstall(_man!, new Installer.KeepOptions(_keepScripts, _keepPrefs, _keepProfiles, _keepLogs), prog));
            _step = 3;
        }
        catch (Exception ex) { _failed = true; _error = ex.Message; }
        _busy = false;
        Render();
    }

    private void Finish()
    {
        var exe = System.IO.Path.Combine(_o.Path, Installer.Exe);
        if (_notes) { var n = System.IO.Path.Combine(_o.Path, "docs", "NOVIDADES.txt"); if (File.Exists(n)) Process.Start(new ProcessStartInfo(n) { UseShellExecute = true }); }
        if (_o.Ndi && !NdiInstalled()) Process.Start(new ProcessStartInfo("https://ndi.video/tools/") { UseShellExecute = true });
        // O instalador pode estar como administrador (UAC): abre o app pelo Explorer, como usuário comum.
        if (_launch && File.Exists(exe))
        {
            if (Installer.IsAdmin) Process.Start(new ProcessStartInfo("explorer.exe", "\"" + exe + "\"") { UseShellExecute = false });
            else Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = _o.Path });
        }
        Close();
    }

    // ═══ Cancelar / fechar ═══
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (Done || _failed || (!_install && _man == null)) { Close(); return; }
        ConfirmTitle.Text = _install ? "Cancelar a instalação?" : "Cancelar a desinstalação?";
        ConfirmText.Text = Busy ? "Os arquivos já copiados serão removidos e nenhuma alteração será mantida." : _install ? "O ARCA Teleprompter não será instalado." : "O ARCA Teleprompter continuará instalado.";
        ConfirmBtn.Content = _install ? "Cancelar instalação" : "Cancelar";
        Confirm.Visibility = Visibility.Visible;
    }
    private void KeepGoing_Click(object sender, RoutedEventArgs e) => Confirm.Visibility = Visibility.Collapsed;
    private void DoCancel_Click(object sender, RoutedEventArgs e)
    {
        Confirm.Visibility = Visibility.Collapsed;
        if (Busy && _busy) { _cts.Cancel(); _task = "Revertendo alterações"; Render(); return; }
        Close();
    }
    private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_busy) { e.Cancel = true; Close_Click(this, new RoutedEventArgs()); return; }
        base.OnClosing(e);
    }

    // ═══ Auxiliares visuais ═══
    private static SolidColorBrush Hex(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static bool PathOk(string p) => System.Text.RegularExpressions.Regex.IsMatch(p.Trim(), @"^[a-zA-Z]:\\[^<>:""|?*]*$") && p.Trim().Length > 3;
    private static bool NdiInstalled()
    {
        foreach (var v in new[] { "NDI_RUNTIME_DIR_V6", "NDI_RUNTIME_DIR_V5" })
            if (Environment.GetEnvironmentVariable(v) is { Length: > 0 } d && File.Exists(System.IO.Path.Combine(d, "Processing.NDI.Lib.x64.dll"))) return true;
        return false;
    }
    private TextBlock Label(string t, double top = 0) => new() { Text = t, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Hex("#5A5A55"), Margin = new Thickness(0, top, 0, 0) };
    private static TextBlock Small(string t, string color) => new() { Text = t, FontSize = 12, Foreground = Hex(color), TextWrapping = TextWrapping.Wrap, LineHeight = 18, Margin = new Thickness(0, 10, 0, 0) };
    private UIElement Table((string K, string V)[] rows)
    {
        var sp = new StackPanel();
        for (var i = 0; i < rows.Length; i++)
        {
            var g = new Grid { Margin = new Thickness(14, 10, 14, 10) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); g.ColumnDefinitions.Add(new ColumnDefinition());
            g.Children.Add(new TextBlock { Text = rows[i].K, Foreground = Hex("#67675F") });
            var v = new TextBlock { Text = rows[i].V, FontWeight = FontWeights.Medium }; Grid.SetColumn(v, 1); g.Children.Add(v);
            sp.Children.Add(new Border { BorderBrush = Hex("#EBEBEB"), BorderThickness = new Thickness(0, i == 0 ? 0 : 1, 0, 0), Child = g });
        }
        return new Border { BorderBrush = Hex("#E2E2DE"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), ClipToBounds = true, Child = sp };
    }
    private CheckBox Check(string label, bool on, Action<bool> set, double top)
    {
        var c = new CheckBox { Content = label, IsChecked = on, Margin = new Thickness(0, top, 0, 0) };
        c.Checked += (_, _) => set(true); c.Unchecked += (_, _) => set(false);
        return c;
    }
    private UIElement DoneBox(string head, string sub)
    {
        var b = new Border { CornerRadius = new CornerRadius(12), Background = Hex("#E9F5EE"), Padding = new Thickness(16, 14, 16, 14) };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        var dot = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(17), Background = Hex("#2F8F5B"), Margin = new Thickness(0, 0, 14, 0) };
        dot.Child = new Shapes.Path { Data = Geometry.Parse("M3.5 8.5l3 3 6-6.5"), Stroke = Brushes.White, StrokeThickness = 2, Width = 16, Height = 16, Stretch = Stretch.Uniform };
        sp.Children.Add(dot);
        var t = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        t.Children.Add(new TextBlock { Text = head, FontWeight = FontWeights.SemiBold });
        t.Children.Add(new TextBlock { Text = sub, FontSize = 12, Foreground = Hex("#3A5A48"), TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 400 });
        sp.Children.Add(t); b.Child = sp;
        return b;
    }
    private static ControlTemplate SegTemplate(bool on)
    {
        var f = new FrameworkElementFactory(typeof(Border));
        f.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        f.SetValue(Border.BackgroundProperty, on ? Brushes.White : Brushes.Transparent);
        f.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var c = new FrameworkElementFactory(typeof(ContentPresenter)); c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        f.AppendChild(c);
        return new ControlTemplate(typeof(Button)) { VisualTree = f };
    }
    private static ControlTemplate ChipTemplate(bool on)
    {
        var f = new FrameworkElementFactory(typeof(Border));
        f.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
        f.SetValue(Border.BackgroundProperty, on ? Hex("#1A1A18") : Brushes.White);
        f.SetValue(Border.BorderBrushProperty, on ? Hex("#1A1A18") : Hex("#E2E2DE"));
        f.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        f.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var c = new FrameworkElementFactory(typeof(ContentPresenter)); c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        f.AppendChild(c);
        return new ControlTemplate(typeof(Button)) { VisualTree = f };
    }

    private const string License = """
        Contrato de licença de usuário final — ARCA Teleprompter

        1. Concessão. A ARCA concede a você uma licença não exclusiva para instalar e usar o software em computadores sob seu controle, de acordo com o plano contratado.

        2. Restrições. Não é permitido sublicenciar, alugar, fazer engenharia reversa ou remover avisos de propriedade.

        3. Componentes de terceiros. O software inclui bibliotecas de código aberto (NAudio — MIT; Vosk — Apache 2.0; DocumentFormat.OpenXml — MIT; PdfPig — Apache 2.0; ASP.NET Core — MIT; CommunityToolkit.Mvvm — MIT; QRCoder — MIT) e fontes sob SIL Open Font License. A saída NDI usa o NDI® Runtime, distribuído pela Vizrt NDI AB sob a licença do respectivo fabricante.

        4. Dados. Roteiros e configurações ficam no seu computador. O monitor web funciona apenas na rede local. O reconhecimento de voz é executado offline.

        5. Garantia. O software é fornecido "no estado em que se encontra", sem garantias além das exigidas por lei.

        6. Lei aplicável. Este contrato é regido pelas leis da República Federativa do Brasil.
        """;
}
