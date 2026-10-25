using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Arca.Core.Net;
using Arca.Core.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Arca.Server;

/// <summary>Comando recebido de um navegador.</summary>
public sealed record RemoteCommand(string Name, double? Value);

/// <summary>
/// Servidor do monitor web e do PWA (iPad): HTTP + WebSocket na rede local, via Kestrel.
/// Porta HTTP (padrão 8420) e, opcionalmente, HTTPS na porta seguinte com certificado autoassinado
/// gerado na primeira execução (necessário para o modo offline do PWA no iPad).
/// O app publica: <see cref="PublishHtml"/>, <see cref="PublishState"/> e <see cref="PublishPosition"/>.
/// </summary>
public sealed class PrompterServer : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private string? _html, _state, _pos;
    private string _password = "";
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

    public int Port { get; private set; }
    public int? HttpsPort { get; private set; }
    public bool Running => _app != null;
    /// <summary>Navegadores podem enviar comandos (play, velocidade…).</summary>
    public bool AllowControl { get; set; }
    public string Password { get => _password; set => SetPassword(value ?? ""); }
    public int Viewers => _clients.Values.Count(c => c.Authed);

    public event Action<RemoteCommand>? Command;
    public event Action<int>? ViewersChanged;

    private sealed class Client
    {
        public required WebSocket Socket { get; init; }
        public bool Authed { get; set; }
        public readonly SemaphoreSlim Send = new(1, 1);
        public string? Agent { get; init; }
    }

    public async Task StartAsync(int port, bool https)
    {
        if (_app != null) return;
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k =>
        {
            k.Listen(IPAddress.Any, port);
            if (https)
            {
                var cert = Certificate();
                k.Listen(IPAddress.Any, port + 1, o => o.UseHttps(cert));
            }
        });
        var app = builder.Build();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });
        var files = new ManifestEmbeddedFileProvider(typeof(PrompterServer).Assembly, "wwwroot");
        app.Use(async (ctx, next) =>
        {
            // Monitor e PWA não devem ser guardados em cache desatualizado (o app pode ter sido atualizado).
            ctx.Response.Headers.CacheControl = "no-cache";
            await next();
        });
        app.MapGet("/monitor", ctx => { ctx.Response.Redirect("/"); return Task.CompletedTask; });
        app.MapGet("/api/info", () => Results.Json(new { app = "ARCA Teleprompter", viewers = Viewers, control = AllowControl, needPass = _password.Length > 0, https = HttpsPort }));
        app.MapGet("/certificado.cer", () => https ? Results.File(Certificate().Export(X509ContentType.Cert), "application/x-x509-ca-cert", "ARCA-Teleprompter.cer") : Results.NotFound());
        app.Map("/ws", HandleSocket);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        var types = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        types.Mappings[".webmanifest"] = "application/manifest+json";
        app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = types });
        await app.StartAsync();
        _app = app; Port = port; HttpsPort = https ? port + 1 : null;
        Log.Info($"Monitor web ativo na porta {port}" + (https ? $" e https {port + 1}" : ""));
    }

    public async Task StopAsync()
    {
        var app = _app; _app = null;
        foreach (var c in _clients.Values) { try { c.Socket.Abort(); } catch { /* */ } }
        _clients.Clear(); ViewersChanged?.Invoke(0);
        if (app != null) { await app.StopAsync(); await app.DisposeAsync(); }
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>Endereços para digitar no iPad/celular (IPv4 da rede local, os privados primeiro).</summary>
    public static List<string> LanAddresses()
    {
        var list = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var a in ni.GetIPProperties().UnicastAddresses)
                if (a.Address.AddressFamily == AddressFamily.InterNetwork) list.Add(a.Address.ToString());
        }
        static bool Private(string ip) => ip.StartsWith("192.168.") || ip.StartsWith("10.") || (ip.StartsWith("172.") && int.TryParse(ip.Split('.')[1], out var b) && b is >= 16 and <= 31);
        return list.Distinct().OrderBy(ip => Private(ip) ? 0 : 1).ToList();
    }

    // ——— Publicação (thread-safe; chamadas da thread da interface) ———
    public void PublishHtml(string html) { _html = Json(new JsonObject { ["t"] = "html", ["html"] = html }); Broadcast(_html); }
    public void PublishState(PrompterSnapshot s) { _state = JsonSerializer.Serialize(new { t = "state", s }); Broadcast(_state); }
    /// <summary>Posição de leitura (até ~30×/s). Vai com o relógio do servidor para o cliente compensar o atraso da rede.</summary>
    /// <param name="y">Posição, em unidades do espaço de 1000.</param>
    /// <param name="velocity">Velocidade em unidades/s (o cliente extrapola entre mensagens).</param>
    /// <param name="block">Índice do bloco (data-i) sob a linha de leitura (−1 = usar só y).</param>
    /// <param name="offset">Distância, em unidades, do topo do bloco até a linha de leitura.</param>
    /// <param name="frac">Fração (0–1) do bloco já passada da linha de leitura — independe da diagramação do navegador.</param>
    /// <param name="blockHeight">Altura do bloco no app (para o cliente converter a velocidade para a sua diagramação).</param>
    public void PublishPosition(double y, double velocity, int block, double offset, double frac = -1, double blockHeight = 0)
    {
        // "ts": relógio do servidor (ms) — o cliente compensa a variação de atraso da rede (Wi-Fi) com ele.
        _pos = Json(new JsonObject { ["t"] = "y", ["y"] = Math.Round(y, 2), ["v"] = Math.Round(velocity, 2), ["b"] = block, ["o"] = Math.Round(offset, 2), ["f"] = Math.Round(frac, 5), ["bh"] = Math.Round(blockHeight, 2), ["ts"] = Math.Round(_clock.Elapsed.TotalMilliseconds, 1) });
        Broadcast(_pos);
    }
    public void Flash(string text, string? sub = null) => Broadcast(Json(new JsonObject { ["t"] = "flash", ["text"] = text, ["sub"] = sub }));

    private static string Json(JsonObject o) => o.ToJsonString();

    private void Broadcast(string msg)
    {
        if (_clients.IsEmpty) return;
        var bytes = Encoding.UTF8.GetBytes(msg);
        foreach (var c in _clients.Values) if (c.Authed) _ = SendAsync(c, bytes, dropIfBusy: msg.StartsWith("{\"t\":\"y\""));
    }

    private static async Task SendAsync(Client c, byte[] data, bool dropIfBusy = false)
    {
        // Posições chegam a cada quadro: se o cliente está lento, descarta em vez de acumular.
        if (dropIfBusy) { if (!await c.Send.WaitAsync(0)) return; }
        else await c.Send.WaitAsync();
        try { if (c.Socket.State == WebSocketState.Open) await c.Socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None); }
        catch { /* desconectou */ }
        finally { c.Send.Release(); }
    }

    private void SetPassword(string p)
    {
        if (p == _password) return;
        _password = p;
        // Senha nova: todos precisam entrar de novo.
        foreach (var c in _clients.Values)
        {
            c.Authed = p.Length == 0;
            _ = SendAsync(c, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { t = "auth", ok = c.Authed, needPass = p.Length > 0 })));
            if (c.Authed) Replay(c);
        }
        ViewersChanged?.Invoke(Viewers);
    }

    private void Replay(Client c)
    {
        foreach (var m in new[] { _html, _state, _pos }) if (m != null) _ = SendAsync(c, Encoding.UTF8.GetBytes(m));
    }

    private async Task HandleSocket(HttpContext ctx)
    {
        if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
        using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();
        var client = new Client { Socket = ws, Agent = ctx.Request.Headers.UserAgent };
        _clients[id] = client;
        var buf = new byte[16 * 1024];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var sb = new StringBuilder();
                WebSocketReceiveResult r;
                do
                {
                    r = await ws.ReceiveAsync(buf, ctx.RequestAborted);
                    if (r.MessageType == WebSocketMessageType.Close) return;
                    sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                    if (sb.Length > 64 * 1024) return; // mensagem grande demais: cliente inválido
                } while (!r.EndOfMessage);
                OnMessage(client, sb.ToString());
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException) { /* desconectou */ }
        finally
        {
            _clients.TryRemove(id, out _);
            ViewersChanged?.Invoke(Viewers);
        }
    }

    private void OnMessage(Client c, string json)
    {
        JsonNode? m;
        try { m = JsonNode.Parse(json); } catch (JsonException) { return; }
        var t = m?["t"]?.GetValue<string>();
        if (t == "auth")
        {
            var pass = m?["pass"]?.GetValue<string>() ?? "";
            var ok = _password.Length == 0 || CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(pass), Encoding.UTF8.GetBytes(_password));
            c.Authed = ok;
            _ = SendAsync(c, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { t = "auth", ok, needPass = _password.Length > 0 })));
            if (ok) Replay(c);
            ViewersChanged?.Invoke(Viewers);
            return;
        }
        if (t == "cmd" && c.Authed && AllowControl)
        {
            var name = m?["name"]?.GetValue<string>() ?? "";
            if (!RemoteCommands.Allowed.Contains(name)) return;
            double? val = null;
            try { if (m?["value"] is JsonValue v && v.TryGetValue<double>(out var d) && double.IsFinite(d)) val = d; } catch { /* */ }
            Command?.Invoke(new RemoteCommand(name, val));
        }
    }

    /// <summary>Certificado autoassinado (RSA 2048, 5 anos) guardado em %APPDATA%\ARCA Teleprompter\https.pfx.</summary>
    public static X509Certificate2 Certificate()
    {
        var path = Path.Combine(AppPaths.Data, "https.pfx");
        if (File.Exists(path))
        {
            try { var c = new X509Certificate2(path, "arca", X509KeyStorageFlags.Exportable); if (c.NotAfter > DateTime.Now.AddDays(30)) return c; } catch { /* recria */ }
        }
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=ARCA Teleprompter", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost"); san.AddDnsName(Environment.MachineName);
        foreach (var ip in LanAddresses()) san.AddIpAddress(IPAddress.Parse(ip));
        san.AddIpAddress(IPAddress.Loopback);
        req.CertificateExtensions.Add(san.Build());
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
        Directory.CreateDirectory(AppPaths.Data);
        File.WriteAllBytes(path, cert.Export(X509ContentType.Pfx, "arca"));
        return new X509Certificate2(path, "arca", X509KeyStorageFlags.Exportable);
    }
}
