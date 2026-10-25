using System.Collections.Concurrent;
using System.Diagnostics;
using Whisper.net;

namespace Arca.Audio;

/// <summary>
/// Reconhecimento de fala offline com Whisper (whisper.cpp via Whisper.net, modelo multilíngue quantizado).
/// Bem mais preciso que o Vosk com microfones reais (sala, ruído, voz longe), ao custo de mais CPU.
/// <para>
/// Mesmo contrato do <see cref="SpeechRecognizer"/>: mesma entrada, mesmo tratamento do sinal (<see cref="VoiceFrontEnd"/>) e os
/// mesmos eventos. O Whisper não é de streaming: a thread própria retranscreve uma janela deslizante de 2–4 s a cada ~0,3 s,
/// só quando o VAD (o detector de fala do gate) acusa voz — no silêncio o modelo nem roda, e por isso não "inventa" frases.
/// </para>
/// <para>
/// Para emitir só palavras novas e estáveis usa o acordo local (LocalAgreement-2): uma palavra sai quando duas transcrições
/// seguidas concordam nela. No fim da frase (pausa de 0,5 s) o resto sai de uma vez e a janela recomeça.
/// O vocabulário do roteiro vira o "prompt" do Whisper (contexto), o que puxa a transcrição para as palavras do texto.
/// </para>
/// </summary>
public sealed class WhisperRecognizer : IVoiceRecognizer
{
    public const int Rate = VoiceFrontEnd.Rate;
    private static Shared? _shared;
    private static readonly object FactoryLock = new();

    /// <summary>Maior janela retranscrita (s). Mais longa = mais contexto, porém mais lenta.</summary>
    public double MaxWindow { get; set; } = 4.0;
    /// <summary>Áudio mantido (s) quando a janela passa de <see cref="MaxWindow"/>: o corte é no fim de uma palavra já emitida.</summary>
    public double Keep { get; set; } = 2.5;
    /// <summary>Intervalo mínimo entre transcrições (s).</summary>
    public double Hop { get; set; } = 0.3;
    /// <summary>Palavra que terminou há este tempo (s) já sai sem esperar a confirmação da próxima transcrição. 0 = só acordo.</summary>
    public double StableLag { get; set; } = 0.35;
    /// <summary>Pausa que encerra a frase e libera as últimas palavras (s).</summary>
    public double EndPause { get; set; } = 0.5;
    /// <summary>
    /// Contexto de áudio do codificador (quadros de 20 ms). 0 = 30 s completos (lento). Do tamanho da janela, o Whisper
    /// fica ~6× mais rápido — é o que deixa a latência abaixo de 1 s em CPU.
    /// </summary>
    public int AudioContext { get; set; } = -1;
    /// <summary>Threads de CPU do Whisper.</summary>
    public int Threads { get; set; } = Math.Clamp(Environment.ProcessorCount / 2, 2, 8);
    /// <summary>Usa o roteiro como prompt (contexto) do Whisper.</summary>
    public bool UsePrompt { get; set; } = true;
    /// <summary>Palavras do roteiro no prompt.</summary>
    public int PromptWords { get; set; } = 45;
    /// <summary>Usa a placa de vídeo (Vulkan) quando houver. Desligado: só CPU.</summary>
    public bool Gpu { get; set; } = true;
    /// <summary>Atenção "flash" no whisper.cpp (vale para o próximo modelo carregado).</summary>
    public static bool FlashAttention { get; set; }
    /// <summary>Modelo pronto (o carregamento acontece na thread do reconhecedor, sem travar a interface).</summary>
    public event Action? Loaded;

    private readonly AudioInput _input;
    private readonly string _language;
    private readonly string _modelPath;
    private Shared? _model;
    private readonly ConcurrentQueue<(float[] Pcm, bool Speech)> _queue = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _worker;
    private readonly VoiceFrontEnd _fe = new();
    private volatile bool _running;
    private volatile string? _pendingPrompt;
    private string _prompt = "";
    private List<string>? _vocab0;

    // Áudio da janela atual (16 kHz) e tempos absolutos em amostras desde o início.
    private readonly List<float> _buf = new();
    private long _bufStart, _total, _lastSpeech = -1, _lastRun;
    private int _speechInBuf;
    // Palavras já emitidas (texto normalizado e fim absoluto) e a hipótese anterior ainda não confirmada.
    private readonly List<(string Norm, long End)> _committed = new();
    private List<HypWord> _prev = new();
    private long _committedEnd;

