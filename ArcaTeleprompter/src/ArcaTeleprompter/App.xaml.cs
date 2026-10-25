using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Arca.App.Services;
using Arca.Core.Storage;

namespace Arca.App;

/// <summary>
/// Entrada do aplicativo: instância única (abrir um .tps com o app já aberto manda o arquivo para a janela
/// existente por um named pipe), carregamento das preferências e tratamento global de erros.
/// </summary>
public partial class App : Application
{
    // Uma instância por instalação (pasta do programa): versões em pastas diferentes não se misturam.
    private static readonly string InstanceKey = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory.ToLowerInvariant())))[..10];
    private static readonly string MutexName = "ARCA.Teleprompter.Instance." + InstanceKey;
    private static readonly string PipeName = "ARCA.Teleprompter.Open." + InstanceKey;
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Arquivos a abrir + "--snap=arquivo.png" (captura da janela, usada no suporte e nos testes).
        var files = e.Args.Where(a => (!a.StartsWith('-') && File.Exists(a)) || a.StartsWith("--snap=") || a.StartsWith("--cmd=") || a.StartsWith("--autoteste=") || a.StartsWith("--ativar=") || a == "--talent").ToArray();
        _mutex = new Mutex(true, MutexName, out var first);
        if (!first)
        {
            // Já existe uma janela aberta: entrega os arquivos a ela e sai.
            try
            {
                using var c = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                c.Connect(1500);
                var data = Encoding.UTF8.GetBytes(string.Join("\n", files.Length > 0 ? files : new[] { "--focus" }));
                c.Write(data);
            }
            catch { /* janela travada ou fechando */ }
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUiError;
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Log.Error("Erro fatal", a.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, a) => { Log.Error("Tarefa", a.Exception); a.SetObserved(); };

        base.OnStartup(e);
        var start = FirstRun.Check();
        var settings = Stores.LoadSettings();
        var w = new MainWindow(settings);
        MainWindow = w;
        w.Show();
        start.Finish(w, settings);
        // Procura versão nova (não nas execuções de autoteste).
        var automation = e.Args.Any(a => a.StartsWith("--autoteste=") || a.StartsWith("--ativar="));
        if (!automation) w.Updates.CheckOnStartup();
        // Licença: sem ela, oferece ativar depois das boas-vindas; com ela, confere online em segundo plano.
        _ = w.LicenseOnStartup(automation);
        foreach (var f in files) w.Dispatcher.BeginInvoke(() => Handle(w, f), DispatcherPriority.ApplicationIdle);
        _ = ListenPipe(w);
    }

    private static void Handle(MainWindow w, string arg)
    {
        if (arg.StartsWith("--snap=")) w.SaveSnapshot(arg[7..]);
        // --cmd=nome[:valor] — mesmos comandos do controle remoto (play, faster, nextBm, gotoBm:2…), para automação.
        else if (arg.StartsWith("--cmd=")) { var p = arg[6..].Split(':', 2); w.RemoteCommand(p[0], p.Length > 1 && double.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null); }
        else if (arg == "--talent") w.ToggleTalent();
        // --autoteste=relatorio.txt — colar, apresentador, desfazer/refazer, velocidade, pausa e linha em branco.
        else if (arg.StartsWith("--autoteste=")) _ = w.SelfTest(arg[12..]);
        // --ativar=ARCA-XXXX-XXXX-XXXX — ativa a licença sem interface (instalação pela TI, testes); o resultado vai para o registro.
        else if (arg.StartsWith("--ativar=")) _ = w.ActivateFromCommandLine(arg[9..]);
        else if (arg != "--focus" && File.Exists(arg)) w.OpenPath(arg);
    }

    private static async Task ListenPipe(MainWindow w)
    {
        while (true)
        {
            try
            {
                await using var s = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await s.WaitForConnectionAsync();
                using var r = new StreamReader(s, Encoding.UTF8);
                var text = await r.ReadToEndAsync();
                await w.Dispatcher.InvokeAsync(() =>
                {
                    if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
                    w.Activate();
                    foreach (var f in text.Split('\n', StringSplitOptions.RemoveEmptyEntries)) Handle(w, f);
                });
            }
            catch (Exception e) { Log.Error("Pipe", e); await Task.Delay(1000); }
        }
    }

    /// <summary>Erro na interface: registra, avisa e continua (o roteiro aberto não pode ser perdido no ar).</summary>
    private void OnUiError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("Erro na interface", e.Exception);
        e.Handled = true;
        try
        {
            Arca.App.Controls.ArcaDialog.Info(MainWindow, "Erro", "Ocorreu um erro inesperado, mas o ARCA Teleprompter continua funcionando.", $"{e.Exception.Message}\n\nDetalhes em %LOCALAPPDATA%\\ARCA Teleprompter\\logs.", danger: true);
        }
        catch { /* */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mutex?.ReleaseMutex(); } catch { /* */ }
        base.OnExit(e);
    }
}
