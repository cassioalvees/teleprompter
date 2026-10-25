using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Arca.App.Controls;
using Arca.App.Editor;
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Storage;
using Arca.Ndi;

namespace Arca.App.Services;

/// <summary>
/// Saída NDI (Arca.Ndi.dll), pensada para rolagem sem trancos:
/// <list type="bullet">
/// <item>O <b>texto</b> é renderizado em faixas ("tiles") da altura do quadro, numa thread WPF própria, só quando
///   a rolagem chega perto de uma faixa nova (≈ uma a cada poucos segundos) — não a cada quadro.</item>
/// <item>O <b>overlay</b> (faixa, fade, seta, linha de leitura) é renderizado uma vez, com fundo transparente.</item>
/// <item>Uma thread de envio com relógio de alta precisão monta cada quadro copiando linhas das faixas e
///   compondo o overlay (alguns milissegundos) e envia no ritmo exato do formato (29,97 / 59,94 / 25 qps).</item>
/// </list>
/// </summary>
public sealed class NdiService
{
    private readonly MainWindow _main;
    private Thread? _renderThread, _sendThread;
    private Dispatcher? _disp;
    private NdiSender? _sender;
    private PrompterSurface? _surface;
    private volatile bool _running;
    private DispatcherTimer? _statusTimer;

    // Posição (thread da interface → thread de envio), com tempo do relógio de alta precisão.
    private readonly object _posLock = new();
    private double _y, _v; private long _yAt;
    private System.Runtime.GCLatencyMode _gcMode = System.Runtime.GCLatencyMode.Interactive;

    // Faixas de texto prontas: índice → BGRA (W×H). Versão muda quando texto/aparência mudam.
    private readonly object _tileLock = new();
    private readonly Dictionary<int, byte[]> _tiles = new();
    private readonly HashSet<int> _pending = new();
    private int _version;
    private byte[]? _overlay;                 // BGRA pré-multiplicado
    private (int From, int To)[][] _ovRows = Array.Empty<(int, int)[]>(); // trechos com alfa ≠ 0, por linha
    private byte[] _bgRow = Array.Empty<byte>();
    private double _readPct = 30, _contentPx;
    private NdiFormat? _fmt;
    private bool _alpha, _mirror;

