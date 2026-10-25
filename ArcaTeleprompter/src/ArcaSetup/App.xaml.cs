using System.IO;
using System.Text.Json;
using System.Windows;

namespace Arca.Setup;

/// <summary>
/// Instalador e desinstalador do ARCA Teleprompter.
///   ArcaSetup.exe                 → instalação (o pacote vem embutido)
///   ArcaSetup.exe --resume x.json → continuação elevada (UAC) com as opções escolhidas
///   ArcaSetup.exe --update        → atualização pedida pelo app: usa a instalação existente, sem perguntas, e reabre o app
///   ArcaUninstall.exe --uninstall → desinstalação (registrada em Configurações › Aplicativos)
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args;
        var uninstall = args.Contains("--uninstall") || Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "").Equals("ArcaUninstall", StringComparison.OrdinalIgnoreCase);
        InstallOptions? resume = null;
        var ri = Array.IndexOf(args, "--resume");
        if (ri >= 0 && ri + 1 < args.Length && File.Exists(args[ri + 1]))
        {
            try { resume = JsonSerializer.Deserialize<InstallOptions>(File.ReadAllText(args[ri + 1])); File.Delete(args[ri + 1]); } catch { }
        }
        // Instalação para todos os usuários: a remoção mexe em Arquivos de Programas e no HKLM.
        if (uninstall && !Installer.IsAdmin && Installer.Installed() is { Scope: Scope.AllUsers } && !args.Contains("--no-elevate"))
        {
            if (Installer.RelaunchElevated("--uninstall --no-elevate")) { Shutdown(); return; }
        }
        DispatcherUnhandledException += (_, x) =>
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "arca-setup.log"), $"{DateTime.Now:s} {x.Exception}\r\n"); } catch { }
            MessageBox.Show("Erro inesperado: " + (string.IsNullOrWhiteSpace(x.Exception.Message) ? x.Exception.GetType().Name : x.Exception.Message), "ARCA Teleprompter", MessageBoxButton.OK, MessageBoxImage.Error);
            x.Handled = true;
        };
        var w = new SetupWindow(!uninstall, resume, args.Contains("--update"));
        MainWindow = w;
        w.Closed += (_, _) => { Installer.ScheduleFolderRemoval(); Shutdown(); };
        w.Show();
    }
}