    private readonly record struct HypWord(string Text, string Norm, long Start, long End);

    /// <inheritdoc cref="VoiceFrontEnd.GateDb"/>
    public double GateDb { get => _fe.GateDb; set => _fe.GateDb = value; }
    /// <inheritdoc cref="VoiceFrontEnd.InputGainDb"/>
    public double InputGainDb { get => _fe.InputGainDb; set => _fe.InputGainDb = value; }
    /// <inheritdoc cref="VoiceFrontEnd.AutoGain"/>
    public bool AutoGain { get => _fe.AutoGain; set => _fe.AutoGain = value; }
    /// <inheritdoc cref="VoiceFrontEnd.LevelDb"/>
    public double LevelDb => _fe.LevelDb;

    /// <summary>Nova palavra reconhecida (thread do reconhecedor).</summary>
    public event Action<string>? Word;
    public event Action<double>? Level { add => _fe.Level += value; remove => _fe.Level -= value; }
    public event Action<Exception>? Error;
    /// <summary>Cada transcrição: duração do áudio e tempo de processamento (ms) — diagnóstico e testes.</summary>
    public event Action<double, double>? Pass;
    /// <summary>Hipótese de cada transcrição (diagnóstico).</summary>
    public event Action<string>? Trace;

    /// <param name="language">Idioma do app (pt-BR, es-ES, en-US…).</param>
    /// <param name="vocabulary">Palavras do roteiro em volta da posição (as 40 anteriores e as seguintes), em ordem.</param>
    public WhisperRecognizer(AudioInput input, string modelPath, string language, IEnumerable<string>? vocabulary = null)
    {
        _input = input;
        _language = language.Split('-')[0].ToLowerInvariant();
        _modelPath = modelPath;
        _vocab0 = vocabulary?.ToList();
        _worker = new Thread(Run) { IsBackground = true, Name = "Arca.Voz.Whisper", Priority = ThreadPriority.AboveNormal };
    }

    /// <summary>Modelo carregado (compartilhado entre sessões; liberado só quando ninguém mais o usa).</summary>
    private sealed class Shared
    {
        public required WhisperFactory Factory; public required string Path; public bool Gpu; public int Users; public bool Retired;
    }

    /// <summary>
    /// Carrega o modelo uma vez por processo (centenas de MB). Na troca de modelo, o anterior só é liberado quando a sessão
    /// que ainda o usa termina (ligar/desligar rápido não derruba a transcrição em andamento).
    /// </summary>
    private static Shared Acquire(string path, bool gpu)
    {
        lock (FactoryLock)
        {
            if (_shared == null || _shared.Path != path || _shared.Gpu != gpu)
            {
                if (_shared != null) { _shared.Retired = true; if (_shared.Users == 0) _shared.Factory.Dispose(); }
                // Sem GPU: nem carrega a biblioteca Vulkan (vale até fechar o app, pois a biblioteca nativa carrega uma vez).
                if (!gpu && Whisper.net.LibraryLoader.RuntimeOptions.LoadedLibrary == null)
                    Whisper.net.LibraryLoader.RuntimeOptions.RuntimeLibraryOrder = new() { Whisper.net.LibraryLoader.RuntimeLibrary.Cpu };
                // Tempos por DTW (atenção cruzada): dizem onde cada palavra está na janela, para emitir as já estáveis.
                var name = System.IO.Path.GetFileName(path).ToLowerInvariant();
                var heads = name.Contains("small") ? WhisperAlignmentHeadsPreset.Small : name.Contains("tiny") ? WhisperAlignmentHeadsPreset.Tiny
                    : name.Contains("base") ? WhisperAlignmentHeadsPreset.Base : WhisperAlignmentHeadsPreset.None;
                var f = WhisperFactory.FromPath(path, new WhisperFactoryOptions
                {
                    UseGpu = gpu, UseDtwTimeStamps = heads != WhisperAlignmentHeadsPreset.None, HeadsPreset = heads, UseFlashAttention = FlashAttention
                });
                _shared = new Shared { Factory = f, Path = path, Gpu = gpu };
            }
            _shared.Users++;
            return _shared;
        }
    }

