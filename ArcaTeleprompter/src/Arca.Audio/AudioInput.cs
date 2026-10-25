using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Arca.Audio;

/// <summary>Driver de captura.</summary>
public enum AudioDriver { Wasapi, Asio, /** Arquivo WAV tocado em tempo real (testes e demonstração). */ File }

/// <summary>Dispositivo de entrada listável na interface.</summary>
public sealed record AudioDeviceInfo(AudioDriver Driver, string Id, string Name, int Channels)
{
    public override string ToString() => Name;
}

/// <summary>
/// Captura de áudio de microfone ou interface (consoles digitais, interfaces USB).
/// WASAPI (compartilhado, ~10–20 ms) para qualquer microfone; ASIO para escolher um canal físico
/// específico da interface (ex.: CH 5 de uma X-AIR/UMC). Entrega blocos mono em float, na taxa nativa.
/// </summary>
public sealed class AudioInput : IDisposable
{
    private WasapiCapture? _wasapi;
    private AsioOut? _asio;
    private float[] _mono = Array.Empty<float>();
    private double[] _chLevel = Array.Empty<double>();
    private int _bestCh;
    private NAudio.Wave.WaveFileWriter? _diag;
    private long _diagLeft;

    /// <summary>Formato bruto da captura (para diagnóstico).</summary>
    public string FormatInfo { get; private set; } = "";
    /// <summary>Canal escolhido automaticamente (0 = primeiro).</summary>
    public int ActiveChannel => _bestCh;

    /// <summary>
    /// Grava a captura <b>bruta</b> (todos os canais, formato original) por alguns segundos num WAV — para analisar
    /// problemas de reconhecimento com o áudio real do microfone.
    /// </summary>
    public void RecordDiagnostic(string path, double seconds)
    {
        if (_wasapi == null) return;
        lock (this) { _diag?.Dispose(); _diag = new NAudio.Wave.WaveFileWriter(path, _wasapi.WaveFormat); _diagLeft = (long)(_wasapi.WaveFormat.AverageBytesPerSecond * seconds); }
    }

    public AudioDriver Driver { get; }
    public string DeviceId { get; }
    /// <summary>Canal (0 = primeiro) usado no ASIO. No WASAPI e em arquivos com vários canais, usa o canal com mais voz.</summary>
    public int Channel { get; }
    public int SampleRate { get; private set; }

    /// <summary>Bloco de amostras mono [-1, 1]. Chamado na thread de áudio: não bloquear.</summary>
    public event Action<float[], int>? Samples;
    public event Action<Exception?>? Stopped;

    public AudioInput(AudioDriver driver, string deviceId, int channel = 0)
    {
        Driver = driver; DeviceId = deviceId; Channel = Math.Max(0, channel);
    }

    public static List<AudioDeviceInfo> ListWasapi()
    {
        var list = new List<AudioDeviceInfo>();
        try
        {
            using var en = new MMDeviceEnumerator();
            string? def = null;
            try { def = en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID; } catch { /* sem padrão */ }
            foreach (var d in en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                int ch = 1; try { ch = d.AudioClient.MixFormat.Channels; } catch { /* */ }
                list.Add(new AudioDeviceInfo(AudioDriver.Wasapi, d.ID, d.FriendlyName + (d.ID == def ? " (padrão)" : ""), ch));
            }
        }
        catch (Exception) { /* sem áudio */ }
        return list;
    }

    public static List<AudioDeviceInfo> ListAsio()
    {
        var list = new List<AudioDeviceInfo>();
        string[] names;
        try { names = AsioOut.GetDriverNames(); } catch { return list; }
        foreach (var n in names)
        {
            var ch = 0;
            try { using var a = new AsioOut(n); ch = a.DriverInputChannelCount; } catch { /* driver ocupado ou sem hardware */ }
            list.Add(new AudioDeviceInfo(AudioDriver.Asio, n, n, ch));
        }
        return list;
    }

    public void Start()
    {
        if (Driver == AudioDriver.Wasapi) StartWasapi();
        else if (Driver == AudioDriver.Asio) StartAsio();
        else StartFile();
    }

    private CancellationTokenSource? _fileCts;
    /// <summary>Ritmo da leitura de arquivo (só testes): 1 = tempo real; N = N vezes mais devagar, como num computador N vezes mais rápido.</summary>
    public static double FilePace { get; set; } = 1;

    private void StartFile()
    {
        var reader = new AudioFileReader(DeviceId);
        SampleRate = reader.WaveFormat.SampleRate;
        var ch = reader.WaveFormat.Channels;
        _fileCts = new CancellationTokenSource();
        var ct = _fileCts.Token;
        new Thread(() =>
        {
            var block = SampleRate / 50 * ch; var buf = new float[block];
            var sw = System.Diagnostics.Stopwatch.StartNew(); long sent = 0;
            try
            {
                int n;
                while (!ct.IsCancellationRequested && (n = reader.Read(buf, 0, block)) > 0)
                {
                    var frames = n / ch;
                    if (_mono.Length < frames) _mono = new float[frames];
                    PickChannel(ch, frames, (i, c) => buf[i * ch + c]);
                    for (var i = 0; i < frames; i++) _mono[i] = buf[i * ch + _bestCh];
                    Samples?.Invoke(_mono, frames);
                    sent += frames;
                    var wait = (int)(sent * 1000 * FilePace / SampleRate - sw.ElapsedMilliseconds);
                    if (wait > 0) Thread.Sleep(wait);
                }
                Stopped?.Invoke(null);
            }
            catch (Exception e) { Stopped?.Invoke(e); }
            finally { reader.Dispose(); }
        }) { IsBackground = true, Name = "Arca.ArquivoDeAudio" }.Start();
    }

