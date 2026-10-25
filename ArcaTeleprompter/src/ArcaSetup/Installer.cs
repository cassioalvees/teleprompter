using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;

namespace Arca.Setup;

/// <summary>Escopo da instalação.</summary>
public enum Scope { AllUsers, CurrentUser }

/// <summary>Opções escolhidas no assistente.</summary>
public sealed class InstallOptions
{
    public string Path { get; set; } = "";
    public Scope Scope { get; set; } = Scope.AllUsers;
    public bool Voice { get; set; } = true;
    public bool Web { get; set; } = true;
    public bool Samples { get; set; } = true;
    public bool Ndi { get; set; }
    public bool StartMenu { get; set; } = true;
    public bool Desktop { get; set; } = true;
    public bool Associate { get; set; } = true;
}

/// <summary>O que foi instalado (gravado em install.json para o desinstalador desfazer exatamente isso).</summary>
public sealed class Manifest
{
    public string Version { get; set; } = "";
    public string Path { get; set; } = "";
    public Scope Scope { get; set; }
    public List<string> Shortcuts { get; set; } = new();
    public bool Associated { get; set; }
    public bool Firewall { get; set; }
    public string? SamplesDir { get; set; }
    public DateTime InstalledAt { get; set; }
    /// <summary>Instalação nova (não havia versão instalada): o app abre limpo na primeira vez.</summary>
    public bool Fresh { get; set; }
}

/// <summary>Progresso para a interface.</summary>
public sealed record Progress(string? Task = null, string? File = null, double? Pct = null, string? Log = null, bool Warn = false);

/// <summary>
/// Operações reais de instalação e desinstalação do ARCA Teleprompter.
/// O pacote (payload.zip, embutido neste executável) tem as pastas app/, models/, samples/ e docs/.
/// </summary>
public static class Installer
{
    public const string Product = "ARCA Teleprompter";
    public const string Exe = "ArcaTeleprompter.exe";
    /// <summary>Desinstalador (este mesmo programa, compilado sem o pacote) que vai na pasta do app.</summary>
    public const string Uninstaller = "ArcaUninstall.exe";
    public const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ARCATeleprompter";
    public const string ProgId = "ARCA.Script";
    public const string FwRule = "ARCA Teleprompter - Monitor web";
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.0.0";