    /// <summary>
    /// Pré-carrega o modelo e faz uma transcrição curta de aquecimento (thread de fundo): na placa de vídeo, a primeira
    /// transcrição prepara os shaders e leva segundos — assim isso não acontece quando o operador liga a voz.
    /// </summary>
    public static void Preload(string path, bool gpu, string language = "pt")
    {
        var s = Acquire(path, gpu);
        try
        {
            using var proc = s.Factory.CreateBuilder().WithLanguage(language.Split('-')[0].ToLowerInvariant()).WithSingleSegment().WithNoContext()
                .WithAudioContextSize(256).WithMaxTokensPerSegment(4).Build();
            var rnd = new Random(1); var audio = new float[Rate];
            for (var i = 0; i < audio.Length; i++) audio[i] = (float)(rnd.NextDouble() - 0.5) * 0.002f;
            foreach (var _ in proc.ProcessAsync(audio).ToBlockingEnumerable()) { }
        }
        finally { Release(s); }
    }

    private static void Release(Shared s)
    {
        lock (FactoryLock) { if (--s.Users == 0 && s.Retired) s.Factory.Dispose(); }
    }

    /// <summary>Biblioteca nativa em uso (Vulkan = placa de vídeo; Cpu) — aparece no registro.</summary>
    public static string Runtime => Whisper.net.LibraryLoader.RuntimeOptions.LoadedLibrary?.ToString() ?? "?";

    /// <summary>
    /// Prompt = trecho do roteiro de 10 palavras antes da posição até <see cref="PromptWords"/> (a janela de vocabulário começa
    /// 40 palavras antes da posição e é trocada a cada 30 lidas). Prompt longo custa tempo em toda transcrição.
    /// </summary>
    private string Prompt(IEnumerable<string>? vocabulary) =>
        vocabulary == null ? "" : string.Join(' ', vocabulary.Skip(30).Take(PromptWords));

    public void SetVocabulary(IEnumerable<string>? vocabulary)
    {
        var p = Prompt(vocabulary);
        if (p != _prompt) _pendingPrompt = p;
    }

    public void Start()
    {
        _input.Samples += OnSamples;
        _running = true;
        _worker.Start();
        _input.Start();
    }

    private void OnSamples(float[] buf, int n)
    {
        var got = _fe.Process(buf, n, _input.SampleRate, out var f, out var speech);
        if (got <= 0) return;
        _queue.Enqueue((f.AsSpan(0, got).ToArray(), speech));
        _signal.Set();
    }

    private WhisperProcessor Build()
    {
        var ctx = AudioContext < 0 ? (int)Math.Ceiling((MaxWindow + 0.6) * 50) : AudioContext;
        var b = _model!.Factory.CreateBuilder()
            .WithLanguage(_language)
            .WithThreads(Threads)
            .WithNoContext()
            .WithSingleSegment()
            .WithTokenTimestamps()
            .WithProbabilities()
            .WithTemperature(0f)
            .WithTemperatureInc(0f)      // sem novas tentativas: latência previsível
            .WithNoSpeechThreshold(0.6f)
            .WithMaxTokensPerSegment((int)(MaxWindow * 10) + 8);   // fala ≈ 5 tokens/s: corta laços de repetição
        if (ctx > 0) b = b.WithAudioContextSize(Math.Min(1500, ctx));
        if (UsePrompt && _prompt.Length > 0) b = b.WithPrompt(_prompt);
        return b.Build();
    }

