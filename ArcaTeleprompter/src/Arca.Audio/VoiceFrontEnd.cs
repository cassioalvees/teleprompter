using NAudio.Dsp;

namespace Arca.Audio;

/// <summary>
/// Contrato comum dos motores de reconhecimento (Vosk e Whisper): mesma entrada (<see cref="AudioInput"/>), mesmo
/// tratamento do sinal e os mesmos eventos — o resto do app não precisa saber qual motor está ligado.
/// </summary>
public interface IVoiceRecognizer : IDisposable
{
    /// <summary>Nova palavra reconhecida (thread do reconhecedor).</summary>
    event Action<string>? Word;
    event Action<double>? Level;
    event Action<Exception>? Error;
    double GateDb { get; set; }
    double InputGainDb { get; set; }
    bool AutoGain { get; set; }
    double LevelDb { get; }
    /// <summary>Palavras do roteiro em volta da posição de leitura (gramática no Vosk, contexto no Whisper).</summary>
    void SetVocabulary(IEnumerable<string>? vocabulary);
    void Start();
}

/// <summary>
/// Tratamento do sinal antes do reconhecedor (thread de captura): passa-altas 80 Hz → controle automático de ganho
/// (AGC) + gate suave → reamostragem para 16 kHz mono. Entrega blocos float em [-1, 1] e se o bloco tem fala.
/// </summary>
public sealed class VoiceFrontEnd
{
    public const int Rate = 16000;

    private readonly WdlResampler _rs = new();
    private bool _rsReady;
    private float[] _rsOut = new float[4096];
    private float[] _work = new float[4096];
    private BiQuadFilter? _hp;
    private float _agc = 4f, _gate = 1f;
    private double _floor = 0.001;
    private const double TargetRms = 0.1;   // −20 dBFS: nível que o modelo reconhece melhor
    private const float MaxGain = 40f;       // +32 dB para microfones fracos / linhas baixas
    private const float GateFloor = 0.15f;   // −16 dB no silêncio (gate suave, nunca corta a fala)

    /// <summary>
    /// Limiar do gate em dBFS (nível de entrada). Abaixo dele — e abaixo do ruído de fundo medido — o áudio é
    /// atenuado (não zerado), para o ruído não virar "palavras".
    /// </summary>
    public double GateDb { get; set; } = -58;
    /// <summary>Volume de entrada em dB aplicado antes de tudo (o medidor mostra o nível já com ele).</summary>
    public double InputGainDb { get => _inDb; set { _inDb = Math.Clamp(value, -24, 30); _inGain = (float)Math.Pow(10, _inDb / 20); } }
    private double _inDb; private float _inGain = 1f;
    /// <summary>Ganho automático (leva a fala a −20 dBFS). Desligado: só o volume de entrada.</summary>
    public bool AutoGain { get; set; } = true;
    /// <summary>Último nível RMS em dBFS (−70 a 0).</summary>
    public double LevelDb { get; private set; } = -70;

    public event Action<double>? Level;

    /// <summary>Processa um bloco da captura (taxa nativa). Devolve as amostras a 16 kHz em <paramref name="pcm"/> (0 = nada ainda).</summary>
    public int Process(float[] buf, int n, int sampleRate, out float[] pcm, out bool speech)
    {
        // A taxa só é conhecida depois que a captura abre: configura o reamostrador no primeiro bloco.
        if (!_rsReady)
        {
            _rs.SetMode(true, 2, false);
            _rs.SetFilterParms();
            _rs.SetFeedMode(true);
            _rs.SetRates(sampleRate, Rate);
            _rsReady = true;
        }
        // Passa-altas 80 Hz: tira ronco, vento e zumbido de rede que confundem o modelo.
        _hp ??= BiQuadFilter.HighPassFilter(sampleRate, 80, 0.707f);
        if (_work.Length < n) _work = new float[n];
        double q = 0;
        for (var i = 0; i < n; i++) { var x = _hp.Transform(buf[i] * _inGain); _work[i] = x; q += x * x; }
        var rms = Math.Sqrt(q / Math.Max(1, n));
        var db = Math.Max(-70, 20 * Math.Log10(rms + 1e-6));
        LevelDb = db; Level?.Invoke(db);

        // Ruído de fundo: segue o mínimo rápido e sobe devagar (≈ +2 dB/s).
        _floor = rms < _floor ? _floor * 0.8 + rms * 0.2 : _floor * 1.0025;
        speech = rms > _floor * 1.8 && db >= GateDb;

        // AGC: leva a fala a −20 dBFS. Desce rápido (evita saturar), sobe devagar; só aprende durante a fala.
        if (!AutoGain) _agc = 1f;
        var a0 = _agc;
        if (speech && AutoGain)
        {
            var want = (float)Math.Clamp(TargetRms / Math.Max(rms, 1e-5), 0.5, MaxGain);
            _agc = want < _agc ? _agc * 0.5f + want * 0.5f : _agc * 0.96f + want * 0.04f;
        }
        var g0 = _gate;
        _gate = speech ? Math.Min(1f, _gate + 0.5f) : Math.Max(GateFloor, _gate * 0.85f);

        // Reamostragem para 16 kHz (filtro sinc da WDL), com ganho interpolado no bloco (sem estalos).
        int framesIn = _rs.ResamplePrepare(n, 1, out var inBuf, out var inOff);
        for (var i = 0; i < framesIn; i++)
        {
            var t = (float)i / Math.Max(1, framesIn);
            var gain = (a0 + (_agc - a0) * t) * (g0 + (_gate - g0) * t);
            inBuf[inOff + i] = Math.Clamp(_work[i] * gain, -1f, 1f);
        }
        var outFrames = (int)Math.Ceiling(n * (double)Rate / sampleRate) + 16;
        if (_rsOut.Length < outFrames) _rsOut = new float[outFrames];
        pcm = _rsOut;
        return Math.Max(0, _rs.ResampleOut(_rsOut, 0, framesIn, outFrames, 1));
    }
}
