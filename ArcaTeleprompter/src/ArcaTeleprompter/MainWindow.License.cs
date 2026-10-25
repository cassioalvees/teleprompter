using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Arca.App.Controls;
using Arca.App.Services;
using Arca.Core.Storage;

namespace Arca.App;

/// <summary>
/// Licença e modo demonstração. Sem licença: 10 minutos de prompter por dia, um roteiro e uma tela de prompter;
/// NDI, monitor web/iPad, voz, redação (MOS), câmera/gravação e impressão ficam para a versão licenciada.
/// O código de ativação fica na conta do site (Minha conta › Licenças), que também remove a licença de um computador.
/// </summary>
public partial class MainWindow
{
    public LicenseService License { get; private set; } = null!;
    public bool IsDemo => !License.IsLicensed;
    public bool TalentOpen => TalentLayer.Visibility == Visibility.Visible;

    private static readonly Dictionary<string, string> DemoFeatures = new()
    {
        ["ndi"] = "A saída NDI",
        ["web"] = "O monitor web e o controle pelo iPad",
        ["voz"] = "O acompanhamento por voz",
        ["mos"] = "A integração com a redação (MOS)",
        ["video"] = "A câmera e a gravação de vídeo",
        ["imprimir"] = "Imprimir e exportar PDF",
        ["telas"] = "Mais de uma tela de prompter",
        ["roteiros"] = "Vários roteiros abertos ao mesmo tempo",
    };

    private void InitLicense()
    {
        License = new LicenseService();
        License.Changed += () => Dispatcher.BeginInvoke(UpdateLicenseStatus);
        UpdateLicenseStatus();
        Log.Info(License.IsLicensed ? $"Licença: {License.Info!.Code} · {License.Info.Name}" : $"Licença: modo demonstração ({License.Demo.RemainingMinutes} min hoje)");
    }

    /// <summary>
    /// Libera o recurso com licença; na demonstração avisa e oferece ativar. Use na entrada de cada recurso.
    /// </summary>
    public bool Allow(string feature)
    {
        if (License.IsLicensed) return true;
        var what = DemoFeatures.TryGetValue(feature, out var w) ? w : "Este recurso";
        var r = ArcaDialog.Ask(this, "Modo demonstração", $"{what} faz parte da versão licenciada",
            "A conta no site é gratuita e a licença é vitalícia. Na demonstração: 10 minutos de prompter por dia, um roteiro e uma tela de prompter.",
            new[] { "Agora não", "Ativar licença" }, 1, 0);
        if (r == 1) _ = ShowLicense();
        return false;
    }

    // ——— Tempo de prompter da demonstração ———

    private bool _demoStopping;
    /// <summary>Chamado a cada quadro com o prompt rodando: conta o tempo e para no limite do dia.</summary>
    private void DemoTick(double dt)
    {
        if (!IsDemo || !Engine.Live) return;
        License.Demo.Add(dt);
        if (!License.Demo.Exhausted || _demoStopping) return;
        _demoStopping = true;
        Pause();
        Dispatcher.BeginInvoke(() => { DemoLimit(); _demoStopping = false; UpdateLicenseStatus(); });
    }

    /// <summary>Play na demonstração: só com tempo do dia sobrando.</summary>
    private bool DemoCanPlay()
    {
        if (!IsDemo || !License.Demo.Exhausted) return true;
        DemoLimit();
        return false;
    }

    private void DemoLimit()
    {
        var r = ArcaDialog.Ask(this, "Modo demonstração", "Os 10 minutos de prompter de hoje acabaram",
            "Amanhã a demonstração libera mais 10 minutos. Com a licença gratuita, o prompter não tem limite.",
            new[] { "Fechar", "Ativar licença" }, 1, 0);
        if (r == 1) _ = ShowLicense();
    }

    // ——— Barra de status ———

    private void UpdateLicenseStatus()
    {
        if (LicenseStatus == null) return;
        if (License.IsLicensed) { LicenseStatus.Text = "Licenciado"; LicenseStatus.Foreground = (Brush)FindResource("Muted"); LicenseStatus.ToolTip = $"{License.Info!.Name} · {License.Info.Code}"; }
        else
        {
            LicenseStatus.Text = $"Demonstração · {License.Demo.RemainingMinutes} min hoje · Ativar";
            LicenseStatus.Foreground = (Brush)FindResource("Accent");
            LicenseStatus.ToolTip = "Clique para ativar a licença";
        }
        Title = License.IsLicensed ? "ARCA Teleprompter" : "ARCA Teleprompter · demonstração";
    }

    private int _statusTick;
    /// <summary>~5×/s: atualiza os minutos restantes da demonstração na barra de status.</summary>
    private void LicenseSlowTick()
    {
        if (IsDemo && Engine.Live && ++_statusTick % 25 == 0) UpdateLicenseStatus();
    }

    private void LicenseStatus_Click(object s, MouseButtonEventArgs e) => _ = ShowLicense();
    private void License_Click(object s, RoutedEventArgs e) => _ = ShowLicense();

    // ——— Telas de licença ———