    public NdiService(MainWindow main) => _main = main;
    public bool Running => _running;

    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint ms);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint ms);

    public bool Start()
    {
        var vm = _main.Vm; var app = vm.App;
        if (_running) return true;
        if (!_main.Allow("ndi")) { vm.NdiOn = false; return false; }
        if (!NdiSender.IsAvailable)
        {
            vm.NdiOn = false; vm.NdiDot = "Live";
            vm.NdiStatus = "NDI Runtime não encontrado. Instale as NDI Tools (ndi.video/tools) e reabra o app.";
            return false;
        }
        var fmt = NdiFormat.Get(app.NdiFormat);
        try { _sender = new NdiSender(app.NdiName.Trim().Length > 0 ? app.NdiName.Trim() : "ARCA-PROMPTER", fmt, app.NdiAlpha); }
        catch (Exception e) { vm.NdiOn = false; vm.NdiDot = "Live"; vm.NdiStatus = "Erro · " + e.Message; Log.Error("NDI", e); return false; }
        _fmt = fmt; _alpha = app.NdiAlpha; _mirror = app.NdiMirror; _lastSig = null;
        _running = true;
        var ready = new ManualResetEventSlim();
        _renderThread = new Thread(() => RenderThread(fmt, ready)) { IsBackground = true, Name = "Arca.NDI.render" };
        _renderThread.SetApartmentState(ApartmentState.STA);
        _renderThread.Start();
        ready.Wait(3000);
        PushContent();
        timeBeginPeriod(1);
        // Enquanto o NDI está no ar, o .NET evita as limpezas de memória completas que param todas as threads.
        _gcMode = System.Runtime.GCSettings.LatencyMode;
        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
        _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "Arca.NDI.send", Priority = ThreadPriority.AboveNormal };
        _sendThread.Start();
        vm.NdiOn = true; vm.NdiDot = "Accent";
        vm.NdiStatus = $"Anunciando \"{_sender.Name}\" · {fmt.Label}";
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => Status();
        _statusTimer.Start();
        Log.Info($"NDI ligado: {_sender.Name} {fmt.Id} alpha={app.NdiAlpha} runtime {NdiSender.RuntimeVersion}");
        _main.UpdateNetLabel();
        return true;
    }

    public void Stop()
    {
        if (!_running && _sender == null) return;
        _running = false;
        _statusTimer?.Stop();
        _sendThread?.Join(1000);
        timeEndPeriod(1);
        System.Runtime.GCSettings.LatencyMode = _gcMode;
        _disp?.InvokeShutdown();
        _renderThread?.Join(1000);
        _sender?.Dispose(); _sender = null; _surface = null; _disp = null;
        lock (_tileLock) { _tiles.Clear(); _pending.Clear(); _retired.Clear(); _overlay = null; }
        var vm = _main.Vm; vm.NdiOn = false; vm.NdiDot = "Faint"; vm.NdiStatus = "Desligada";
        _main.UpdateNetLabel();
    }

    /// <summary>Formato/alpha/espelho mudaram: reinicia a saída.</summary>
    public void UpdateOptions() { if (_running) { Stop(); Start(); } }

    private void Status()
    {
        if (_sender == null) return;
        var n = _sender.Connections; var (pgm, pvw) = _sender.Tally;
        var vm = _main.Vm;
        vm.NdiDot = n > 0 ? "Ok" : "Accent";
        vm.NdiStatus = (n > 0 ? $"Transmitindo · {n} receptor(es)" : "Anunciando na rede · aguardando receptor") + $" · \"{_sender.Name}\""
            + (pgm ? " · NO AR (tally)" : pvw ? " · em preview" : "") + (vm.App.NdiMirror ? " · espelhada" : "") + (vm.App.NdiAlpha ? " · alpha" : "");
    }

    // ═══ Thread de renderização (WPF) ═══

    private void RenderThread(NdiFormat fmt, ManualResetEventSlim ready)
    {
        _disp = Dispatcher.CurrentDispatcher;
        var s = new PrompterSurface { Width = fmt.Width, Height = fmt.Height, FlipH = _mirror, ShowFlashAndCount = false, ShowHud = false };
        s.Measure(new Size(fmt.Width, fmt.Height)); s.Arrange(new Rect(0, 0, fmt.Width, fmt.Height));
        _surface = s;
        // O FlowDocument diagrama em segundo plano: quando a altura do texto muda, as faixas são refeitas.
        s.LayoutChanged += ScheduleRefresh;
        ready.Set();
        Dispatcher.Run();
    }

    /// <summary>Envia texto e aparência para a thread do NDI (o FlowDocument é criado lá) e refaz as faixas.</summary>
    public void PushContent()
    {
        if (!_running || _disp == null) return;
        var doc = _main.Doc.Clone(); var st = _main.Vm.S.Clone(); var key = st.FontKey;
        // Velocidade e duração não mudam a imagem: não refaz nada (refazer a cada ajuste fazia a saída piscar).
        var look = st.Clone(); look.SpeedT = 0; look.MinSpeed = 0; look.MaxSpeed = 0; look.ProgramSeconds = 0;
        var sig = System.Text.Json.JsonSerializer.Serialize(doc.Blocks) + System.Text.Json.JsonSerializer.Serialize(look);
        if (sig == _lastSig) return;
        _lastSig = sig;
        var layout = PrompterLayout.Compute(st, FontCatalog.Factor(key));
        _disp.BeginInvoke(() =>
        {
            var s = _surface; var fmt = _fmt;
            if (s == null || fmt == null) return;
            var fd = ScriptFlow.BuildPrompter(doc, st, layout, FontCatalog.Family(key), out var who);
            s.SetContent(fd, who, st, layout);
            _st = st;
            // As faixas antigas continuam no ar até as novas ficarem prontas (troca de uma vez, sem quadro vazio).
            ScheduleRefresh();
        });
    }

    private PrompterSettings? _st;
    private string? _lastSig;
    private bool _refreshQueued;

    /// <summary>Refaz overlay e faixas depois que o WPF terminar de diagramar o texto (prioridade ociosa).</summary>
    private void ScheduleRefresh()
    {
        if (_disp == null || _refreshQueued) return;
        _refreshQueued = true;
        _disp.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
        {
            _refreshQueued = false;
            var s = _surface; var fmt = _fmt; var st = _st;
            if (s == null || fmt == null || st == null) return;
            Layout(s, fmt);
            var k = fmt.Width / PrompterLayout.Width;
            // Overlay: só a camada fixa, sobre fundo transparente.
            s.ShowLayers(false, true);
            var ov = new byte[fmt.Width * fmt.Height * 4];
            s.CaptureInto(ov, fmt.Width, fmt.Height, true);
            s.ShowLayers(true, false);
            var rows = OverlayRows(ov, fmt.Width, fmt.Height);
            if (Environment.GetEnvironmentVariable("ARCA_NDI_DEBUG") is { Length: > 0 } dbg)
            {
                Log.Info($"NDI overlay: linhas com alfa {rows.Count(r => r.Length > 0)} · pixels {rows.Sum(r => r.Sum(g => g.Item2 - g.Item1 + 1))}");
                var bmp = System.Windows.Media.Imaging.BitmapSource.Create(fmt.Width, fmt.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null, ov, fmt.Width * 4);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder(); enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
                using var f = System.IO.File.Create(System.IO.Path.Combine(dbg, "overlay.png")); enc.Save(f);
            }
            var bg = ScriptFlow.ParseBrush(st.Background, System.Windows.Media.Brushes.Black) as System.Windows.Media.SolidColorBrush;
            var row = new byte[fmt.Width * 4];
            if (!_alpha && bg != null) for (var i = 0; i < fmt.Width; i++) { row[i * 4] = bg.Color.B; row[i * 4 + 1] = bg.Color.G; row[i * 4 + 2] = bg.Color.R; row[i * 4 + 3] = 255; }
            var contentPx = s.ContentHeight * k;
            // Faixas em volta da posição atual, renderizadas antes da troca.
            double y; lock (_posLock) y = _y;
            var cur = TileAt(y * k - fmt.Height * st.ReadPct / 100, fmt.Height);
            var fresh = new Dictionary<int, byte[]>();
            for (var i = cur - 1; i <= cur + 2; i++) if (CaptureTile(i, st.ReadPct, contentPx) is { } t) fresh[i] = t;
            lock (_tileLock)
            {
                _version++;
                foreach (var t in _tiles.Values) _retired.Enqueue((t, Stopwatch.GetTimestamp()));
                _tiles.Clear(); _pending.Clear();
                foreach (var (i, t) in fresh) _tiles[i] = t;
                _overlay = ov; _ovRows = rows; _bgRow = row;
                _readPct = st.ReadPct; _contentPx = contentPx;
            }
        });
    }

    private static void Layout(PrompterSurface s, NdiFormat fmt)
    {
        s.Measure(new Size(fmt.Width, fmt.Height)); s.Arrange(new Rect(0, 0, fmt.Width, fmt.Height)); s.UpdateLayout();
    }

    /// <summary>Renderiza a faixa <paramref name="i"/> (pixels do texto [i·H, (i+1)·H)). Thread de renderização.</summary>
    private void RenderTile(int i, int version)
    {
        var s = _surface; var fmt = _fmt;
        if (s == null || fmt == null || i < 0) return;
        lock (_tileLock) { if (version != _version || _tiles.ContainsKey(i)) { _pending.Remove(i); return; } }
        var buf = CaptureTile(i, _readPct, _contentPx);
        lock (_tileLock)
        {
            _pending.Remove(i);
            if (buf == null || version != _version) return;
            _tiles[i] = buf;
            // Mantém só as faixas perto da posição (memória: 8 MB cada em 1080p).
            foreach (var old in _tiles.Keys.Where(x => Math.Abs(x - i) > 3).ToList()) { _retired.Enqueue((_tiles[old], Stopwatch.GetTimestamp())); _tiles.Remove(old); }
        }
    }

    /// <summary>Renderiza a faixa <paramref name="i"/> do texto (null se está além do fim do texto).</summary>
    private byte[]? CaptureTile(int i, double readPct, double contentPx)
    {
        var s = _surface; var fmt = _fmt;
        if (s == null || fmt == null || i < 0) return null;
        if (i * (double)fmt.Height > contentPx + fmt.Height) return null;
        var k = fmt.Width / PrompterLayout.Width;
        // Posição que coloca o pixel i·H do texto na linha 0 do quadro.
        s.Y = (i * (double)fmt.Height + fmt.Height * readPct / 100) / k;
        Layout(s, fmt);
        var buf = Rent(fmt.Width * fmt.Height * 4);
        try
        {
            s.CaptureInto(buf, fmt.Width, fmt.Height, _alpha);
            if (Environment.GetEnvironmentVariable("ARCA_NDI_DEBUG") is { Length: > 0 } dbg)
            {
                Log.Info($"NDI faixa {i}: Y={s.Y:0.0} conteúdo={s.ContentHeight:0} px={contentPx:0}");
                var bmp = System.Windows.Media.Imaging.BitmapSource.Create(fmt.Width, fmt.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null, buf, fmt.Width * 4);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder(); enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
                using var f = System.IO.File.Create(System.IO.Path.Combine(dbg, $"tile{i}.png")); enc.Save(f);
            }
        }
        catch (Exception e) { Log.Error("NDI faixa", e); return null; }
        return buf;
    }

    private static int TileAt(double px, int h) => (int)Math.Floor(px / h);

    // Faixas reaproveitadas: cada uma tem 8 MB em 1080p e alocar sempre forçava limpezas de memória que paravam o
    // envio. A faixa descartada só volta a ser usada depois de 0,5 s (o quadro em montagem pode ainda estar lendo).
    private readonly Queue<(byte[] Buf, long At)> _retired = new();
    private void Retire(byte[] b) { lock (_tileLock) _retired.Enqueue((b, Stopwatch.GetTimestamp())); }
    private byte[] Rent(int size)
    {
        lock (_tileLock)
        {
            while (_retired.Count > 0)
            {
                var (b, at) = _retired.Peek();
                if ((Stopwatch.GetTimestamp() - at) / (double)Stopwatch.Frequency < 0.5) break;
                _retired.Dequeue();
                if (b.Length == size) return b;
            }
        }
        return new byte[size];
    }

    /// <summary>Trechos de cada linha onde o overlay tem alfa (a seta ocupa poucos pixels; a faixa, linhas inteiras).</summary>
    private static (int, int)[][] OverlayRows(byte[] ov, int w, int h)
    {
        var rows = new (int, int)[h][];
        var seg = new List<(int, int)>();
        for (var r = 0; r < h; r++)
        {
            seg.Clear(); int o = r * w * 4, start = -1;
            for (var x = 0; x < w; x++)
            {
                var on = ov[o + x * 4 + 3] != 0;
                if (on && start < 0) start = x;
                else if (!on && start >= 0) { seg.Add((start, x - 1)); start = -1; }
            }
            if (start >= 0) seg.Add((start, w - 1));
            rows[r] = seg.Count == 0 ? Array.Empty<(int, int)>() : seg.ToArray();
        }
        return rows;
    }

    // ═══ Thread de envio (relógio próprio) ═══

    private void SendLoop()
    {
        var fmt = _fmt!; int w = fmt.Width, h = fmt.Height, stride = w * 4;
        var frame = new byte[stride * h];
        var period = (double)fmt.FpsD / fmt.FpsN; // segundos por quadro (ex.: 1001/30000)
        var clock = Stopwatch.StartNew();
        long n = 0;
        var k = w / PrompterLayout.Width;
        // Métricas (registro a cada 10 s): tempo de montagem e atraso em relação ao instante ideal.
        double sumWork = 0, sumCompose = 0, maxWork = 0, maxLate = 0, maxCompose = 0, maxSend = 0; int frames = 0, late2 = 0; var lastLog = clock.Elapsed.TotalSeconds;
        int gc0 = GC.CollectionCount(0), gc2 = GC.CollectionCount(2);
        // Pausa: o quadro anterior foi previsto com a velocidade de antes e pode ter passado ~1 px do ponto.
        double lastSent = 0, lastMoveV = 0, holdY = 0, holdAt = 0; var holding = false;
        while (_running)
        {
            // Espera o instante exato do próximo quadro: dorme em passos de 1 ms e gira no último.
            var due = n * period;
            while (_running)
            {
                var left = due - clock.Elapsed.TotalSeconds;
                if (left <= 0) break;
                if (left > 0.002) Thread.Sleep(1); else Thread.SpinWait(50);
            }
            if (!_running) break;
            var late = clock.Elapsed.TotalSeconds - due;
            if (late > period * 3) n = (long)(clock.Elapsed.TotalSeconds / period); // atrasou muito: retoma o ritmo

            double y, v; long at;
            lock (_posLock) { y = _y; v = _v; at = _yAt; }
            var dt = Math.Min(0.25, (Stopwatch.GetTimestamp() - at) / (double)Stopwatch.Frequency);
            var t0 = clock.Elapsed.TotalMilliseconds;
            double tc = 0;
            var yy = y + v * dt;
            if (v != 0) { holding = false; lastMoveV = v; }
            else if (!holding && lastMoveV > 0 && lastSent > yy && lastSent - yy < 3) { holding = true; holdY = y; holdAt = lastSent; lastMoveV = 0; }
            if (holding && Math.Abs(y - holdY) > 0.01) holding = false;   // o app se mexeu (rolar à mão, linha): segue
            if (holding) yy = holdAt;   // parado no mesmo ponto: não recua
            // Andando: diferença pequena da previsão (≤ 3 unidades) nunca puxa o texto contra o movimento — espera alcançar.
            else if (v > 0 && yy < lastSent && lastSent - yy < 3) yy = lastSent;
            else if (v < 0 && yy > lastSent && yy - lastSent < 3) yy = lastSent;
            lastSent = yy;
            try { Compose(frame, yy, k, w, h, stride); tc = clock.Elapsed.TotalMilliseconds - t0; _sender?.Send(frame); }
            catch (Exception e) { Log.Error("NDI quadro", e); }
            var work = clock.Elapsed.TotalMilliseconds - t0;
            sumCompose += tc; maxCompose = Math.Max(maxCompose, tc); maxSend = Math.Max(maxSend, work - tc);
            sumWork += work; maxWork = Math.Max(maxWork, work); maxLate = Math.Max(maxLate, late * 1000); if (late > period * 0.5) late2++; frames++;
            if (clock.Elapsed.TotalSeconds - lastLog >= 5)
            {
                var g0 = GC.CollectionCount(0); var g2 = GC.CollectionCount(2);
                Log.Info($"NDI: {frames} quadros em 5 s · montagem {sumCompose / frames:0.0} ms (máx {maxCompose:0.0}) + envio {(sumWork - sumCompose) / frames:0.0} ms (máx {maxSend:0.0}) · maior atraso {maxLate:0.0} ms · atrasados {late2} · limpezas de memória {g0 - gc0} (completas {g2 - gc2})");
                sumWork = sumCompose = maxWork = maxLate = maxCompose = maxSend = 0; frames = late2 = 0; lastLog = clock.Elapsed.TotalSeconds; gc0 = g0; gc2 = g2;
            }
            n++;
        }
    }

    private void Compose(byte[] frame, double y, double k, int w, int h, int stride)
    {
        byte[]? ov; (int, int)[][] ovRows; byte[] bgRow; double readPct; int version;
        lock (_tileLock) { ov = _overlay; ovRows = _ovRows; bgRow = _bgRow; readPct = _readPct; version = _version; }
        // Posição em subpixel: linha inteira p0 + fração (0–1). Cada linha do quadro mistura a linha do texto e a
        // seguinte na proporção da fração — o movimento fica contínuo em vez de andar em degraus de 1 px.
        var pos = y * k - h * readPct / 100;
        var p0 = (long)Math.Floor(pos);
        var wgt = (int)Math.Round((pos - p0) * 256);
        if (wgt >= 256) { p0++; wgt = 0; }
        int t0 = TileAt(p0, h), t1 = t0 + 1;
        byte[]? a, b;
        lock (_tileLock)
        {
            _tiles.TryGetValue(t0, out a); _tiles.TryGetValue(t1, out b);
            // Pede as faixas que faltam e a próxima no sentido da rolagem (pré-carregamento).
            foreach (var need in new[] { t0, t1, t1 + 1, t0 - 1 })
                if (need >= 0 && !_tiles.ContainsKey(need) && _pending.Add(need))
                { var idx = need; _disp?.BeginInvoke(DispatcherPriority.Background, () => RenderTile(idx, version)); }
        }
        var span = frame.AsSpan();
        // Linha do texto p: da faixa certa, ou fundo (antes do início / depois do fim / faixa ainda não pronta).
        ReadOnlySpan<byte> Row(long p)
        {
            var ti = TileAt(p, h);
            var src = ti == t0 ? a : ti == t1 ? b : null;
            if (src != null && p >= 0) return src.AsSpan((int)(p - (long)ti * h) * stride, stride);
            return bgRow.Length == stride ? bgRow : _zeroRow.Length == stride ? _zeroRow : (_zeroRow = new byte[stride]);
        }
        for (var r = 0; r < h; r++)
        {
            var dst = span.Slice(r * stride, stride);
            if (wgt == 0) Row(p0 + r).CopyTo(dst);
            else Lerp(Row(p0 + r), Row(p0 + r + 1), dst, wgt);
        }
        if (ov != null) BlendOverlay(frame, ov, ovRows, w, h);
    }

    private byte[] _zeroRow = Array.Empty<byte>();

    /// <summary>d = a·(1 − w/256) + b·(w/256), vetorizado (SIMD).</summary>
    private static void Lerp(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> d, int w)
    {
        var n = System.Numerics.Vector<byte>.Count; var i = 0;
        var wa = new System.Numerics.Vector<ushort>((ushort)(256 - w)); var wb = new System.Numerics.Vector<ushort>((ushort)w);
        for (; i <= d.Length - n; i += n)
        {
            System.Numerics.Vector.Widen(new System.Numerics.Vector<byte>(a[i..]), out var alo, out var ahi);
            System.Numerics.Vector.Widen(new System.Numerics.Vector<byte>(b[i..]), out var blo, out var bhi);
            var lo = System.Numerics.Vector.ShiftRightLogical(alo * wa + blo * wb, 8);
            var hi = System.Numerics.Vector.ShiftRightLogical(ahi * wa + bhi * wb, 8);
            System.Numerics.Vector.Narrow(lo, hi).CopyTo(d[i..]);
        }
        for (; i < d.Length; i++) d[i] = (byte)((a[i] * (256 - w) + b[i] * w) >> 8);
    }

    /// <summary>Overlay (BGRA pré-multiplicado) sobre o quadro: d = s + d·(1 − αs). Só nos trechos com alfa.</summary>
    private static unsafe void BlendOverlay(byte[] frame, byte[] ov, (int From, int To)[][] rows, int w, int h)
    {
        fixed (byte* fp = frame) fixed (byte* op = ov)
        {
            for (var r = 0; r < h; r++)
            foreach (var (from, to) in rows[r])
            {
                var o = op + (r * w + from) * 4; var d = fp + (r * w + from) * 4;
                for (var x = from; x <= to; x++, o += 4, d += 4)
                {
                    int sa = o[3];
                    if (sa == 0) continue;
                    if (sa == 255) { *(uint*)d = *(uint*)o; continue; }
                    int ia = 255 - sa;
                    d[0] = (byte)(o[0] + (d[0] * ia + 127) / 255);
                    d[1] = (byte)(o[1] + (d[1] * ia + 127) / 255);
                    d[2] = (byte)(o[2] + (d[2] * ia + 127) / 255);
                    d[3] = (byte)(sa + (d[3] * ia + 127) / 255);
                }
            }
        }
    }

    public void SetPosition(double y, double v) { lock (_posLock) { _y = y; _v = v; _yAt = Stopwatch.GetTimestamp(); } }
}