    private void Run()
    {
        WhisperProcessor? proc = null;
        try
        {
            _model = Acquire(_modelPath, Gpu);
            Loaded?.Invoke();
            _prompt = Prompt(_vocab0);
            proc = Build();
            var hop = (long)(Hop * Rate);
            while (_running)
            {
                _signal.WaitOne(50);
                while (_queue.TryDequeue(out var c))
                {
                    _buf.AddRange(c.Pcm);
                    _total += c.Pcm.Length;
                    if (c.Speech) { _lastSpeech = _total; _speechInBuf += c.Pcm.Length; }
                }
                if (!_running) break;
                if (_pendingPrompt is { } np)
                {
                    _pendingPrompt = null; _prompt = np;
                    proc.Dispose(); proc = Build();
                }

                // Sem fala na janela: o Whisper nem roda (evita alucinação no silêncio). Guarda 0,3 s antes da fala.
                var end = _total - _lastSpeech >= EndPause * Rate;
                // Estalo curto (tosse, batida) seguido de silêncio: não é fala, descarta.
                if (_speechInBuf < 0.25 * Rate && end) _speechInBuf = 0;
                if (_speechInBuf == 0) { Trim(_total - (long)(0.3 * Rate)); continue; }
                if (_total - _lastRun < hop || _speechInBuf < 0.25 * Rate) continue;

                // Janela longa demais: corta no fim de uma palavra já emitida (≈ Keep antes do fim).
                if (_buf.Count > MaxWindow * Rate)
                {
                    var want = _total - (long)(Keep * Rate);
                    var cut = _committed.Select(w => w.End).Where(e => e > _bufStart && e <= want + Rate / 2).DefaultIfEmpty(-1).Max();
                    Trim(cut > 0 && _total - cut <= MaxWindow * Rate ? cut : _total - (long)((Keep + 0.5) * Rate));
                }

                _lastRun = _total;
                var hyp = Transcribe(proc);
                var fresh = NewWords(hyp);
                Trace?.Invoke($"[{_bufStart / (double)Rate:0.00}–{_total / (double)Rate:0.00}{(end ? " FIM" : "")}] " + string.Join(' ', hyp.Select(w => $"{w.Text}@{w.Start / (double)Rate:0.00}-{w.End / (double)Rate:0.00}")) + " ⇒ novas: " + string.Join(' ', fresh.Select(w => w.Text)));
                if (end)
                {
                    // Fim da frase: libera tudo e recomeça a janela (mantém 0,3 s para a próxima palavra).
                    Commit(fresh, fresh.Count);
                    _prev = new();
                    _speechInBuf = 0;
                    Trim(_total - (long)(0.3 * Rate));
                }
                else
                {
                    // Acordo local: emite o prefixo em que esta transcrição e a anterior concordam — ou que já terminou
                    // há StableLag no áudio (o fim da janela é instável; o meio, não). Diminui a latência sem
                    // emitir a palavra ainda sendo falada.
                    var k = 0; var stable = _total - (long)(StableLag * Rate);
                    while (k < fresh.Count && (k < _prev.Count && fresh[k].Norm == _prev[k].Norm || fresh[k].End <= stable)) k++;
                    Commit(fresh, k);
                    _prev = fresh.Skip(k).ToList();
                }
            }
        }
        catch (Exception e) { if (_running) Error?.Invoke(e); }
        finally { proc?.Dispose(); if (_model != null) Release(_model); }
    }

    private void Trim(long from)
    {
        var drop = (int)Math.Clamp(from - _bufStart, 0, _buf.Count);
        if (drop == 0) return;
        _buf.RemoveRange(0, drop);
        _bufStart += drop;
        if (_speechInBuf > _buf.Count) _speechInBuf = _buf.Count;
    }