    public static bool IsAdmin { get { using var id = WindowsIdentity.GetCurrent(); return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); } }

    /// <summary>
    /// Pasta aceitável para instalar: caminho completo de disco, fora da pasta do Windows, e nunca a raiz do disco, a
    /// pasta de usuários ou a própria "Arquivos de Programas" (o instalador escreveria lá dentro como administrador).
    /// </summary>
    public static bool SafeTarget(string p)
    {
        try
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(p.Trim(), @"^[a-zA-Z]:\\[^<>:""|?*]*$")) return false;
            var full = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(p.Trim()));
            bool Under(Environment.SpecialFolder f) { var d = Environment.GetFolderPath(f); return d.Length > 0 && (full.Equals(d, StringComparison.OrdinalIgnoreCase) || full.StartsWith(d + "\\", StringComparison.OrdinalIgnoreCase)); }
            bool Is(Environment.SpecialFolder f) { var d = Environment.GetFolderPath(f); return d.Length > 0 && full.Equals(System.IO.Path.TrimEndingDirectorySeparator(d), StringComparison.OrdinalIgnoreCase); }
            if (full.Length <= 3) return false;                                   // raiz do disco
            if (Under(Environment.SpecialFolder.Windows)) return false;           // C:\Windows e tudo dentro
            if (Is(Environment.SpecialFolder.ProgramFiles) || Is(Environment.SpecialFolder.ProgramFilesX86) || Is(Environment.SpecialFolder.CommonApplicationData)) return false;
            if (Is(Environment.SpecialFolder.UserProfile) || full.Equals(System.IO.Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
        catch { return false; }
    }

    public static string DefaultPath(Scope s) => s == Scope.AllUsers
        ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Product)
        : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", Product);

    /// <summary>A pasta precisa de administrador? (fora do perfil do usuário e sem permissão de escrita).</summary>
    /// <summary>Precisa de UAC: instalação para todos os usuários (HKLM, menu Iniciar comum) ou pasta sem permissão de escrita.</summary>
    public static bool NeedsAdmin(string path, Scope scope)
    {
        if (IsAdmin) return false;
        if (scope == Scope.AllUsers) return true;
        try
        {
            var dir = path;
            while (!Directory.Exists(dir)) { var p = System.IO.Path.GetDirectoryName(dir); if (p == null || p == dir) break; dir = p; }
            var probe = System.IO.Path.Combine(dir, ".arca-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, ""); File.Delete(probe);
            return false;
        }
        catch { return true; }
    }

    // ——— Pacote ———
    public static Stream? Payload() => Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
        ?? (File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "payload.zip")) ? File.OpenRead(System.IO.Path.Combine(AppContext.BaseDirectory, "payload.zip")) : null);

    public static (long App, long Voice, long Samples) PayloadSizes()
    {
        using var s = Payload(); if (s == null) return (0, 0, 0);
        using var z = new ZipArchive(s, ZipArchiveMode.Read);
        long a = 0, v = 0, sm = 0;
        foreach (var e in z.Entries)
        {
            var n = e.FullName.Replace('\\', '/');
            if (n.StartsWith("models/")) v += e.Length;
            else if (n.StartsWith("samples/")) sm += e.Length;
            else a += e.Length;
        }
        return (a, v, sm);
    }

    public static Manifest? Installed()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var k = hive.OpenSubKey(RegKey);
            if (k?.GetValue("InstallLocation") is string loc)
            {
                var f = System.IO.Path.Combine(loc, "install.json");
                if (File.Exists(f)) try { return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(f)); } catch { }
                return new Manifest { Path = loc, Scope = hive == Registry.LocalMachine ? Scope.AllUsers : Scope.CurrentUser, Version = k.GetValue("DisplayVersion") as string ?? "" };
            }
        }
        return null;
    }

    public static bool AppRunning() => Process.GetProcessesByName("ArcaTeleprompter").Length > 0;

    /// <summary>
    /// Processos do ARCA (app ou desinstalador) que podem estar prendendo arquivos da pasta — inclusive um
    /// desinstalador antigo esquecido aberto. Quando o caminho do processo não pode ser lido (processo elevado),
    /// conta pelo nome. Nunca inclui este próprio processo.
    /// </summary>
    public static List<Process> Blocking(string? dir)
    {
        var me = Environment.ProcessId; var list = new List<Process>();
        foreach (var name in new[] { "ArcaTeleprompter", "ArcaUninstall" })
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.Id == me) continue;
                string? path = null; try { path = p.MainModule?.FileName; } catch { }
                if (dir == null || path == null || path.StartsWith(System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(dir)) + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) list.Add(p);
            }
        return list;
    }

    public static async Task<bool> CloseApp(string? dir = null)
    {
        foreach (var p in Process.GetProcessesByName("ArcaTeleprompter").Concat(dir != null ? Blocking(dir) : Enumerable.Empty<Process>()).DistinctBy(p => p.Id))
        {
            try { p.CloseMainWindow(); if (!p.WaitForExit(4000)) { p.Kill(true); p.WaitForExit(3000); } } catch { }
        }
        await Task.Delay(500);
        return !AppRunning() && (dir == null || Blocking(dir).Count == 0);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existing, string? newName, int flags);
    private const string OldSuffix = ".arca-old";

    /// <summary>
    /// Repete por alguns segundos quando o arquivo está preso por outro programa (antivírus, indexador, um app
    /// terminando de fechar). Se continuar preso, explica qual programa está usando o arquivo.
    /// </summary>
    private static async Task<T> Retry<T>(Func<T> op, string path, CancellationToken ct)
    {
        for (var i = 0; ; i++)
        {
            try { return op(); }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) is 32 or 33) // compartilhamento / bloqueio
            {
                if (i >= 24) throw new IOException(Locked(path), ex);
                await Task.Delay(i < 4 ? 100 : 400, ct);
            }
        }
    }

    private static string Locked(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        var who = Holders(path).Where(p => p.Pid != Environment.ProcessId).Select(p => p.Name).Distinct().ToList();
        return who.Count > 0
            ? $"O arquivo {name} está aberto em: {string.Join(", ", who)}. Feche esse programa e clique em Tentar de novo."
            : $"O arquivo {name} está sendo usado por outro programa (às vezes o antivírus conferindo a pasta). Aguarde alguns segundos e clique em Tentar de novo.";
    }

    // Gerenciador de Reinicialização do Windows: diz quais processos estão com um arquivo aberto.
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RmUniqueProcess { public int Pid; public System.Runtime.InteropServices.ComTypes.FILETIME Start; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct RmProcessInfo
    {
        public RmUniqueProcess Process;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 256)] public string AppName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 64)] public string ServiceName;
        public int AppType; public uint Status; public uint Session;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] public bool Restartable;
    }
    [System.Runtime.InteropServices.DllImport("rstrtmgr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int RmStartSession(out uint session, int flags, System.Text.StringBuilder key);
    [System.Runtime.InteropServices.DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint session);
    [System.Runtime.InteropServices.DllImport("rstrtmgr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int RmRegisterResources(uint session, uint files, string[] names, uint apps, IntPtr appList, uint services, string[]? serviceNames);
    [System.Runtime.InteropServices.DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint session, out uint needed, ref uint count, [System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] RmProcessInfo[]? list, ref uint reasons);

    /// <summary>Processos com o arquivo aberto (nome amigável e PID). Vazio se o Windows não souber dizer.</summary>
    public static List<(int Pid, string Name)> Holders(string path)
    {
        var r = new List<(int, string)>();
        if (RmStartSession(out var s, 0, new System.Text.StringBuilder(33)) != 0) return r;
        try
        {
            if (RmRegisterResources(s, 1, new[] { path }, 0, IntPtr.Zero, 0, null) != 0) return r;
            uint n = 0, reasons = 0;
            var err = RmGetList(s, out var needed, ref n, null, ref reasons);
            if (err != 234 /* ERROR_MORE_DATA */ || needed == 0) return r;
            var list = new RmProcessInfo[needed]; n = needed;
            if (RmGetList(s, out _, ref n, list, ref reasons) != 0) return r;
            foreach (var p in list.Take((int)n))
            {
                var name = p.AppName;
                try { using var pr = Process.GetProcessById(p.Process.Pid); if (string.IsNullOrWhiteSpace(name)) name = pr.ProcessName; } catch { }
                r.Add((p.Process.Pid, string.IsNullOrWhiteSpace(name) ? $"processo {p.Process.Pid}" : name));
            }
        }
        catch { }
        finally { RmEndSession(s); }
        return r;
    }

    /// <summary>Apaga sobras de atualizações anteriores (arquivos renomeados porque estavam em uso).</summary>
    private static void SweepOld(string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var f in Directory.EnumerateFiles(dir, "*" + OldSuffix + "*", SearchOption.AllDirectories)) { try { File.Delete(f); } catch { } }
    }

    // ——— Instalação ———
    public static async Task Install(InstallOptions o, IProgress<Progress> ui, CancellationToken ct)
    {
        var target = o.Path.TrimEnd('\\', '/');
        var undo = new Stack<Action>();
        var createdDir = !Directory.Exists(target);
        void Step(string t, string f = "", double? p = null) => ui.Report(new Progress(t, f, p));
        void Ok(string l, bool warn = false) => ui.Report(new Progress(Log: l, Warn: warn));
        try
        {
            Step("Preparando a instalação", "Verificando espaço em disco e permissões", 1);
            using var payload = Payload() ?? throw new InvalidOperationException("Pacote de instalação não encontrado (payload.zip).");
            using var zip = new ZipArchive(payload, ZipArchiveMode.Read);
            // Zips criados no Windows PowerShell 5 usam "\" nos caminhos: normaliza para "/".
            static string N(ZipArchiveEntry e) => e.FullName.Replace('\\', '/');
            var entries = zip.Entries.Where(e => e.Length > 0 && (N(e).StartsWith("app/") || N(e).StartsWith("docs/") || (o.Voice && N(e).StartsWith("models/")))).ToList();
            if (!entries.Any(e => N(e).Equals("app/" + Exe, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("Pacote de instalação incompleto (o aplicativo não está no pacote).");
            var total = entries.Sum(e => e.Length);
            var drive = new DriveInfo(System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(target))!);
            if (drive.AvailableFreeSpace < total * 1.1) throw new IOException($"Espaço insuficiente em {drive.Name} (precisa de {total / (1 << 20)} MB).");
            if (AppRunning() || Blocking(target).Count > 0)
            {
                Step("Fechando o ARCA Teleprompter", "O app ou o desinstalador estava aberto");
                if (!await CloseApp(target)) Ok("Um programa do ARCA continua aberto; arquivos em uso serão substituídos mesmo assim", true);
            }
            SweepOld(target);
            Directory.CreateDirectory(target);
            if (createdDir) undo.Push(() => Directory.Delete(target, true));
            Ok("Preparando a instalação");

            // Cópia com progresso (3% a 82%). Cada arquivo que já existe é renomeado antes de gravar o novo:
            // o Windows permite renomear até uma DLL carregada, e se algo falhar a versão anterior volta inteira.
            long done = 0;
            var backups = new List<string>();
            foreach (var e in entries)
            {
                ct.ThrowIfCancellationRequested();
                var rel = N(e).StartsWith("app/") ? N(e)[4..] : N(e); // app/* vai para a raiz
                var dst = System.IO.Path.GetFullPath(System.IO.Path.Combine(target, rel.Replace('/', '\\')));
                if (!dst.StartsWith(System.IO.Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) continue; // proteção contra "zip slip"
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst)!);
                if (File.Exists(dst))
                {
                    var old = dst + OldSuffix + Guid.NewGuid().ToString("N")[..6];
                    await Retry(() => { File.Move(dst, old); return true; }, dst, ct);
                    backups.Add(old);
                    undo.Push(() => { if (File.Exists(dst)) File.Delete(dst); File.Move(old, dst); });
                }
                else undo.Push(() => File.Delete(dst));
                var fs = await Retry(() => new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, true), dst, ct);
                await using (var src = e.Open())
                await using (fs)
                    await src.CopyToAsync(fs, ct);
                done += e.Length;
                Step(N(e).StartsWith("models/") ? "Instalando o modelo de voz pt-BR" : "Copiando o aplicativo", dst, 3 + 79 * done / Math.Max(1, total));
            }
            Ok("Copiando o aplicativo");
            if (backups.Count > 0)
            {
                // A versão anterior só sai depois de tudo copiado. O que continuar preso sai na reinicialização (ou na próxima instalação).
                var left = 0;
                foreach (var f in backups) { try { File.Delete(f); } catch { left++; if (IsAdmin) MoveFileEx(f, null, 4 /* MOVEFILE_DELAY_UNTIL_REBOOT */); } }
                backups.Clear();
                if (left > 0) Ok($"{left} arquivo(s) antigo(s) em uso serão removidos ao reiniciar o Windows", true);
            }
            if (o.Voice) Ok("Modelo de voz offline (Vosk pt-BR) instalado");

            var exe = System.IO.Path.Combine(target, Exe);
            var man = new Manifest { Version = Version, Path = target, Scope = o.Scope, InstalledAt = DateTime.Now, Fresh = Installed() == null };

            if (o.Web)
            {
                Step("Configurando o monitor web", "Regra de firewall: portas 8420/8421 (rede privada)", 84);
                if (IsAdmin)
                {
                    Run("netsh", $"advfirewall firewall delete rule name=\"{FwRule}\"");
                    var ok = Run("netsh", $"advfirewall firewall add rule name=\"{FwRule}\" dir=in action=allow protocol=TCP localport=8420-8421 profile=private program=\"{exe}\"") == 0;
                    man.Firewall = ok;
                    if (ok) undo.Push(() => Run("netsh", $"advfirewall firewall delete rule name=\"{FwRule}\""));
                    Ok(ok ? "Configurando o monitor web" : "Regra de firewall não criada (o Windows perguntará no primeiro uso)", !ok);
                }
                else Ok("Monitor web: o Windows pedirá permissão de rede no primeiro uso", true);
            }

            if (o.Samples)
            {
                Step("Copiando roteiros de exemplo", "", 86);
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Product, "Exemplos");
                Directory.CreateDirectory(dir);
                foreach (var e in zip.Entries.Where(e => N(e).StartsWith("samples/") && e.Length > 0))
                {
                    var dst = System.IO.Path.Combine(dir, e.Name);
                    if (!File.Exists(dst)) e.ExtractToFile(dst);
                }
                man.SamplesDir = dir;
                Ok("Copiando roteiros de exemplo");
            }

            var root = o.Scope == Scope.AllUsers ? Registry.LocalMachine : Registry.CurrentUser;
            if (o.Associate)
            {
                Step("Associando arquivos .tps", @"Software\Classes\.tps → " + ProgId, 89);
                using (var k = root.CreateSubKey(@"Software\Classes\.tps")) k.SetValue("", ProgId);
                using (var k = root.CreateSubKey($@"Software\Classes\{ProgId}")) k.SetValue("", "Roteiro ARCA Teleprompter");
                using (var k = root.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon")) k.SetValue("", $"\"{exe}\",0");
                using (var k = root.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command")) k.SetValue("", $"\"{exe}\" \"%1\"");
                undo.Push(() => { root.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false); root.DeleteSubKeyTree(@"Software\Classes\.tps", false); });
                man.Associated = true;
                Notify();
                Ok("Associando arquivos .tps");
            }

            if (o.StartMenu || o.Desktop)
            {
                Step("Criando atalhos", string.Join(" · ", new[] { o.StartMenu ? "Menu Iniciar" : "", o.Desktop ? "Área de trabalho" : "" }.Where(s => s.Length > 0)), 93);
                var dirs = new List<string>();
                if (o.StartMenu) dirs.Add(Environment.GetFolderPath(o.Scope == Scope.AllUsers ? Environment.SpecialFolder.CommonPrograms : Environment.SpecialFolder.Programs));
                if (o.Desktop) dirs.Add(Environment.GetFolderPath(o.Scope == Scope.AllUsers ? Environment.SpecialFolder.CommonDesktopDirectory : Environment.SpecialFolder.DesktopDirectory));
                foreach (var d in dirs)
                {
                    var lnk = System.IO.Path.Combine(d, Product + ".lnk");
                    try { Shortcut(lnk, exe, target, "Teleprompter para estúdio e jornalismo"); man.Shortcuts.Add(lnk); undo.Push(() => File.Delete(lnk)); }
                    catch (Exception ex) { Ok("Atalho não criado em " + d + ": " + ex.Message, true); }
                }
                Ok("Criando atalhos");
            }

            Step("Registrando o desinstalador", "Configurações › Aplicativos › " + Product, 97);
            using (var k = root.CreateSubKey(RegKey))
            {
                var un = System.IO.Path.Combine(target, Uninstaller);
                k.SetValue("DisplayName", Product); k.SetValue("DisplayVersion", Version); k.SetValue("Publisher", "ARCA");
                k.SetValue("DisplayIcon", $"\"{exe}\",0"); k.SetValue("InstallLocation", target);
                k.SetValue("UninstallString", $"\"{un}\" --uninstall"); k.SetValue("QuietUninstallString", $"\"{un}\" --uninstall");
                k.SetValue("EstimatedSize", (int)(total / 1024), RegistryValueKind.DWord); k.SetValue("NoModify", 1, RegistryValueKind.DWord); k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            }
            undo.Push(() => root.DeleteSubKeyTree(RegKey, false));
            File.WriteAllText(System.IO.Path.Combine(target, "install.json"), JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));
            Ok("Registrando o desinstalador");
            ui.Report(new Progress("Concluído", "", 100));
        }
        catch (Exception)
        {
            // Cancelado ou falhou: desfaz o que foi feito, na ordem inversa.
            while (undo.Count > 0) { try { undo.Pop()(); } catch { } }
            throw;
        }
    }

    // ——— Desinstalação ———
    public sealed record KeepOptions(bool Scripts, bool Prefs, bool Profiles, bool Logs);

    public static async Task Uninstall(Manifest man, KeepOptions keep, IProgress<Progress> ui)
    {
        void Step(string t, string f = "", double? p = null) => ui.Report(new Progress(t, f, p));
        void Ok(string l, bool warn = false) => ui.Report(new Progress(Log: l, Warn: warn));
        Step("Fechando o ARCA Teleprompter", "Encerrando processos", 5);
        await CloseApp();
        Ok("Fechando o ARCA Teleprompter");

        Step("Removendo arquivos do programa", man.Path, 20);
        // O desinstalador roda de dentro da pasta: apaga tudo o que não está em uso agora e o restante
        // (ele mesmo e o runtime carregado) logo depois de fechar — ver ScheduleFolderRemoval.
        var running = System.IO.Path.GetDirectoryName(Environment.ProcessPath!)!;
        var inPlace = string.Equals(System.IO.Path.GetFullPath(running).TrimEnd('\\'), System.IO.Path.GetFullPath(man.Path).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        if (Directory.Exists(man.Path))
        {
            // Arquivos que o próprio desinstalador ainda pode carregar (runtime .NET/WPF) ficam até ele fechar.
            var own = inPlace ? OwnFiles(man.Path) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.EnumerateFiles(man.Path, "*", SearchOption.AllDirectories))
            {
                if (own.Contains(System.IO.Path.GetRelativePath(man.Path, f))) continue;
                try { File.Delete(f); } catch { }
            }
            foreach (var d in Directory.EnumerateDirectories(man.Path, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length)) { try { Directory.Delete(d, false); } catch { } } // só as vazias
            if (!inPlace) try { Directory.Delete(man.Path, true); } catch { }
        }
        PendingRemoval = inPlace ? man.Path : null;
        Ok("Removendo arquivos do programa");

        Step("Removendo atalhos e associação .tps", "Menu Iniciar · Área de trabalho · .tps", 50);
        foreach (var l in man.Shortcuts) try { File.Delete(l); } catch { }
        var root = man.Scope == Scope.AllUsers ? Registry.LocalMachine : Registry.CurrentUser;
        if (man.Associated) { try { root.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false); root.DeleteSubKeyTree(@"Software\Classes\.tps", false); Notify(); } catch { } }
        Ok("Removendo atalhos e associação .tps");

        if (man.Firewall) { Step("Removendo regra de firewall", "Portas 8420/8421", 58); Run("netsh", $"advfirewall firewall delete rule name=\"{FwRule}\""); Ok("Removendo regra de firewall"); }

        var data = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Product);
        var local = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Product);
        void Del(string f) { try { File.Delete(System.IO.Path.Combine(data, f)); } catch { } }
        if (!keep.Scripts) { Step("Apagando roteiros e versões", data, 66); Del("session.json"); Del("versions.json"); Ok("Apagando roteiros e versões"); }
        if (!keep.Prefs) { Step("Apagando preferências", data, 72); Del("settings.json"); Del("https.pfx"); Ok("Apagando preferências"); }
        if (!keep.Profiles) { Step("Apagando perfis de apresentador", data, 78); Del("profiles.json"); Ok("Apagando perfis de apresentador"); }
        if (!keep.Logs) { Step("Apagando registros e cache", local, 84); try { if (Directory.Exists(local)) Directory.Delete(local, true); } catch { } Ok("Apagando registros e cache"); }
        if (!keep.Scripts && !keep.Prefs && !keep.Profiles) try { if (Directory.Exists(data) && !Directory.EnumerateFileSystemEntries(data).Any()) Directory.Delete(data); } catch { }

        Step("Removendo registro do desinstalador", "Aplicativos instalados", 95);
        try { root.DeleteSubKeyTree(RegKey, false); } catch { }
        Ok("Removendo registro do desinstalador");
        ui.Report(new Progress("Concluído", "", 100));
    }

    /// <summary>Arquivos (caminho relativo) usados pelo desinstalador, lidos do seu .deps.json.</summary>
    private static HashSet<string> OwnFiles(string dir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var self = System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath!);
        foreach (var ext in new[] { ".exe", ".dll", ".deps.json", ".runtimeconfig.json" }) set.Add(self + ext);
        foreach (var f in new[] { "hostfxr.dll", "hostpolicy.dll", "coreclr.dll", "clrjit.dll", "clrgc.dll", "System.Private.CoreLib.dll", "install.json" }) set.Add(f);
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(dir, self + ".deps.json")));
            foreach (var target in doc.RootElement.GetProperty("targets").EnumerateObject())
                foreach (var lib in target.Value.EnumerateObject())
                    foreach (var kind in new[] { "runtime", "native", "resources" })
                        if (lib.Value.TryGetProperty(kind, out var files))
                            foreach (var f in files.EnumerateObject()) set.Add(kind == "resources" ? f.Name.Replace('/', System.IO.Path.DirectorySeparatorChar) : System.IO.Path.GetFileName(f.Name));
        }
        catch { /* sem deps.json: mantém só o essencial */ }
        return set;
    }

    /// <summary>Pasta a apagar depois que este processo terminar (desinstalação feita de dentro dela).</summary>
    public static string? PendingRemoval { get; private set; }

    public static void ScheduleFolderRemoval()
    {
        if (PendingRemoval == null) return;
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"{PendingRemoval}\"") { CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = System.IO.Path.GetTempPath() });
        }
        catch { }
    }

    // ——— Auxiliares ———
    public static int Run(string exe, string args)
    {
        try { using var p = Process.Start(new ProcessStartInfo(exe, args) { CreateNoWindow = true, UseShellExecute = false }); p!.WaitForExit(15000); return p.ExitCode; }
        catch { return -1; }
    }

    /// <summary>Atalho .lnk via WScript.Shell (COM do Windows).</summary>
    private static void Shortcut(string lnk, string target, string workDir, string desc)
    {
        var t = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell indisponível");
        dynamic shell = Activator.CreateInstance(t)!;
        dynamic sc = shell.CreateShortcut(lnk);
        sc.TargetPath = target; sc.WorkingDirectory = workDir; sc.Description = desc; sc.IconLocation = target + ",0";
        sc.Save();
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")] private static extern void SHChangeNotify(int e, uint f, IntPtr a, IntPtr b);
    /// <summary>Avisa o Explorer que as associações de arquivo mudaram (ícone do .tps).</summary>
    private static void Notify() => SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);

    /// <summary>Relança este programa como administrador (UAC). false se o usuário recusar.</summary>
    public static bool RelaunchElevated(string args)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, args) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
}
