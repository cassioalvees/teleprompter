using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Arca.Setup;
using Microsoft.Win32;

// Uso: SetupTest  → instala numa pasta temporária por cima de uma "instalação anterior" e confere:
//   1) arquivo preso o tempo todo por outro processo: falha com o nome do programa e devolve a pasta como estava;
//   2) arquivo preso só por 2 s: o instalador espera e conclui.
var dir = Path.Combine(Path.GetTempPath(), "arca-setuptest-" + Guid.NewGuid().ToString("N")[..6], "ARCA Teleprompter");
var hadKey = Registry.CurrentUser.OpenSubKey(Installer.RegKey) != null;
var fails = 0;
void Check(bool ok, string what) { Console.WriteLine((ok ? "OK    " : "FALHA ") + what); if (!ok) fails++; }

// Pastas que o instalador aceita (também na continuação elevada).
foreach (var (p, want) in new[] {
    (@"C:\Program Files\ARCA Teleprompter", true), (@"D:\Apps\ARCA Teleprompter", true), (@"C:\Users\Cassio Santos\AppData\Local\Programs\ARCA Teleprompter", true),
    (@"C:\", false), (@"C:\Windows", false), (@"C:\Windows\System32", false), (@"C:\Program Files", false), (@"C:\ProgramData", false),
    (@"C:\Users", false), (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), false), (@"relativo\pasta", false), (@"C:\pasta<inválida>", false) })
    Check(Installer.SafeTarget(p) == want, $"pasta {(want ? "aceita" : "recusada")}: {p}");

// "Instalação anterior": o app do pacote com um byte diferente em cada arquivo.
using (var zip = ZipFile.OpenRead(Path.Combine(AppContext.BaseDirectory, "payload.zip")))
    foreach (var e in zip.Entries.Where(e => e.Length > 0 && e.FullName.Replace('\\', '/').StartsWith("app/")))
    {
        var dst = Path.Combine(dir, e.FullName.Replace('\\', '/')[4..].Replace('/', '\\'));
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        e.ExtractToFile(dst);
        File.AppendAllText(dst, "anterior");
    }
Dictionary<string, string> Snapshot() => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
    .ToDictionary(f => f[dir.Length..], f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));
var before = Snapshot();
var locked = Path.Combine(dir, "WinRT.Runtime.dll");
Console.WriteLine($"Pasta: {dir} ({before.Count} arquivos)");

Process Hold(int seconds) => Process.Start(new ProcessStartInfo("powershell.exe",
    $"-NoProfile -Command \"$f=[IO.File]::Open('{locked}','Open','Read','Read'); Start-Sleep {seconds}; $f.Close()\"") { CreateNoWindow = true, UseShellExecute = false })!;
var opt = new InstallOptions { Path = dir, Scope = Scope.CurrentUser, Voice = false, Web = false, Samples = false, StartMenu = false, Desktop = false, Associate = false };
var log = new Progress<Arca.Setup.Progress>(_ => { });

// 1) Preso o tempo todo
var h = Hold(60); Thread.Sleep(1500);
var sw = Stopwatch.StartNew();
string? err = null;
try { await Installer.Install(opt, log, CancellationToken.None); } catch (Exception ex) { err = ex.Message; }
sw.Stop(); try { h.Kill(true); h.WaitForExit(); } catch { }
Console.WriteLine($"   mensagem: {err}");
Check(err != null && err.Contains("WinRT.Runtime.dll") && err.Contains("powershell", StringComparison.OrdinalIgnoreCase), "arquivo preso: falha dizendo o arquivo e o programa");
Check(sw.Elapsed.TotalSeconds is > 5 and < 30, $"esperou antes de desistir ({sw.Elapsed.TotalSeconds:0.0} s)");
var after = Snapshot();
Check(after.Count == before.Count && after.All(kv => before.TryGetValue(kv.Key, out var v) && v == kv.Value), "pasta devolvida exatamente como estava (sem sobras .arca-old)");

// 2) Preso só por 2 s
h = Hold(2); Thread.Sleep(1200);
err = null;
try { await Installer.Install(opt, log, CancellationToken.None); } catch (Exception ex) { err = ex.Message; }
h.WaitForExit();
Check(err == null, "arquivo preso por 2 s: instalou depois de esperar" + (err != null ? " — " + err : ""));
using (var zip = ZipFile.OpenRead(Path.Combine(AppContext.BaseDirectory, "payload.zip")))
{
    var e = zip.Entries.First(x => x.FullName.Replace('\\', '/') == "app/WinRT.Runtime.dll");
    Check(new FileInfo(locked).Length == e.Length, "WinRT.Runtime.dll é a versão nova");
}
Check(!Directory.EnumerateFiles(dir, "*.arca-old*", SearchOption.AllDirectories).Any(), "sem sobras da versão anterior");

if (!hadKey) Registry.CurrentUser.DeleteSubKeyTree(Installer.RegKey, false);
try { Directory.Delete(Path.GetDirectoryName(dir)!, true); } catch { }
Console.WriteLine(fails == 0 ? "TUDO OK" : $"{fails} FALHA(S)");
return fails;
