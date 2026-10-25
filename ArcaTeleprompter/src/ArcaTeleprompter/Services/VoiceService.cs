using System.Windows.Threading;
using Arca.App.ViewModels;
using Arca.Audio;
using Arca.Core.Engine;
using Arca.Core.Storage;

namespace Arca.App.Services;

/// <summary>
/// Acompanhamento por voz: captura (Arca.Audio: WASAPI/ASIO/arquivo) → reconhecimento offline (Vosk ou Whisper) →
/// VoiceAligner (Arca.Core) → alvo de rolagem. A rolagem para no IMPROVISO e retoma ao reencontrar o texto.
/// </summary>
public sealed class VoiceService
{
    private readonly MainWindow _main;
    private readonly VoiceAligner _al = new();
    private IVoiceRecognizer? _sr;
    private AudioInput? _input;
    private DispatcherTimer? _sim;
    private DateTime _lastLevel;
    private string _mode = "off";
    private string? _filePath;
    private MainViewModel Vm => _main.Vm;

    public VoiceService(MainWindow main) { _main = main; }

    public bool Active => _mode != "off";
    public VoiceState State => _al.State;

    // ——— Modelo de voz ———

    /// <summary>Whisper (preciso, mais CPU/GPU) em vez do Vosk (rápido, leve).</summary>
    public bool UsesWhisper => Vm.App.VoiceEngine == "whisper";

    public string? ModelPath() => UsesWhisper
        ? WhisperRecognizer.FindModel(VoiceModels.Whisper(Vm.App.WhisperModel).Language, AppPaths.VoiceModelDirs())
        : SpeechRecognizer.FindModel(Vm.App.VoiceLanguage, AppPaths.VoiceModelDirs());

    /// <summary>Modelo que o botão "Baixar modelo" baixa para o motor e idioma escolhidos.</summary>
    private VoiceModel? Downloadable() => UsesWhisper ? VoiceModels.Whisper(Vm.App.WhisperModel) : VoiceModels.For(Vm.App.VoiceLanguage);

