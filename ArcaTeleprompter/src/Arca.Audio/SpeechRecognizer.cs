using System.Collections.Concurrent;
using System.Text.Json;
using Vosk;

namespace Arca.Audio;

/// <summary>
/// Reconhecimento de fala offline (Vosk / Kaldi) em streaming.
/// Fluxo: AudioInput (taxa nativa, mono float) → passa-altas 80 Hz → controle automático de ganho
/// (AGC) + gate suave → reamostragem para 16 kHz → PCM 16 bits → fila → thread própria com o Vosk. Nunca roda na thread da interface,
/// para a rolagem não engasgar. Emite cada palavra nova assim que aparece no resultado parcial.
/// </summary>
public sealed class SpeechRecognizer : IVoiceRecognizer
{
    public const int Rate = VoiceFrontEnd.Rate;
    private static Model? _model;
    private static string? _modelPath;

    private readonly AudioInput _input;
    // ~10 s de áudio: cobre o carregamento do modelo na primeira vez sem perder o começo da fala.
    private readonly BlockingCollection<short[]> _queue = new(new ConcurrentQueue<short[]>(), 1000);
    private readonly string _path;
    private static readonly object ModelLock = new();
    private readonly Thread _worker;
    private VoskRecognizer? _rec;
    private volatile bool _running;
    private int _emitted;
    private string? _grammar;
    private volatile string? _pendingGrammar;
    private volatile bool _hasPending;

    // Processamento de sinal (thread de captura): passa-altas, AGC, gate e 16 kHz — comum aos motores.
    private readonly VoiceFrontEnd _fe = new();

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
    /// <summary>Reconhecedor pronto para ouvir (o áudio de antes fica na fila e é reconhecido em seguida).</summary>
    public event Action? Ready;

    /// <param name="vocabulary">
    /// Palavras do roteiro. Quando informado, o Vosk reconhece só essas palavras e "[unk]" para o resto —
    /// muito mais preciso para seguir o texto, e o improviso aparece como palavras desconhecidas.
    /// </param>
    public SpeechRecognizer(AudioInput input, string modelPath, IEnumerable<string>? vocabulary = null)
    {
        _input = input;
        _path = modelPath;
        _grammar = Grammar(vocabulary);
        _worker = new Thread(Run) { IsBackground = true, Name = "Arca.Voz", Priority = ThreadPriority.AboveNormal };
    }

    private static string? Grammar(IEnumerable<string>? vocabulary)
    {
        if (vocabulary == null) return null;
        var words = vocabulary.Select(w => w.ToLowerInvariant().Trim()).Where(w => w.Length > 0).Distinct().Take(5000).ToList();
        if (words.Count == 0) return null;
        // Sem escapar acentos: o parser JSON do Vosk não entende sequências \uXXXX.
        var opts = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        return JsonSerializer.Serialize(words.Append("[unk]"), opts);
    }

    /// <summary>
    /// Troca o vocabulário (janela do roteiro em volta da posição de leitura). Uma gramática pequena deixa o
    /// reconhecimento bem mais preciso. A troca acontece no fim de uma frase ou no silêncio, sem perder palavras.
    /// </summary>
    public void SetVocabulary(IEnumerable<string>? vocabulary)
    {
        var g = Grammar(vocabulary);
        if (g == _grammar) return;
        _pendingGrammar = g; _hasPending = true;
    }

    /// <summary>Carrega o modelo uma vez por processo (centenas de MB na memória nos modelos grandes).</summary>
    private static Model LoadModel(string path)
    {
        lock (ModelLock)
        {
            if (_model != null && _modelPath == path) return _model;
            Vosk.Vosk.SetLogLevel(-1);
            _model?.Dispose();
            _model = new Model(path);
            _modelPath = path;
            return _model;
        }
    }

    /// <summary>Pré-carrega o modelo (thread de fundo): ligar a voz depois não espera o carregamento.</summary>
    public static void Preload(string path) => LoadModel(path);

