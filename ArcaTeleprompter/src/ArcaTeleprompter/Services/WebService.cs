using System.IO;
using Arca.Core.Net;
using Arca.Core.Storage;
using Arca.Server;

namespace Arca.App.Services;

/// <summary>
/// Monitor web e PWA do iPad: liga o <see cref="PrompterServer"/> (Arca.Server.dll), publica o texto,
/// o estado e a posição, e aplica no app os comandos vindos do navegador (quando liberados).
/// </summary>
public sealed class WebService
{
    private readonly MainWindow _main;
    private PrompterServer? _srv;
    private string _lastState = "";
    private DateTime _lastPos;

    public WebService(MainWindow main) => _main = main;

    public bool Running => _srv?.Running == true;
    /// <summary>Iniciando ou parando (evita pedidos duplicados do interruptor).</summary>
    public bool Busy { get; private set; }

    public async Task<bool> Start()
    {
        if (!_main.Allow("web")) return false;
        var vm = _main.Vm; var app = vm.App;
        if (Running || Busy) return Running;
        Busy = true;
        try { return await StartCore(); } finally { Busy = false; }
    }

    private async Task<bool> StartCore()
    {
        var vm = _main.Vm; var app = vm.App;
        vm.WebStatus = "Iniciando servidor…"; vm.WebDot = "Accent";
        _srv = new PrompterServer { AllowControl = app.WebControl, Password = app.WebPassword };
        _srv.Command += c => _main.Dispatcher.BeginInvoke(() => _main.RemoteCommand(c.Name, c.Value));
        _srv.ViewersChanged += n => _main.Dispatcher.BeginInvoke(() => { vm.WebViewers = n; Status(); });
        try { await _srv.StartAsync(app.WebPort, https: true); }
        catch (Exception e)
        {
            Log.Error("Monitor web", e);
            _srv = null;
            vm.WebOn = false; vm.WebDot = "Live";
            vm.WebStatus = e.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase) || e is System.Net.Sockets.SocketException or IOException
                ? $"Erro · porta {app.WebPort} em uso por outro programa" : "Erro · " + e.Message;
            return false;
        }
        var ips = PrompterServer.LanAddresses();
        var host = ips.FirstOrDefault() ?? "localhost";
        vm.WebUrl = $"http://{host}:{app.WebPort}/";
        vm.WebUrlHttps = $"https://{host}:{app.WebPort + 1}/";
        vm.WebOn = true;
        _lastState = "";
        PublishAll();
        Status();
        return true;
    }

    public async Task Stop()
    {
        var s = _srv; _srv = null;
        Busy = true;
        try { if (s != null) await s.StopAsync(); } finally { Busy = false; }
        var vm = _main.Vm;
        vm.WebOn = false; vm.WebViewers = 0; vm.WebUrl = ""; vm.WebUrlHttps = "";
        Status();
    }

    public void UpdateOptions()
    {
        if (_srv == null) return;
        _srv.AllowControl = _main.Vm.App.WebControl;
        _srv.Password = _main.Vm.App.WebPassword;
        _lastState = ""; PublishState();
    }

    private void Status()
    {
        var vm = _main.Vm;
        if (!Running) { vm.WebStatus = "Desligado"; vm.WebDot = "Faint"; }
        else { vm.WebStatus = $"Ativo · {vm.WebViewers} navegador(es) conectado(s)" + (vm.App.WebControl ? " · controle liberado" : ""); vm.WebDot = "Ok"; }
        _main.UpdateNetLabel();
    }

    public void PublishAll() { if (!Running) return; PublishHtml(); PublishState(); PublishPosition(true); }

    public void PublishHtml() { if (Running) _srv!.PublishHtml(_main.DisplayHtml()); }

    /// <summary>Publica o estado só quando muda (chamado ~5×/s).</summary>
    public void PublishState()
    {
        if (!Running) return;
        var snap = _main.Snapshot();
        var sig = System.Text.Json.JsonSerializer.Serialize(snap);
        if (sig == _lastState) return;
        _lastState = sig;
        _srv!.PublishState(snap);
    }

    /// <summary>Posição a ~30 Hz (o navegador extrapola entre mensagens).</summary>
    public void PublishPosition(bool force = false)
    {
        if (!Running) return;
        var now = DateTime.UtcNow;
        if (!force && (now - _lastPos).TotalMilliseconds < 33) return;
        _lastPos = now;
        var (y, v, b, o, f, h) = _main.PositionForWeb();
        _srv!.PublishPosition(y, v, b, o, f, h);
    }

    public void Flash(string text, string? sub) { if (Running) _srv!.Flash(text, sub); }
}