    private string? _warm;
    /// <summary>Prepara o motor de voz em segundo plano (modelo + placa de vídeo), para a voz começar a ouvir na hora.</summary>
    public void Warmup()
    {
        var p = ModelPath(); if (p == null) return;
        var key = (UsesWhisper ? "w:" + Vm.App.WhisperGpu + ":" : "v:") + p;
        if (_warm == key) return; _warm = key;
        var whisper = UsesWhisper; var gpu = Vm.App.WhisperGpu; var lang = Vm.App.VoiceLanguage;
        new Thread(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (whisper) WhisperRecognizer.Preload(p, gpu, lang); else SpeechRecognizer.Preload(p);
                Log.Info($"Voz preparada em {sw.ElapsedMilliseconds} ms: {System.IO.Path.GetFileName(p)}");
            }
            catch (Exception e) { _warm = null; Log.Error("Preparar voz", e); }
        }) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Arca.Voz.Preparar" }.Start();
    }

    public void CheckModel()
    {
        var p = ModelPath();
        if (p != null) Warmup();
        var m = Downloadable();
        Vm.ModelMissing = p == null;
        Vm.ModelStatus = p != null ? "Modelo offline: " + System.IO.Path.GetFileName(p)
            : m == null ? "Sem modelo para este idioma."
            : UsesWhisper ? $"Modelo {m.Label} não instalado ({m.SizeMb} MB, multilíngue)." : $"Modelo de voz não instalado ({m.SizeMb} MB).";
    }

    public async Task DownloadModel()
    {
        var m = Downloadable();
        if (m == null) return;
        Vm.ModelStatus = $"Baixando {m.Label}…";
        var prog = new Progress<double>(v => { Vm.ModelProgress = v; Vm.ModelStatus = $"Baixando {m.Label}… {v:P0}"; });
        try { await VoiceModels.DownloadAsync(m, System.IO.Path.Combine(AppPaths.Local, "models"), prog, CancellationToken.None); }
        catch (Exception e) { Vm.ModelStatus = "Falha no download: " + e.Message; Log.Error("Modelo de voz", e); return; }
        CheckModel();
    }

    // ——— Início / parada ———

    public void ToggleMic() { if (Active) Stop(); else StartAudio(Vm.App.AudioDriver == "asio" ? AudioDriver.Asio : AudioDriver.Wasapi, Vm.App.AudioDevice, Vm.App.AsioChannel, "mic"); }
    public void StartFile(string path) { Stop(); _filePath = path; StartAudio(AudioDriver.File, path, 0, "file"); }
    public void RestartIfRunning() { if (_mode == "mic") { Stop(); ToggleMic(); } }
    public void SetGate(double db) { if (_sr != null) _sr.GateDb = db; }
    public void ApplySensitivity()
    {
        _al.Window = Vm.App.VoiceWindow; _al.Tolerance = Vm.App.VoiceTolerance;
        _al.Threshold = Math.Clamp(Vm.App.VoiceStrict, 0.5, 0.95);
        _main.Engine.VoiceSmooth = Math.Clamp(Vm.App.VoiceSmooth, 0.1, 1.5);
    }

    private void Prepare()
    {
        _al.Load(_main.Doc);
        ApplySensitivity();
        _al.Start(_al.WordAtOrAfterBlock(_main.CurrentBlock()));
        Vm.Heard.Clear(); Vm.VoiceError = "";
        _main.Engine.Pause();
        _main.Engine.VoiceMode = true; _main.Engine.VoiceTarget = null; _lastWord = -1;
        SetState();
    }

    private void StartAudio(AudioDriver driver, string device, int channel, string mode)
    {
        if (!_main.Allow("voz")) return;
        var model = ModelPath();
        if (model == null) { CheckModel(); Vm.VoiceError = "Baixe o modelo de voz offline (abaixo) ou use “Simular leitura”."; return; }
        Prepare();
        try
        {
            var input = new AudioInput(driver, device, channel); _input = input;
            _vocabAt = _al.Position;
            var vocab = Vm.UseScriptVocabulary ? VocabWindow(_al.Position) : null;
            if (UsesWhisper)
            {
                var w = new WhisperRecognizer(input, model, Vm.App.VoiceLanguage, vocab) { Gpu = Vm.App.WhisperGpu };
                // O modelo carrega na thread do reconhecedor (centenas de MB): a interface não trava.
                w.Loaded += () =>
                {
                    var rt = WhisperRecognizer.Runtime;
                    Log.Info($"Whisper pronto: {System.IO.Path.GetFileName(model)} · {rt}");
                    var gpu = rt.Contains("Vulkan", StringComparison.OrdinalIgnoreCase) && Vm.App.WhisperGpu;
                    _main.Dispatcher.BeginInvoke(() => Vm.ModelStatus = "Modelo offline: " + System.IO.Path.GetFileName(model) + (gpu ? " · placa de vídeo" : " · processador"));
                };
                // Transcrição lenta (> 0,8 s) atrasa a rolagem: avisa uma vez, nas primeiras 15 transcrições.
                var passes = 0; double slow = 0;
                w.Pass += (_, ms) =>
                {
                    if (++passes > 15) return;
                    slow += ms;
                    if (passes == 15 && slow / passes > 800)
                    {
                        Log.Info($"Whisper lento: {slow / passes:0} ms por transcrição");
                        _main.Dispatcher.BeginInvoke(() => Vm.ModelStatus = $"Whisper lento neste computador ({slow / passes / 1000:0.0} s por trecho)"
                            + (Vm.App.WhisperModel == "small" ? ": escolha o modelo Base." : ": prefira o Vosk."));
                    }
                };
                _sr = w;
            }
            else _sr = new SpeechRecognizer(input, model, vocab);
            _sr.GateDb = Vm.App.NoiseGateDb; _sr.InputGainDb = Vm.App.VoiceGainDb; _sr.AutoGain = Vm.App.VoiceAgc;
            _sr.Word += w => _main.Dispatcher.BeginInvoke(() => Hear(w));
            _sr.Level += db =>
            {
                var now = DateTime.UtcNow; if ((now - _lastLevel).TotalMilliseconds < 80) return; _lastLevel = now;
                _main.Dispatcher.BeginInvoke(() => Vm.LevelDb = db);
            };
            _sr.Error += e => _main.Dispatcher.BeginInvoke(() =>
            {
                Log.Error("Reconhecimento", e);
                Vm.VoiceError = "Reconhecimento: " + e.Message + (UsesWhisper && Vm.App.WhisperGpu ? " — tente desligar “Usar a placa de vídeo”." : "");
                Stop();
            });
            input.Stopped += e => _main.Dispatcher.BeginInvoke(() =>
            {
                if (e != null) { Vm.VoiceError = "Entrada de áudio parou: " + e.Message; Stop(); }
                else if (_mode == "file") { Vm.VoiceTitle = "Fim do arquivo de áudio"; Stop(); }
            });
            _sr.Start();
            _mode = mode; Vm.VoiceMode = mode;
            _main.SetLocked(true);
            if (input.FormatInfo.Length > 0) Log.Info("Microfone: " + input.FormatInfo);
            Log.Info($"Voz iniciada: {driver} {(driver == AudioDriver.Asio ? device + " canal " + (channel + 1) : device)} · {_sr.GetType().Name} · modelo {System.IO.Path.GetFileName(model)}");
        }
        catch (Exception e)
        {
            Log.Error("Voz", e);
            Vm.VoiceError = driver == AudioDriver.Asio ? "Não foi possível abrir o driver ASIO (interface conectada? em uso por outro programa?): " + e.Message : "Não foi possível abrir o microfone: " + e.Message;
            Stop();
        }
        SetState();
    }

    /// <summary>
    /// Simulação (sem microfone): "lê" o roteiro palavra por palavra e, a cada 22 palavras, improvisa uma frase
    /// fora do texto — mostra os estados SEGUINDO, IMPROVISO e RESSINC.
    /// </summary>
    public void ToggleSim()
    {
        if (_mode == "sim") { Stop(); return; }
        if (!_main.Allow("voz")) return;
        Stop(); Prepare();
        var improv = new[] { "deixa", "eu", "só", "fazer", "um", "comentário", "rápido", "aqui", "antes" };
        int p = _al.Position, said = 0, imp = -1;
        _sim = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(340) };
        _sim.Tick += (_, _) =>
        {
            if (imp >= 0) { Hear(improv[imp]); Vm.LevelDb = -22 - Random.Shared.NextDouble() * 6; if (++imp >= improv.Length) { imp = -1; p = _al.Position; } return; }
            if (p >= _al.Words.Count) { Stop(); return; }
            Hear(_al.Words[p].Text); p++; said++; Vm.LevelDb = -18 - Random.Shared.NextDouble() * 8;
            if (said % 22 == 0) imp = 0;
        };
        _sim.Start();
        _mode = "sim"; Vm.VoiceMode = "sim";
        _main.SetLocked(true);
        SetState();
    }

    /// <summary>Grava 15 s do microfone exatamente como chega (todos os canais) em logs\voz-diagnostico-*.wav.</summary>
    public void RecordDiagnostic()
    {
        if (_mode != "mic") ToggleMic();
        if (_mode != "mic" || _input == null) { _main.Flash("Não foi possível abrir o microfone"); return; }
        System.IO.Directory.CreateDirectory(AppPaths.Logs);
        var path = System.IO.Path.Combine(AppPaths.Logs, $"voz-diagnostico-{DateTime.Now:yyyyMMdd-HHmmss}.wav");
        _input.RecordDiagnostic(path, 15);
        Log.Info("Diagnóstico de voz: " + path + " · " + _input.FormatInfo);
        _main.Flash("Gravando 15 s — leia o roteiro em voz alta");
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15.5) };
        t.Tick += (_, _) => { t.Stop(); _main.Flash("Diagnóstico salvo nos registros"); };
        t.Start();
    }

    /// <summary>
    /// O operador levou o texto para outro ponto (rolagem, marcador, parágrafo, início) com a voz ligada: o
    /// acompanhamento recomeça dali — dá para voltar a um trecho anterior.
    /// </summary>
    public void JumpTo(double y)
    {
        if (!Active) return;
        var (block, _) = _main.AnchorAt(y);
        var w = _al.WordAtOrAfterBlock(block);
        _al.Start(w);
        _lastWord = w - 1;
        _main.Engine.VoiceTarget = y;
        if (_sr != null && Vm.UseScriptVocabulary) { _vocabAt = _al.Position; _sr.SetVocabulary(VocabWindow(_vocabAt)); }
        SetState();
    }

    public void Stop()
    {
        _sim?.Stop(); _sim = null;
        var sr = _sr; _sr = null;
        if (sr != null) Task.Run(sr.Dispose);
        _al.Stop();
        var was = _mode != "off";
        _mode = "off"; Vm.VoiceMode = "off"; Vm.LevelDb = -70;
        _main.Engine.VoiceMode = false; _main.Engine.VoiceTarget = null;
        if (was) _main.SetLocked(false);
        SetState();
    }

    /// <summary>Palavras mudaram (edição): recarrega mantendo a posição.</summary>
    public void Reload() { if (Active) { var pos = _al.Position; _al.Load(_main.Doc); } }

    /// <summary>Palavras do roteiro em volta da posição (40 para trás, 220 para frente) — gramática do reconhecedor.</summary>
    private IEnumerable<string> VocabWindow(int pos) => _al.Words.Skip(Math.Max(0, pos - 40)).Take(260).Select(w => w.Text.ToLowerInvariant()).Concat(CommandWords);

    // ═══ Comandos por voz: "comando voltar", "comando início", "comando parar", "comando próximo" ═══
    private static readonly string[] CommandWords = { "comando", "voltar", "volta", "início", "inicio", "começo", "parar", "pare", "próximo", "proximo", "avançar" };
    private bool _cmdArmed; private DateTime _cmdAt;

    /// <summary>Trata a palavra como parte de um comando. Devolve true se ela foi consumida (não vai para o alinhador).</summary>
    private bool Command(string w)
    {
        var n = Arca.Core.Model.TextUtil.Normalize(w);
        if (n == "comando") { _cmdArmed = true; _cmdAt = DateTime.UtcNow; return true; }
        if (!_cmdArmed || (DateTime.UtcNow - _cmdAt).TotalSeconds > 3) { _cmdArmed = false; return false; }
        _cmdArmed = false;
        switch (n)
        {
            case "voltar": case "volta": _main.JumpParagraph(-1); _main.Flash("Voz: parágrafo anterior"); return true;
            case "inicio": case "comeco": _main.GoTo(0, textLine: true); _main.Flash("Voz: início"); return true;
            case "proximo": case "avancar": _main.JumpParagraph(1); _main.Flash("Voz: próximo parágrafo"); return true;
            case "parar": case "pare": _main.Dispatcher.BeginInvoke(() => { Stop(); _main.Flash("Voz desligada (comando)"); }); return true;
        }
        return false;
    }

    public void SetInputGain(double db) { if (_sr != null) _sr.InputGainDb = db; }
    public void SetAutoGain(bool on) { if (_sr != null) _sr.AutoGain = on; }
    private int _vocabAt;
    private int _lastWord = -1;

    private void Hear(string w)
    {
        if (!Active) return;
        if (Command(w)) { Vm.Heard.Add(new HeardVM(w, 1)); while (Vm.Heard.Count > 16) Vm.Heard.RemoveAt(0); return; }
        var before = _al.State;
        var h = _al.Hear(w);
        if (_sr != null && Vm.UseScriptVocabulary && Math.Abs(_al.Position - _vocabAt) >= 30) { _vocabAt = _al.Position; _sr.SetVocabulary(VocabWindow(_vocabAt)); }
        Vm.Heard.Add(new HeardVM(w, h.Match));
        while (Vm.Heard.Count > 16) Vm.Heard.RemoveAt(0);
        if (h.Match == 2 && _al.LastMatch is int i)
        {
            var word = _al.Words[i];
            // Rolagem contínua: avança pela fração da linha já lida (+ antecipação), sem saltos entre linhas
            // nem entre parágrafos.
            var y = _main.ReadingY(word.Block, word.CharIndex, word.Length, Vm.App.VoiceLead);
            // O texto só anda para a frente. Voltar exige ressincronia confirmada (duas palavras seguidas) de pelo
            // menos 4 palavras — palavras repetidas no roteiro (de, que…) não fazem mais o texto subir e descer.
            var back = i < _lastWord;
            var confirmedBack = back && before == VoiceState.Resyncing && _lastWord - i >= 4;
            if (!double.IsNaN(y) && (!back || confirmedBack))
            {
                var cur = _main.Engine.VoiceTarget;
                // Também nunca atrás de onde o texto já está (ex.: a primeira palavra ouvida ao ligar a voz).
                var floor = Math.Max(cur ?? double.MinValue, _main.Engine.Y) - 0.5;
                if (y >= floor || confirmedBack) _main.Engine.VoiceTarget = y;
                _lastWord = i;
            }
        }
        SetState();
    }

    private void SetState()
    {
        var (code, title, shortName) = (_mode == "off" ? VoiceState.Off : _al.State) switch
        {
            VoiceState.Listening => ("OUVINDO", "Aguardando a primeira palavra do roteiro", "ouvindo"),
            VoiceState.Following => ("SEGUINDO", "Rolagem acompanha a voz", "seguindo"),
            VoiceState.Improvising => ("IMPROVISO", "Fora do roteiro — rolagem pausada", "improviso"),
            VoiceState.Resyncing => ("RESSINC.", "Texto encontrado, confirmando posição", "ressincronizando"),
            _ => ("DESLIGADO", "Acompanhamento desligado", "desligada")
        };
        Vm.VoiceCode = code; Vm.VoiceTitle = title; Vm.VoiceShort = shortName;
        _main.UpdateStatus();
    }
}