    private void Commit(List<HypWord> words, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var w = words[i];
            _committed.Add((w.Norm, w.End));
            _committedEnd = Math.Max(_committedEnd, w.End);
            Word?.Invoke(w.Text);
        }
        if (_committed.Count > 64) _committed.RemoveRange(0, _committed.Count - 64);
    }

    /// <summary>Tira da hipótese o que já foi emitido: pelo tempo e pela sobreposição de até 5 palavras.</summary>
    private List<HypWord> NewWords(List<HypWord> hyp)
    {
        var tol = (long)(0.15 * Rate);
        var fresh = hyp.Where(w => w.Start > _committedEnd - tol || w.End > _committedEnd + tol).ToList();
        if (fresh.Count > 0 && _committed.Count > 0 && Math.Abs(fresh[0].Start - _committedEnd) < Rate)
            for (var n = Math.Min(5, Math.Min(_committed.Count, fresh.Count)); n >= 1; n--)
            {
                var ok = true;
                for (var j = 0; j < n && ok; j++) ok = _committed[_committed.Count - n + j].Norm == fresh[j].Norm;
                if (ok) { fresh.RemoveRange(0, n); break; }
            }
        return fresh;
    }

    private List<HypWord> Transcribe(WhisperProcessor proc)
    {
        var audio = _buf.ToArray();
        var sw = Stopwatch.StartNew();
        var segs = new List<SegmentData>();
        // Process síncrono: a thread já é dedicada; os segmentos chegam pelo enumerador.
        foreach (var s in proc.ProcessAsync(audio).ToBlockingEnumerable()) segs.Add(s);
        Pass?.Invoke(audio.Length * 1000.0 / Rate, sw.Elapsed.TotalMilliseconds);

        var words = new List<HypWord>();
        foreach (var s in segs)
        {
            if (Hallucination(s)) continue;
            string? text = null; long ws = 0, we = 0;
            void Flush()
            {
                if (text == null) return;
                var clean = Clean(text);
                if (clean.Length > 0) words.Add(new HypWord(clean, Norm(clean), _bufStart + ws * Rate / 100, _bufStart + we * Rate / 100));
                text = null;
            }
            foreach (var t in s.Tokens ?? Array.Empty<WhisperToken>())
            {
                var tt = t.Text ?? "";
                if (tt.Length == 0 || tt.StartsWith("[_") || tt.StartsWith("<|")) continue;
                var t0 = t.DtwTimestamp > 0 ? t.DtwTimestamp : t.Start;
                if (tt.StartsWith(' ') || text == null) { if (text != null) we = Math.Max(we, t0); Flush(); text = tt.TrimStart(); ws = t0; we = t0 + 10; }
                else { text += tt; we = Math.Max(we, t0 + 10); }
            }
            Flush();
        }
        // Laço de repetição ("nesta edição nesta edição nesta edição…"): fica com a primeira vez e corta o resto.
        for (var i = 0; i < words.Count; i++)
            for (var n = 1; n <= 8 && i + 3 * n <= words.Count; n++)
            {
                var loop = true;
                for (var j = 0; j < 2 * n && loop; j++) loop = words[i + j].Norm == words[i + n + j].Norm;
                if (loop) { words.RemoveRange(i + n, words.Count - i - n); return words; }
            }
        return words;
    }

    // Frases que o Whisper costuma inventar em ruído/silêncio (créditos de legenda de vídeos do treino).
    private static readonly string[] Ghosts =
    {
        "obrigado", "obrigada", "obrigado por assistir", "tchau", "valeu", "inscrevase", "inscrevase no canal",
        "gracias", "gracias por ver", "thank you", "thanks for watching", "bye", "you"
    };

    private static bool Hallucination(SegmentData s)
    {
        var t = (s.Text ?? "").Trim();
        if (t.Length == 0) return true;
        if (s.NoSpeechProbability > 0.6f && s.Probability < 0.5f) return true;
        if (t.Contains('♪') || t[0] is '[' or '(' && t[^1] is ']' or ')') return true;
        var n = Norm(t.Replace(" ", "")); var spaced = string.Join(' ', t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Norm).Where(x => x.Length > 0));
        if (n.Contains("amaraorg") || n.Contains("legendas") || n.Contains("legendado") || n.Contains("subtitulos") || n.Contains("subtitles")) return true;
        return Ghosts.Contains(spaced);
    }

    /// <summary>Palavra para o alinhador: minúscula, sem pontuação nas pontas (acentos ficam).</summary>
    private static string Clean(string w) => w.Trim().Trim('.', ',', '!', '?', ';', ':', '"', '\'', '“', '”', '…', '-', '–', '—', '(', ')', '[', ']', '«', '»', '¿', '¡').ToLowerInvariant();

    private static string Norm(string w) => Arca.Core.Model.TextUtil.Normalize(w);

    public void Dispose()
    {
        _running = false;
        _input.Samples -= OnSamples;
        _input.Dispose();
        _signal.Set();
        if (_worker.IsAlive) _worker.Join(3000);
    }

    /// <summary>Procura um modelo Whisper (ggml-*.bin) nas pastas; <paramref name="prefer"/> = "base" ou "small".</summary>
    public static string? FindModel(string prefer, IEnumerable<string> roots)
    {
        foreach (var r in roots)
        {
            if (!Directory.Exists(r)) continue;
            var files = Directory.EnumerateFiles(r, "ggml-*.bin").ToList();
            var hit = files.FirstOrDefault(f => Path.GetFileName(f).StartsWith("ggml-" + prefer, StringComparison.OrdinalIgnoreCase)
                                                && !Path.GetFileName(f).Contains(".en", StringComparison.OrdinalIgnoreCase));
            if (hit != null) return hit;
        }
        return null;
    }
}