    private void StartWasapi()
    {
        using var en = new MMDeviceEnumerator();
        // Dispositivo padrão de gravação do Windows (o de "comunicações" costuma ser fone/headset ou ter filtros).
        var dev = string.IsNullOrEmpty(DeviceId) ? en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console) : en.GetDevice(DeviceId);
        _wasapi = new WasapiCapture(dev, true, 20);
        var fmt = _wasapi.WaveFormat;
        SampleRate = fmt.SampleRate;
        FormatInfo = $"{dev.FriendlyName}: {fmt.Channels} canal(is), {fmt.SampleRate} Hz, {fmt.BitsPerSample} bits, {fmt.Encoding}";
        _wasapi.DataAvailable += (_, e) =>
        {
            lock (this)
            {
                if (_diag != null)
                {
                    var take = (int)Math.Min(e.BytesRecorded, _diagLeft);
                    _diag.Write(e.Buffer, 0, take); _diagLeft -= take;
                    if (_diagLeft <= 0) { _diag.Dispose(); _diag = null; }
                }
            }
            var n = ToMono(e.Buffer, e.BytesRecorded, fmt);
            if (n > 0) Samples?.Invoke(_mono, n);
        };
        _wasapi.RecordingStopped += (_, e) => Stopped?.Invoke(e.Exception);
        _wasapi.StartRecording();
    }

    private void StartAsio()
    {
        _asio = new AsioOut(DeviceId);
        var count = _asio.DriverInputChannelCount;
        if (count == 0) throw new InvalidOperationException("O driver ASIO não tem entradas disponíveis.");
        var ch = Math.Min(Channel, count - 1);
        _asio.InputChannelOffset = ch;
        // Interfaces de áudio travam o clock no hardware (geralmente 48 kHz): tenta 48k e cai para 44,1k.
        foreach (var rate in new[] { 48000, 44100 })
        {
            try { _asio.InitRecordAndPlayback(null, 1, rate); SampleRate = rate; break; }
            catch (Exception) when (rate != 44100) { _asio.Dispose(); _asio = new AsioOut(DeviceId) { InputChannelOffset = ch }; }
        }
        _asio.AudioAvailable += (_, e) =>
        {
            var need = e.SamplesPerBuffer;
            if (_mono.Length < need) _mono = new float[need];
            // Um canal pedido: GetAsInterleavedSamples devolve float 32 bits já em [-1, 1].
            var n = e.GetAsInterleavedSamples(_mono);
            Samples?.Invoke(_mono, n);
        };
        _asio.PlaybackStopped += (_, e) => Stopped?.Invoke(e.Exception);
        _asio.Play();
    }

    private int ToMono(byte[] buf, int bytes, WaveFormat f)
    {
        var bps = f.BitsPerSample / 8; var ch = f.Channels;
        var frames = bytes / (bps * ch);
        if (_mono.Length < frames) _mono = new float[frames];
        var isFloat = f.Encoding == WaveFormatEncoding.IeeeFloat || (f.Encoding == WaveFormatEncoding.Extensible && f.BitsPerSample == 32 && f is WaveFormatExtensible wx && wx.SubFormat == NAudio.Dmo.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT);
        float S(int i, int c)
        {
            var o = (i * ch + c) * bps;
            return bps switch
            {
                4 when isFloat => BitConverter.ToSingle(buf, o),
                4 => BitConverter.ToInt32(buf, o) / 2147483648f,
                3 => ((buf[o] << 8 | buf[o + 1] << 16 | buf[o + 2] << 24) >> 8) / 8388608f,
                2 => BitConverter.ToInt16(buf, o) / 32768f,
                _ => (buf[o] - 128) / 128f
            };
        }
        // Microfones "array" de notebook entregam 2–4 canais (às vezes um invertido ou de referência):
        // somar os canais pode cancelar a voz. Usa só o canal com mais sinal.
        PickChannel(ch, frames, S);
        for (var i = 0; i < frames; i++) _mono[i] = S(i, _bestCh);
        return frames;
    }

    /// <summary>Escolhe o canal com mais energia (média móvel de ~1 s, com histerese para não ficar trocando).</summary>
    private void PickChannel(int ch, int frames, Func<int, int, float> sample)
    {
        if (ch <= 1) { _bestCh = 0; return; }
        if (_chLevel.Length != ch) { _chLevel = new double[ch]; _bestCh = 0; }
        for (var c = 0; c < ch; c++)
        {
            double q = 0; for (var i = 0; i < frames; i += 2) { var v = sample(i, c); q += v * v; }
            _chLevel[c] = _chLevel[c] * 0.95 + q / Math.Max(1, frames / 2) * 0.05;
        }
        var best = 0; for (var c = 1; c < ch; c++) if (_chLevel[c] > _chLevel[best]) best = c;
        if (best != _bestCh && _chLevel[best] > _chLevel[_bestCh] * 1.6) _bestCh = best;
    }

    public void Dispose()
    {
        _fileCts?.Cancel();
        try { _wasapi?.StopRecording(); } catch { /* */ }
        _wasapi?.Dispose(); _wasapi = null;
        try { _asio?.Stop(); } catch { /* */ }
        _asio?.Dispose(); _asio = null;
    }
}
