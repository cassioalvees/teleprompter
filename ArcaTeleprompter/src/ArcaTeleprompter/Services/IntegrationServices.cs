using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Arca.Core.Model;
using Arca.Core.Storage;
using Arca.Mos;

namespace Arca.App.Services;

/// <summary>
/// Saída de legenda: envia o texto da linha de leitura quando ela muda.
/// "vmix": API HTTP do vMix (SetText num título), "tcp": uma linha por mensagem para encoders de legenda.
/// </summary>
public sealed class CaptionService
{
    private readonly MainWindow _main;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private TcpClient? _tcp;
    private string _last = "";
    private bool _busy;
    public CaptionService(MainWindow main) => _main = main;

    public async void Send(string line)
    {
        var app = _main.Vm.App;
        if (!app.CaptionOut || line == _last || _busy) return;
        _last = line; _busy = true;
        try
        {
            if (app.CaptionMode == "vmix")
            {
                var url = $"http://{app.CaptionHost}:{app.CaptionPort}/api/?Function=SetText&Input={Uri.EscapeDataString(app.CaptionInput)}&SelectedName={Uri.EscapeDataString(app.CaptionField)}&Value={Uri.EscapeDataString(line)}";
                var r = await Http.GetAsync(url);
                _main.Vm.CaptionStatus = r.IsSuccessStatusCode ? "Enviando ao vMix · " + app.CaptionInput : $"vMix respondeu {(int)r.StatusCode}";
            }
            else
            {
                if (_tcp is not { Connected: true }) { _tcp?.Dispose(); _tcp = new TcpClient(); await _tcp.ConnectAsync(app.CaptionHost, app.CaptionPort); }
                var b = Encoding.UTF8.GetBytes(line + "\r\n");
                await _tcp.GetStream().WriteAsync(b);
                _main.Vm.CaptionStatus = $"Enviando para {app.CaptionHost}:{app.CaptionPort}";
            }
        }
        catch (Exception e) { _main.Vm.CaptionStatus = "Falha: " + e.Message; _tcp?.Dispose(); _tcp = null; }
        finally { _busy = false; }
    }

    public void Reset() { _last = ""; _tcp?.Dispose(); _tcp = null; _main.Vm.CaptionStatus = _main.Vm.App.CaptionOut ? "Aguardando texto" : "Desligado"; }
}

/// <summary>
/// Integração com a redação (MOS): liga o <see cref="MosDevice"/> (Arca.Mos.dll) e transforma cada espelho
/// recebido numa aba de roteiro (um marcador por matéria, seguido do texto de leitura).
/// </summary>
public sealed class MosService
{
    private readonly MainWindow _main;
    private MosDevice? _dev;
    public MosService(MainWindow main) => _main = main;
    public bool Running => _dev?.Running == true;

    public bool Start()
    {
        if (Running) return true;
        if (!_main.Allow("mos")) return false;
        var vm = _main.Vm;
        _dev = new MosDevice { MosId = vm.App.MosId };
        _dev.RunningOrderChanged += ro => _main.Dispatcher.BeginInvoke(() => _main.ApplyRunningOrder(ro.Id, ro.Slug, ToDocument(ro)));
        _dev.RunningOrderDeleted += id => _main.Dispatcher.BeginInvoke(() => _main.Flash("Espelho removido pela redação"));
        _dev.Activity += a => _main.Dispatcher.BeginInvoke(() => Status(a));
        try { _dev.Start(); }
        catch (SocketException e)
        {
            _dev = null; vm.MosOn = false;
            vm.MosStatus = "Portas 10540/10541 em uso ou bloqueadas: " + e.Message;
            return false;
        }
        vm.MosOn = true;
        Status(null);
        Log.Info("MOS ouvindo em 10540/10541 como " + vm.App.MosId);
        _main.UpdateNetLabel();
        return true;
    }

    public void Stop() { _dev?.Stop(); _dev = null; _main.Vm.MosOn = false; _main.Vm.MosStatus = "Desconectado"; _main.UpdateNetLabel(); }

    private void Status(string? activity)
    {
        if (_dev == null) return;
        var vm = _main.Vm;
        vm.MosStatus = _dev.LastContact is { } t
            ? $"{vm.App.MosSystem} conectado ({_dev.LastPeer}) · último contato {t:HH:mm:ss}" + (activity != null ? " · " + activity : "")
            : $"Aguardando o {vm.App.MosSystem} nas portas 10540/10541 · ID MOS {vm.App.MosId}";
    }

    /// <summary>Espelho → roteiro: marcador por matéria (slug) e um parágrafo por &lt;p&gt;; instruções [entre colchetes] viram pausas.</summary>
    public static ScriptDocument ToDocument(RunningOrder ro)
    {
        var d = new ScriptDocument();
        foreach (var s in ro.Stories)
        {
            d.Blocks.Add(ScriptBlock.Mark(string.IsNullOrWhiteSpace(s.Slug) ? s.Id : (s.Number.Length > 0 ? s.Number + " · " : "") + s.Slug));
            foreach (var p in s.Paragraphs)
                d.Blocks.Add(p.StartsWith('[') && p.EndsWith(']') ? ScriptBlock.Pause(p.Trim('[', ']')) : ScriptBlock.Paragraph(p));
        }
        if (d.Blocks.Count == 0) d.Blocks.Add(ScriptBlock.Paragraph(""));
        return d;
    }
}