    public void Start()
    {
        // A captura começa já; o modelo e o reconhecedor ficam prontos na thread do reconhecedor (sem travar a interface).
        _input.Samples += OnSamples;
        _running = true;
        _worker.Start();
        _input.Start();
    }

    private void OnSamples(float[] buf, int n)
    {
        var got = _fe.Process(buf, n, _input.SampleRate, out var f, out _);
        if (got <= 0) return;
        var pcm = new short[got];
        for (var i = 0; i < got; i++) pcm[i] = (short)Math.Clamp(f[i] * 32767f, short.MinValue, short.MaxValue);
        // Fila cheia (máquina lenta): descarta o bloco mais antigo em vez de travar a captura.
        if (!_queue.TryAdd(pcm)) { _queue.TryTake(out _); _queue.TryAdd(pcm); }
    }

    private void Run()
    {
        try
        {
            var model = LoadModel(_path);
            _rec = _grammar != null ? new VoskRecognizer(model, Rate, _grammar) : new VoskRecognizer(model, Rate);
            _rec.SetMaxAlternatives(0);
            _rec.SetWords(false);
            if (!_running) { _rec.Dispose(); _rec = null; return; }   // desligado enquanto carregava
            Ready?.Invoke();
            while (_running)
            {
                if (!_queue.TryTake(out var pcm, 100)) continue;
                var rec = _rec!;
                bool final = rec.AcceptWaveform(pcm, pcm.Length), quiet;
                if (final) { Emit(rec.Result(), "text", true); quiet = true; }
                else quiet = Emit(rec.PartialResult(), "partial", false) == 0;
                // Nova janela de vocabulário: troca só entre frases (nada em andamento no parcial).
                if (_hasPending && quiet)
                {
                    _hasPending = false; _grammar = _pendingGrammar;
                    var next = _grammar != null ? new VoskRecognizer(model, Rate, _grammar) : new VoskRecognizer(model, Rate);
                    next.SetMaxAlternatives(0); next.SetWords(false);
                    _rec = next; rec.Dispose(); _emitted = 0;
                }
            }
        }
        catch (Exception e) { Error?.Invoke(e); }
    }

    /// <summary>Emite só as palavras novas em relação ao parcial anterior. Devolve quantas palavras o resultado tem.</summary>
    private int Emit(string json, string field, bool final)
    {
        string text;
        try { using var d = JsonDocument.Parse(json); text = d.RootElement.TryGetProperty(field, out var t) ? t.GetString() ?? "" : ""; }
        catch (JsonException) { return 0; }
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = _emitted; i < words.Length; i++) Word?.Invoke(words[i]);
        _emitted = final ? 0 : Math.Max(_emitted, words.Length);
        return words.Length;
    }

    public void Dispose()
    {
        _running = false;
        _input.Samples -= OnSamples;
        _input.Dispose();
        if (_worker.IsAlive) _worker.Join(500);
        _rec?.Dispose(); _rec = null;
    }

    /// <summary>Procura um modelo Vosk para o idioma (pastas "vosk-model-*-pt*", "*-en-us*", "*-es*").</summary>
    public static string? FindModel(string language, IEnumerable<string> roots)
    {
        var tag = language.ToLowerInvariant() switch { "pt-br" or "pt-pt" => "-pt", "es-es" => "-es", "en-us" => "-en-us", var x => "-" + x.Split('-')[0] };
        foreach (var r in roots)
        {
            if (!Directory.Exists(r)) continue;
            var dir = Directory.EnumerateDirectories(r, "vosk-model*").Where(d => Path.GetFileName(d).Contains(tag, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => Path.GetFileName(d).Contains("small") ? 0 : 1).FirstOrDefault();
            if (dir != null && File.Exists(Path.Combine(dir, "final.mdl")) || dir != null && Directory.Exists(Path.Combine(dir, "am"))) return dir;
        }
        return null;
    }
}
