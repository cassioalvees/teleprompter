using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using Arca.App.Controls;
using Arca.Core.Storage;

namespace Arca.App.Services;

/// <summary>Versão publicada (arquivo <c>latest.json</c> anexado a cada versão no GitHub Releases).</summary>
public sealed class UpdateInfo
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    /// <summary>Endereço do instalador (absoluto, ou relativo ao endereço do latest.json).</summary>
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";
    [JsonPropertyName("notes")] public string Notes { get; set; } = "";
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonIgnore] public Uri? DownloadUri { get; set; }
}

/// <summary>
/// Atualização automática: ao abrir o app (e em Ajuda › Procurar atualizações) consulta o <c>latest.json</c> da última
/// versão no GitHub. Havendo versão nova, pergunta — "Atualizar agora" baixa o instalador numa tela própria, confere o
/// SHA-256, fecha o app e roda o instalador em modo atualização (o Windows pede a permissão de administrador quando a
/// pasta exige); "Lembrar depois" pergunta de novo na próxima vez que o app abrir. Nunca interrompe o prompt rodando.
/// </summary>
public sealed class UpdateService
{
    /// <summary>Última versão publicada. <c>ARCA_UPDATE_URL</c> troca o endereço (testes com servidor local).</summary>
    public const string DefaultUrl = "https://github.com/cassioalvees/teleprompter/releases/latest/download/latest.json";
    public static string Url => Environment.GetEnvironmentVariable("ARCA_UPDATE_URL") is { Length: > 0 } u ? u : DefaultUrl;

    private readonly MainWindow _main;
    private bool _asking;
    public UpdateService(MainWindow main) { _main = main; }

    public static Version Current => typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0);
    private static string Folder => Path.Combine(AppPaths.Local, "updates");

    /// <summary>Consulta a última versão. Null se não há internet, o arquivo não existe ou é inválido.</summary>
    public static async Task<UpdateInfo?> FetchAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"ARCA-Teleprompter/{Current}");
        using var resp = await http.GetAsync(Url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var info = JsonSerializer.Deserialize<UpdateInfo>(await resp.Content.ReadAsStringAsync(ct));
        if (info == null || !System.Version.TryParse(info.Version, out _) || string.IsNullOrWhiteSpace(info.Url) || info.Sha256.Length != 64) return null;
        // O GitHub redireciona "latest/download" para a versão: endereços relativos valem a partir do endereço final.
        // Nome relativo: monta a partir do endereço ORIGINAL do latest.json. O endereço final do redirecionamento
        // do GitHub é temporário e assinado (?jwt=…); montar a partir dele perdia a assinatura (erro 618).
        var baseUri = new Uri(Url);
        info.DownloadUri = new Uri(baseUri, info.Url);
        return info;
    }

    /// <summary>Ao abrir o app: espera um pouco (não disputa com a abertura) e consulta em segundo plano.</summary>
    public void CheckOnStartup()
    {
        CleanOld();
        if (!_main.Vm.App.UpdateCheck) return;
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        t.Tick += async (_, _) => { t.Stop(); await Check(manual: false); };
        t.Start();
    }

    /// <summary>Consulta e, havendo versão nova, pergunta. <paramref name="manual"/>: também avisa "já está atualizado" e erros.</summary>
    public async Task Check(bool manual)
    {
        if (_asking) return;
        UpdateInfo? info;
        try { info = await FetchAsync(); }
        catch (Exception e)
        {
            Log.Info("Atualização: sem resposta (" + e.Message + ")");
            if (manual) ArcaDialog.Info(_main, "Atualização", "Não foi possível procurar atualizações", "Confira a conexão com a internet e tente de novo.");
            return;
        }
        var v = info != null && System.Version.TryParse(info.Version, out var pv) ? pv : null;
        if (info == null || v == null || v <= Current)
        {
            Log.Info($"Atualização: nenhuma versão nova (instalada {Current.ToString(3)}{(info != null ? ", publicada " + info.Version : "")})");
            if (manual) ArcaDialog.Info(_main, "Atualização", "Você está na versão mais recente", $"ARCA Teleprompter {Current.ToString(3)}.");
            return;
        }
        Log.Info($"Atualização: versão {info.Version} disponível");
        // Nunca no meio do prompt, com a voz ligada ou com outro diálogo aberto: espera ficar livre.
        _asking = true;
        while (!_main.IsIdleForPrompt) await Task.Delay(3000);
        try { Ask(info); } finally { _asking = false; }
    }

    private void Ask(UpdateInfo info)
    {
        var notes = info.Notes.Trim();
        if (notes.Length > 700) notes = notes[..700].TrimEnd() + "…";
        var detail = $"Você está na versão {Current.ToString(3)}. Seus roteiros e preferências são mantidos."
            + (notes.Length > 0 ? "\n\n" + notes : "")
            + (info.Size > 0 ? $"\n\nDownload: {info.Size / 1048576.0:0} MB." : "");
        var r = ArcaDialog.Ask(_main, "Atualização disponível", $"ARCA Teleprompter {info.Version}", detail, new[] { "Lembrar depois", "Atualizar agora" }, 1, 0);
        if (r != 1) { Log.Info("Atualização: lembrar depois"); return; }
        var file = new UpdateDownloadWindow(_main, info).Run();
        if (file == null) return;
        Launch(file);
    }

    /// <summary>Roda o instalador em modo atualização e fecha o app (o instalador reabre o app no fim).</summary>
    private void Launch(string file)
    {
        try
        {
            Log.Info("Atualização: iniciando " + Path.GetFileName(file));
            Process.Start(new ProcessStartInfo(file, "--update") { UseShellExecute = true });
            _main.Close();
        }
        catch (Exception e)
        {
            Log.Error("Atualização", e);
            ArcaDialog.Info(_main, "Atualização", "Não foi possível iniciar a atualização", e.Message, danger: true);
        }
    }

    /// <summary>Instaladores de versões já instaladas não servem mais.</summary>
    private static void CleanOld()
    {
        try
        {
            if (!Directory.Exists(Folder)) return;
            foreach (var f in Directory.GetFiles(Folder))
                if (File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-1)) File.Delete(f);
        }
        catch { /* em uso ou sem permissão: fica para a próxima */ }
    }

    /// <summary>Baixa (com SHA-256 calculado no caminho) para a pasta de atualizações. Devolve o arquivo conferido.</summary>
    public static async Task<string> Download(UpdateInfo info, IProgress<(long Done, long Total)> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(Folder);
        var name = string.Join("_", (info.File.Length > 0 ? info.File : $"ARCA-Teleprompter-Setup-{info.Version}.exe").Split(Path.GetInvalidFileNameChars()));
        var target = Path.Combine(Folder, name); var part = target + ".part";
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"ARCA-Teleprompter/{Current}");
        using var resp = await http.GetAsync(info.DownloadUri!, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? info.Size;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using (var src = await resp.Content.ReadAsStreamAsync(ct))
        await using (var dst = File.Create(part))
        {
            var buf = new byte[1 << 16]; long done = 0; int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n), ct);
                sha.AppendData(buf, 0, n);
                done += n; progress.Report((done, total));
            }
        }
        var hash = Convert.ToHexString(sha.GetHashAndReset());
        if (!hash.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(part);
            throw new InvalidDataException("O arquivo baixado não confere (SHA-256 diferente). Nada foi instalado — tente de novo.");
        }
        if (File.Exists(target)) File.Delete(target);
        File.Move(part, target);
        return target;
    }
}

