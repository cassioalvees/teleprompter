using System.Runtime.InteropServices;
using System.Text;

namespace Arca.Ndi;

/// <summary>Formato de saída NDI.</summary>
public sealed record NdiFormat(string Id, string Label, int Width, int Height, int FpsN, int FpsD)
{
    /// <summary>Texto mostrado em listas (o padrão de record mostraria todos os campos).</summary>
    public override string ToString() => Label;

    public static readonly NdiFormat[] All =
    {
        new("1080p30", "1920 × 1080 · 30p", 1920, 1080, 30000, 1001),
        new("1080p60", "1920 × 1080 · 60p", 1920, 1080, 60000, 1001),
        new("1080p25", "1920 × 1080 · 25p", 1920, 1080, 25, 1),
        new("720p30", "1280 × 720 · 30p", 1280, 720, 30000, 1001)
    };
    public static NdiFormat Get(string id) => All.FirstOrDefault(f => f.Id == id) ?? All[0];
    public double Fps => (double)FpsN / FpsD;
}

/// <summary>
/// Fonte NDI na rede. Recebe quadros BGRA (32 bits, pré-multiplicados quando há alpha) e envia de forma
/// assíncrona com buffer duplo: o NDI lê o quadro anterior enquanto o próximo é preenchido.
/// Seguro para chamar de qualquer thread, uma de cada vez.
/// </summary>
public sealed class NdiSender : IDisposable
{
    private static bool? _available;
    private static string? _version;
    private IntPtr _inst;
    private readonly IntPtr[] _buf = new IntPtr[2];
    private readonly IntPtr _frame;
    private int _cur, _bufSize;
    private IntPtr _name;
    private readonly object _gate = new();

    public string Name { get; }
    public NdiFormat Format { get; }
    public bool Alpha { get; }

    /// <summary>O runtime NDI está instalado e carregou.</summary>
    public static bool IsAvailable
    {
        get
        {
            if (_available == null)
            {
                _available = NdiNative.TryLoad();
                if (_available == true) try { _version = Marshal.PtrToStringAnsi(NdiNative.NDIlib_version()); } catch { /* */ }
            }
            return _available.Value;
        }
    }
    public static string? RuntimeVersion => IsAvailable ? _version : null;
    public static string? RuntimePath => IsAvailable ? NdiNative.FoundPath : null;
    public const string DownloadUrl = "https://ndi.video/tools/";

    public NdiSender(string name, NdiFormat format, bool alpha)
    {
        if (!IsAvailable) throw new InvalidOperationException("NDI Runtime não encontrado. Instale as NDI Tools ou o NDI Runtime.");
        Name = name; Format = format; Alpha = alpha;
        _name = StringToUtf8(name);
        var cs = new NdiNative.SendCreate { p_ndi_name = _name, clock_video = false, clock_audio = false };
        _inst = NdiNative.NDIlib_send_create(ref cs);
        if (_inst == IntPtr.Zero) throw new InvalidOperationException("Não foi possível criar a fonte NDI \"" + name + "\".");
        Identify();
        _bufSize = format.Width * format.Height * 4;
        for (var i = 0; i < 2; i++) _buf[i] = Marshal.AllocHGlobal(_bufSize);
        _frame = Marshal.AllocHGlobal(Marshal.SizeOf<NdiNative.VideoFrameV2>());
    }

    /// <summary>
    /// Metadado de conexão (padrão do SDK) que os receptores mostram: fabricante, produto e versão — o Studio
    /// Monitor, o vMix e o OBS passam a identificar a fonte como ARCA Teleprompter.
    /// </summary>
    private void Identify()
    {
        try
        {
            var ver = typeof(NdiSender).Assembly.GetName().Version?.ToString(3) ?? "";
            var xml = $"<ndi_product long_name=\"ARCA Teleprompter\" short_name=\"ARCA\" manufacturer=\"ARCA\" version=\"{ver}\" session=\"default\" model_name=\"Teleprompter\" serial=\"{Environment.MachineName}\"/>";
            var bytes = System.Text.Encoding.UTF8.GetBytes(xml + "\0");
            var p = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, p, bytes.Length);
            var md = new NdiNative.MetadataFrame { length = bytes.Length, timecode = NdiNative.TimecodeSynthesize, p_data = p };
            NdiNative.NDIlib_send_add_connection_metadata(_inst, ref md);
            Marshal.FreeHGlobal(p); // o SDK copia o conteúdo
        }
        catch (Exception) { /* runtime antigo sem a função: segue sem o metadado */ }
    }

    /// <summary>Envia um quadro. <paramref name="bgra"/> deve ter Width × Height × 4 bytes, linha a linha.</summary>
    public void Send(ReadOnlySpan<byte> bgra)
    {
        lock (_gate)
        {
            if (_inst == IntPtr.Zero) return;
            if (bgra.Length < _bufSize) throw new ArgumentException("Quadro menor que o formato.");
            unsafe { bgra[.._bufSize].CopyTo(new Span<byte>((void*)_buf[_cur], _bufSize)); }
            var f = new NdiNative.VideoFrameV2
            {
                xres = Format.Width, yres = Format.Height,
                FourCC = Alpha ? NdiNative.FourCC_BGRA : NdiNative.FourCC_BGRX,
                frame_rate_N = Format.FpsN, frame_rate_D = Format.FpsD,
                picture_aspect_ratio = (float)Format.Width / Format.Height,
                frame_format_type = NdiNative.FrameProgressive,
                timecode = NdiNative.TimecodeSynthesize,
                p_data = _buf[_cur],
                line_stride_in_bytes = Format.Width * 4
            };
            Marshal.StructureToPtr(f, _frame, false);
            NdiNative.NDIlib_send_send_video_async_v2(_inst, _frame);
            _cur ^= 1;
        }
    }

    /// <summary>Receptores conectados (vMix, OBS, Studio Monitor…).</summary>
    public int Connections => _inst == IntPtr.Zero ? 0 : NdiNative.NDIlib_send_get_no_connections(_inst, 0);

    /// <summary>Tally: (no ar, em preview).</summary>
    public (bool Program, bool Preview) Tally
    {
        get
        {
            if (_inst == IntPtr.Zero) return (false, false);
            var t = new NdiNative.Tally();
            NdiNative.NDIlib_send_get_tally(_inst, ref t, 0);
            return (t.on_program, t.on_preview);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_inst == IntPtr.Zero) return;
            // Envia um quadro nulo para o NDI liberar o último buffer assíncrono antes de destruir.
            NdiNative.NDIlib_send_send_video_async_v2(_inst, IntPtr.Zero);
            NdiNative.NDIlib_send_destroy(_inst);
            _inst = IntPtr.Zero;
            foreach (var b in _buf) if (b != IntPtr.Zero) Marshal.FreeHGlobal(b);
            Marshal.FreeHGlobal(_frame);
            Marshal.FreeHGlobal(_name);
        }
    }

    private static IntPtr StringToUtf8(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s + "\0");
        var p = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, p, bytes.Length);
        return p;
    }
}