    /// <summary>Ao abrir o app: sem licença, oferece ativar (depois das boas-vindas); com licença, confere online.</summary>
    public async Task LicenseOnStartup(bool automation)
    {
        if (License.IsLicensed)
        {
            if (await License.CheckOnline() is { } reason)
                ArcaDialog.Info(this, "Licença", "A licença deste computador foi desativada", reason + " O app voltou ao modo demonstração.", danger: true);
            return;
        }
        if (automation) return;
        await Task.Delay(1500);
        while (!IsIdleForPrompt) await Task.Delay(1000);
        await ShowLicense();
    }

    public async Task ActivateFromCommandLine(string code)
    {
        var err = await License.Activate(code);
        Log.Info(err == null ? $"Ativação pela linha de comando: OK ({License.Info!.Code})" : "Ativação pela linha de comando falhou: " + err);
    }

    /// <summary>Abre Minha conta no site (código de ativação, computador ativado, remover a licença).</summary>
    private static void OpenAccount()
    {
        try { Process.Start(new ProcessStartInfo(LicenseService.Server + "/conta") { UseShellExecute = true }); } catch { }
    }

    private bool _licenseOpen;
    public async Task ShowLicense()
    {
        if (_licenseOpen) return;
        _licenseOpen = true;
        try { if (License.IsLicensed) await ShowLicensed(); else await ShowActivate(); }
        finally { _licenseOpen = false; }
    }

    private async Task ShowActivate(string? code = null, string? error = null)
    {
        while (true)
        {
            var box = new TextBox { Style = (Style)FindResource("Input"), Height = 40, FontSize = 16, Text = code ?? "", CharacterCasing = CharacterCasing.Upper, FontFamily = new FontFamily("Consolas") };
            box.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = "CÓDIGO DE ATIVAÇÃO", Style = (Style)FindResource("Eyebrow"), Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 0, 0, 6) });
            sp.Children.Add(box);
            if (error != null) sp.Children.Add(new TextBlock { Text = error, Foreground = (Brush)FindResource("Live"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            var link = new TextBlock { Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap };
            var a = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("Abrir Minha conta")) { Foreground = (Brush)FindResource("Accent") };
            a.Click += (_, _) => OpenAccount();
            link.Inlines.Add(new System.Windows.Documents.Run("O código fica em Minha conta › Licenças, no site. ")); link.Inlines.Add(a); link.Inlines.Add(new System.Windows.Documents.Run(" — ainda não tem conta? Crie lá, é gratuito."));
            sp.Children.Add(link);
            sp.Children.Add(new TextBlock { Text = $"Demonstração: {License.Demo.RemainingMinutes} min de prompter hoje, um roteiro e uma tela. Computador: {LicenseService.MachineCode}", FontSize = 12, Foreground = (Brush)FindResource("Muted"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
            var r = ArcaDialog.Ask(this, "Licença", "Ative o ARCA Teleprompter", "Cole o código da sua conta. A ativação usa a internet uma vez; depois o app funciona offline.",
                new[] { "Continuar na demonstração", "Ativar" }, 1, 0, sp);
            if (r != 1) return;
            code = box.Text.Trim();
            if (code.Length == 0) { error = "Cole o código de ativação."; continue; }
            Mouse.OverrideCursor = Cursors.Wait;
            try { error = await License.Activate(code); }
            finally { Mouse.OverrideCursor = null; }
            if (error == null)
            {
                ArcaDialog.Info(this, "Licença", "ARCA Teleprompter ativado", $"Licenciado para {License.Info!.Name}{(License.Info.Company.Length > 0 ? " · " + License.Info.Company : "")}. Todos os recursos estão liberados neste computador.");
                return;
            }
        }
    }

    private async Task ShowLicensed()
    {
        var i = License.Info!;
        var r = ArcaDialog.Ask(this, "Licença", "ARCA Teleprompter licenciado",
            $"{i.Name}{(i.Company.Length > 0 ? " · " + i.Company : "")}\n{i.Email}\nCódigo {i.Code} · " + (i.Kind == "ilimitada"
                ? $"licença ilimitada\nComputador {LicenseService.MachineCode}\n\nEsta licença ativa em quantos computadores você quiser, com o mesmo código."
                : $"licença vitalícia\nComputador {LicenseService.MachineCode}\n\nPara usar em outro computador, desative aqui (ou remova em Minha conta › Licenças, no site) e ative lá com o mesmo código."),
            new[] { "Fechar", "Minha conta", "Desativar este computador" }, 0, 0);
        if (r == 1) OpenAccount();
        if (r != 2) return;
        if (ArcaDialog.Ask(this, "Licença", "Desativar este computador?", "O app volta ao modo demonstração aqui, e o código fica livre para ativar em outro computador.",
                new[] { "Cancelar", "Desativar" }, 1, 0, danger: true) != 1) return;
        Mouse.OverrideCursor = Cursors.Wait;
        string? err;
        try { err = await License.Deactivate(); }
        finally { Mouse.OverrideCursor = null; }
        if (err != null) ArcaDialog.Info(this, "Licença", "Não foi possível desativar", err, danger: true);
        else ArcaDialog.Info(this, "Licença", "Computador desativado", $"O código {i.Code} está livre para ativar em outro computador.");
    }
}