/// <summary>Tela de download da atualização: progresso, velocidade e tempo restante; começa sozinha e segue sozinha.</summary>
public sealed class UpdateDownloadWindow
{
    private readonly Window _owner; private readonly UpdateInfo _info;
    public UpdateDownloadWindow(Window owner, UpdateInfo info) { _owner = owner; _info = info; }

    /// <summary>Mostra a tela, baixa e confere. Devolve o instalador baixado, ou null se cancelou ou falhou.</summary>
    public string? Run()
    {
        var bar = new System.Windows.Controls.ProgressBar { Height = 6, Minimum = 0, Maximum = 1, Margin = new Thickness(0, 4, 0, 8), BorderThickness = new Thickness(0),
            Foreground = Application.Current.TryFindResource("Accent") as System.Windows.Media.Brush, Background = Application.Current.TryFindResource("Line") as System.Windows.Media.Brush };
        var status = new System.Windows.Controls.TextBlock { Text = "Conectando…", FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = Application.Current.TryFindResource("Muted") as System.Windows.Media.Brush };
        var panel = new System.Windows.Controls.StackPanel(); panel.Children.Add(bar); panel.Children.Add(status);
        var cts = new CancellationTokenSource();
        string? result = null; var finished = false;   // depois do fim, avisos de progresso atrasados não sobrescrevem a mensagem
        var dlg = ArcaDialog.Create(_owner, "Atualização", $"Baixando a versão {_info.Version}", "Ao terminar, o app fecha e a atualização começa sozinha.",
            new[] { "Cancelar" }, 0, 0, panel, false, out var close);
        var sw = Stopwatch.StartNew();
        var prog = new Progress<(long Done, long Total)>(p =>
        {
            if (finished) return;
            var t = Math.Max(1, p.Total); bar.Value = (double)p.Done / t;
            var s = sw.Elapsed.TotalSeconds; var rate = s > 0.5 ? p.Done / s : 0;
            var left = rate > 0 ? TimeSpan.FromSeconds((t - p.Done) / rate) : TimeSpan.Zero;
            status.Text = $"{p.Done / 1048576.0:0} de {t / 1048576.0:0} MB" + (rate > 0 ? $" · {rate / 1048576.0:0.0} MB/s · {(left.TotalMinutes >= 1 ? $"{(int)left.TotalMinutes} min {left.Seconds:00} s" : $"{left.Seconds} s")} restantes" : "");
        });
        dlg.Loaded += async (_, _) =>
        {
            try
            {
                result = await UpdateService.Download(_info, prog, cts.Token);
                finished = true;
                status.Text = "Download conferido. Iniciando a atualização…"; bar.Value = 1;
                await Task.Delay(700);
                close(1);
            }
            catch (OperationCanceledException) { close(0); }
            catch (Exception e)
            {
                Log.Error("Atualização (download)", e);
                finished = true;
                bar.Foreground = Application.Current.TryFindResource("Live") as System.Windows.Media.Brush;
                status.Text = "Falhou: " + e.Message;
                status.Foreground = Application.Current.TryFindResource("Live") as System.Windows.Media.Brush;
            }
        };
        var r = ArcaDialog.ShowCreated(dlg);
        if (r == 0) { cts.Cancel(); Log.Info("Atualização: download cancelado"); return null; }
        return result;
    }
}
