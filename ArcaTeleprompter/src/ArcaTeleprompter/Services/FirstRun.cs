using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using Arca.Core.Storage;

namespace Arca.App.Services;

/// <summary>
/// Primeira execução depois de instalar ou atualizar.
/// <list type="bullet">
/// <item><b>Instalação nova</b> (o instalador grava <c>Fresh: true</c> no install.json): dados que sobraram de antes
///   (sessão, histórico, preferências) vão para <c>%APPDATA%\ARCA Teleprompter\anterior-AAAAMMDD-HHMM</c> — o app
///   abre limpo, com a tela de boas-vindas. Nada é apagado.</item>
/// <item><b>Atualização</b>: mantém tudo e mostra as novidades da versão.</item>
/// <item>Primeira vez neste usuário (sem configurações): boas-vindas.</item>
/// </list>
/// </summary>
public sealed class FirstRun
{
    private WelcomeKind? _welcome;
    private string? _from;
    private long _stamp;

    public static FirstRun Check()
    {
        var r = new FirstRun();
        try
        {
            var firstEver = !File.Exists(AppPaths.Settings);
            var settings = Stores.LoadSettings();
            var info = ReadInstall();
            if (info is { } i && i.Stamp != settings.LastInstallStamp)
            {
                r._stamp = i.Stamp;
                if (i.Fresh)
                {
                    Archive();
                    r._welcome = WelcomeKind.First;
                }
                else
                {
                    r._welcome = WelcomeKind.Upgrade;
                    r._from = string.IsNullOrEmpty(settings.LastVersion) ? null : settings.LastVersion;
                }
            }
            else if (firstEver) r._welcome = WelcomeKind.First;
            else if (settings.LastVersion != MainWindow.AppVersion && settings.LastVersion.Length > 0) { r._welcome = WelcomeKind.Upgrade; r._from = settings.LastVersion; }
        }
        catch (Exception e) { Log.Error("Primeira execução", e); }
        return r;
    }

    /// <summary>Grava o que já foi visto e mostra as boas-vindas (depois que a janela abriu).</summary>
    public void Finish(MainWindow w, AppSettings settings)
    {
        var changed = settings.LastVersion != MainWindow.AppVersion || (_stamp != 0 && settings.LastInstallStamp != _stamp);
        settings.LastVersion = MainWindow.AppVersion;
        if (_stamp != 0) settings.LastInstallStamp = _stamp;
        if (changed) Stores.SaveSettings(settings);
        if (_welcome is { } k) w.Dispatcher.BeginInvoke(() => w.ShowWelcome(k, _from), DispatcherPriority.ApplicationIdle);
    }

    private static (long Stamp, bool Fresh)? ReadInstall()
    {
        var f = Path.Combine(AppContext.BaseDirectory, "install.json");
        if (!File.Exists(f)) return null;
        using var d = JsonDocument.Parse(File.ReadAllText(f));
        var root = d.RootElement;
        var at = root.TryGetProperty("InstalledAt", out var a) && a.TryGetDateTime(out var dt) ? dt.Ticks : 0;
        var fresh = root.TryGetProperty("Fresh", out var fr) && fr.ValueKind == JsonValueKind.True;
        return (at, fresh);
    }

    /// <summary>Move os dados de antes (exceto o certificado do monitor web) para uma pasta "anterior-…".</summary>
    private static void Archive()
    {
        var dir = AppPaths.Data;
        if (!Directory.Exists(dir)) return;
        var keep = new[] { "https.pfx" };
        var items = Directory.EnumerateFileSystemEntries(dir)
            .Where(p => !keep.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase) && !Path.GetFileName(p).StartsWith("anterior-", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (items.Count == 0) return;
        var dest = Path.Combine(dir, "anterior-" + DateTime.Now.ToString("yyyyMMdd-HHmm"));
        Directory.CreateDirectory(dest);
        foreach (var p in items)
        {
            try
            {
                var to = Path.Combine(dest, Path.GetFileName(p));
                if (Directory.Exists(p)) Directory.Move(p, to); else File.Move(p, to);
            }
            catch (Exception e) { Log.Error("Arquivar " + p, e); }
        }
        Log.Info("Instalação nova: dados anteriores guardados em " + dest);
    }
}
